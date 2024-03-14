using Grains.types;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Timers;
using Shared;
// ReSharper disable TooManyChainedReferences

namespace Grains.usage_tracker;

/// <summary>
/// It keeps track of how many jobs have been started per each account in the last minute (or jobs started more than
/// one minute ago but haven't completed). It will compare this count against the account's quota and choose the least
/// loaded account for the next job.
/// </summary>
public class SchedulerGrain : Grain, ISchedulerGrain, IDisposable
{
    private const long RATE_LIMIT_DURATION = 60;
    private const long CLEANUP_INTERVAL = 180;
    private const int MAX_ATTEMPTS = 99999;
    private const float QUOTA_THRESHOLD = 0.15f;

    private readonly IPersistentState<SchedulerState> _masterTrackerState;
    private readonly ILogger<SchedulerGrain> _logger;
    
    private IDisposable? _timer;
    private IDisposable? _flushTimer;
    
    private Dictionary<string, ApiKeyUsageInfo> _apiKeyStatus = new();

    public SchedulerGrain(
        [PersistentState("masterTrackerState", "MySqlSchrodingerImageStore")]
        IPersistentState<SchedulerState> masterTrackerState,
        ILogger<SchedulerGrain> logger)
    {
        _logger = logger;
        _masterTrackerState = masterTrackerState;
    }
    
    public async Task FlushAsync()
    {
        // activation safe code
        try
        {
            await _masterTrackerState.WriteStateAsync();
        }
        catch (Exception e)
        {
            _logger.LogError($"[SchedulerGrain] : {e.Message}");
        }
    }
    
    public override Task OnActivateAsync()
    {
        _timer = RegisterTimer(asyncCallback: _ => this.AsReference<ISchedulerGrain>().TickAsync(),null,
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromSeconds(1));
        
        _flushTimer = RegisterTimer(asyncCallback: _ => FlushTimerAsync(), null,
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromSeconds(1));
        
        if(_masterTrackerState.State.CompletedImageGenerationRequests == null)
            _masterTrackerState.State.CompletedImageGenerationRequests = new Dictionary<string, RequestAccountUsageInfo>();
        if(_masterTrackerState.State.FailedImageGenerationRequests == null)
            _masterTrackerState.State.FailedImageGenerationRequests = new Dictionary<string, RequestAccountUsageInfo>();
        if(_masterTrackerState.State.PendingImageGenerationRequests == null)
            _masterTrackerState.State.PendingImageGenerationRequests = new Dictionary<string, RequestAccountUsageInfo>();
        if(_masterTrackerState.State.StartedImageGenerationRequests == null)
            _masterTrackerState.State.StartedImageGenerationRequests = new Dictionary<string, RequestAccountUsageInfo>();
        
        return base.OnActivateAsync();
    }
    
    /// <summary>
    /// Opens up the grain factory for mocking.
    /// </summary>
    public virtual new IGrainFactory GrainFactory => base.GrainFactory;

    public Task ReportFailedImageGenerationRequestAsync(RequestStatus requestStatus)
    {
        _logger.LogError($"[SchedulerGrain] Image generation failed with message: {requestStatus.Message}");
        var info = PopFromPending(requestStatus.RequestId);
        if (info == null)
        {
            return Task.CompletedTask;
        }
        var unixTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        info.FailedTimestamp = unixTimestamp;
        info.StartedTimestamp = (requestStatus.RequestTimestamp == 0)? info.StartedTimestamp: requestStatus.RequestTimestamp;
        _masterTrackerState.State.FailedImageGenerationRequests.Add(requestStatus.RequestId, info);
        
        HandleErrorCode(info.ApiKey, info.StartedTimestamp, requestStatus.ErrorCode);
        
        return Task.CompletedTask;
    }

    public Task ReportCompletedImageGenerationRequestAsync(RequestStatus requestStatus)
    {
        _logger.LogInformation($"[SchedulerGrain] Report Completed Image Generation Request with ID: {requestStatus.RequestId}");
        
        var info = PopFromPending(requestStatus.RequestId);
        if (info == null)
        {
            return Task.CompletedTask;
        }
        
        var unixTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        info.CompletedTimestamp = unixTimestamp;
        info.StartedTimestamp = requestStatus.RequestTimestamp;
        _masterTrackerState.State.CompletedImageGenerationRequests.Add(requestStatus.RequestId, info);
        
        RefreshApiUsageInfo(info.ApiKey);
        
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetFailedImageGenerationRequestsAsync()
    {
        return Task.FromResult<IReadOnlyDictionary<string, RequestAccountUsageInfo>>(
            _masterTrackerState.State.FailedImageGenerationRequests);
    }

    public Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetStartedImageGenerationRequestsAsync()
    {
        return Task.FromResult<IReadOnlyDictionary<string, RequestAccountUsageInfo>>(_masterTrackerState.State
            .StartedImageGenerationRequests);
    }

    public Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetPendingImageGenerationRequestsAsync()
    {
        return Task.FromResult<IReadOnlyDictionary<string, RequestAccountUsageInfo>>(_masterTrackerState.State
            .PendingImageGenerationRequests);
    }

    public Task AddImageGenerationRequest(string requestId, string childId, long requestTimestamp)
    {
        _logger.LogInformation($"[SchedulerGrain] Adding image generation request with ID: {requestId} for child ID: {childId}");
        _masterTrackerState.State.StartedImageGenerationRequests.Add(childId, new RequestAccountUsageInfo
        {
            RequestId = requestId,
            RequestTimestamp = requestTimestamp,
            Attempts = 0,
            ChildId = childId
        });

        return Task.CompletedTask;
    }

    public async Task<List<string>> AddApiKeys(List<ApiKeyEntry> apiKeyEntries)
    {
        List<string> addedApiKeys = new();
        foreach (var apiKeyEntry in apiKeyEntries)
        {
            if(_masterTrackerState.State.ApiAccountInfoList == null)
                _masterTrackerState.State.ApiAccountInfoList = new List<APIAccountInfo>();
            
            _masterTrackerState.State.ApiAccountInfoList.Add(new APIAccountInfo
            {
                ApiKey = apiKeyEntry.ApiKey,
                Email = apiKeyEntry.Email,
                Tier = apiKeyEntry.Tier,
                MaxQuota = apiKeyEntry.MaxQuota
            });
            addedApiKeys.Add(apiKeyEntry.ApiKey);
        }
        await _masterTrackerState.WriteStateAsync();

        return addedApiKeys;
    }

    //returns a list of apikeys that were removed
    public async Task<List<string>> RemoveApiKeys(List<string> apiKey)
    {
        List<string> removedApiKeys = new();
        
        if(_masterTrackerState.State.ApiAccountInfoList == null)
            return removedApiKeys;
        
        _masterTrackerState.State.ApiAccountInfoList.RemoveAll(apiInfo =>
        {
            if (apiKey.Contains(apiInfo.ApiKey))
            {
                removedApiKeys.Add(apiInfo.ApiKey);
                return true;
            }
            return false;
        });
        
        await _masterTrackerState.WriteStateAsync();
        
        return removedApiKeys;
    }

    public Task<IReadOnlyList<APIAccountInfo>> GetAllApiKeys()
    {
        if(_masterTrackerState.State.ApiAccountInfoList == null)
            return Task.FromResult<IReadOnlyList<APIAccountInfo>>(new List<APIAccountInfo>());
        return Task.FromResult<IReadOnlyList<APIAccountInfo>>(_masterTrackerState.State.ApiAccountInfoList);
    }

    public Task<SchedulerState> GetImageGenerationStates()
    {
        return Task.FromResult(_masterTrackerState.State);
    }
    
    public Task<Dictionary<string, ApiKeyUsageInfo>> GetApiKeysUsageInfo()
    {
        return Task.FromResult(_apiKeyStatus);
    }
    
    public async Task TickAsync()
    {
        // 1. Purge completed requests
        // 2. Add failed requests to pending queue
        // 3. Check remaining quota for all accounts
        // 4. For all pending tasks, find the account with the most remaining quota
        // 5. Schedule the task, update the account usage info
        
        UpdateApiUsageStatus();

        if (_masterTrackerState.State.ApiAccountInfoList == null)
        {
            return;
        }
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        //get all requests that are within the rate limit duration
        var cutoff = now - RATE_LIMIT_DURATION;
        var allRequests = _masterTrackerState.State.CompletedImageGenerationRequests
            .Concat(_masterTrackerState.State.FailedImageGenerationRequests)
            .Concat(_masterTrackerState.State.PendingImageGenerationRequests)
            .Concat(_masterTrackerState.State.StartedImageGenerationRequests)
            .ToList();
        var usedQuota = allRequests
            .Where(i=> i.Value.StartedTimestamp > cutoff)
            .GroupBy(
            x=>x.Value.ApiKey,
            x=>x
            ).ToDictionary(x=>x.Key, x=>x.Count());
        var remainingQuotaByApiKey = _masterTrackerState.State.ApiAccountInfoList
                .ToDictionary(i=>i.ApiKey, i=> i.MaxQuota  - usedQuota.GetValueOrDefault(i.ApiKey, 0));
        
        //remove on hold api keys
        var apiKeysOnHold = _apiKeyStatus
            .Where(pair => pair.Value.Status == ApiKeyStatus.OnHold)
            .Select(pair => pair.Key)
            .ToList();
        apiKeysOnHold.ForEach(key => remainingQuotaByApiKey.Remove(key));
        
        CleanUpExpiredCompletedRequests();

        var retryableFailedRequests = _masterTrackerState.State.FailedImageGenerationRequests
            .Select(i => new KeyValuePair<long, RequestAccountUsageInfo>(i.Value.FailedTimestamp + (long)Math.Min(Math.Pow(2, i.Value.Attempts), 8.0), i.Value))
            .Where(i => i.Key < now)
            .ToList();
        var startedRequests = _masterTrackerState.State.StartedImageGenerationRequests
            .Select(i => new KeyValuePair<long, RequestAccountUsageInfo>(i.Value.RequestTimestamp, i.Value))
            .ToList();
        
        var sortedRequests = retryableFailedRequests
            .Concat(startedRequests)
            .OrderBy(i => i.Key)
            .Select(i => i.Value)
            .ToList();

        var prunedSortedRequests = PruneRequestsWithMaxAttempts(sortedRequests);
        var processedRequests = await ProcessRequest(prunedSortedRequests, remainingQuotaByApiKey);
        
        var removedFailedRequests = RemoveProcessedRequests(_masterTrackerState.State.FailedImageGenerationRequests, processedRequests).ToHashSet();
        var removedStartedRequest = RemoveProcessedRequests(_masterTrackerState.State.StartedImageGenerationRequests, processedRequests).ToHashSet();

        //monitor for requests that are not removed
        var unremovedRequests = processedRequests.ToHashSet();
        unremovedRequests.ExceptWith(removedFailedRequests);
        unremovedRequests.ExceptWith(removedStartedRequest);
        
        if (unremovedRequests.Count > 0)
        {
            _logger.LogError($"[SchedulerGrain] Requests {string.Join(",", unremovedRequests)} not found in failed or started requests");
        }
    }

    #region Private Methods

    private void RefreshApiUsageInfo(string apiKey)
    {
        if(_apiKeyStatus.TryGetValue(apiKey, out var usageInfo))
        {
            usageInfo.Attempts = 0;
            usageInfo.LastUsedTimestamp = 0;
            usageInfo.Status = ApiKeyStatus.Active;
        }
        else
        {
            _logger.LogError($"[SchedulerGrain] API key: {apiKey} not found in usage info");
        }
    }
    
    private void UpdateApiUsageStatus()
    {
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        foreach (var usageInfo in _apiKeyStatus.Select(pair => pair.Value).Where(usageInfo => usageInfo.LastUsedTimestamp + (long)Math.Min(Math.Pow(3, usageInfo.Attempts), 27.0) < now))
        {
            usageInfo.Status = ApiKeyStatus.Active;
        }
    }
    
    private void HandleErrorCode(string apiKey, long lastUsedTimestamp, DalleErrorCode? errorCode)
    {
        if(errorCode == null)
            return;
        
        _logger.LogError($"[SchedulerGrain] Error code: {errorCode.ToString()} for API key: {apiKey}");
        
        var apiInfo = _masterTrackerState.State.ApiAccountInfoList.Find(info => info.ApiKey == apiKey);
        if (apiInfo == null)
        {
            _logger.LogError($"[SchedulerGrain] API key: {apiKey} not found in the list");
            return;
        }
        
        switch (errorCode)
        {
            case DalleErrorCode.rate_limit_reached:
            case DalleErrorCode.invalid_api_key:
                var isInvalidKey = errorCode == DalleErrorCode.invalid_api_key;
                var delay = isInvalidKey ? 86400 : 3600; // 1 day for invalid key, 1 hour for rate limit

                if (!_apiKeyStatus.TryGetValue(apiKey, out var usageInfo))
                {
                    usageInfo = new ApiKeyUsageInfo { ApiKey = apiKey };
                    _apiKeyStatus.Add(apiKey, usageInfo);
                }

                usageInfo.Attempts = isInvalidKey ? 0 : usageInfo.Attempts + 1;
                usageInfo.LastUsedTimestamp = lastUsedTimestamp + delay;
                usageInfo.Status = ApiKeyStatus.OnHold;
                break;
            default:
                if (_apiKeyStatus.TryGetValue(apiKey, out usageInfo))
                {
                    usageInfo.Attempts++;
                    usageInfo.LastUsedTimestamp = lastUsedTimestamp;
                    usageInfo.Status = ApiKeyStatus.OnHold;
                }
                else
                {
                    _apiKeyStatus.Add(apiKey, new ApiKeyUsageInfo
                    {
                        ApiKey = apiKey,
                        LastUsedTimestamp = lastUsedTimestamp,
                        Status = ApiKeyStatus.OnHold,
                        Attempts = 1
                    });
                }
                break;
        }
    }

    private async Task FlushTimerAsync()
    {
        await this.AsReference<ISchedulerGrain>().FlushAsync();
    }
    
    private static List<string> RemoveProcessedRequests(Dictionary<string, RequestAccountUsageInfo> requests, List<string> processedRequests)
    {
        var requestsRemoved = new List<string>();
        
        foreach (var requestId in processedRequests.Where(requests.ContainsKey))
        {
            requests.Remove(requestId);
            requestsRemoved.Add(requestId);
        }

        return requestsRemoved;
    }

    private void CleanUpExpiredCompletedRequests()
    {
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        
        //clean up completed requests
        foreach (var info in _masterTrackerState.State.CompletedImageGenerationRequests)
        {
            var difference = now - info.Value.StartedTimestamp;
            
            if (difference > CLEANUP_INTERVAL)
            {
                _masterTrackerState.State.CompletedImageGenerationRequests.Remove(info.Key);
            }
        }
    }
    
    private async Task<List<string>> ProcessRequest(IEnumerable<RequestAccountUsageInfo> sortedRequests, IDictionary<string, int> apiQuota)
    {
        List<string> requestIdToRemove = new();
        foreach (var info in sortedRequests)
        {
            var requestId = info.ChildId;
            
            var imageGenerationGrain = GrainFactory.GetGrain<IImageGeneratorGrain>(info.ChildId);
            if (imageGenerationGrain == null)
            {
                _logger.LogError($"[SchedulerGrain] Cannot find ImageGeneratorGrain with ID: {info.ChildId}");
                continue;
            }

            info.ApiKey = GetApiKey(apiQuota);
            
            // if there are no available api keys, we will try again in the next scheduling
            if (string.IsNullOrEmpty(info.ApiKey))
            {
                _logger.LogError("[SchedulerGrain] No available API keys, will try again in the next scheduling");
                break;
            }
            
            info.Attempts++;
            
            AlarmWhenLowOnQuota(apiQuota);
            
            info.StartedTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
            // Get child gen grain to process failed request again with the new api key
            await imageGenerationGrain.SetApiKey(info.ApiKey);
            
            // remove from list to add to pending
            _logger.LogWarning($"[SchedulerGrain] Request {requestId} is pending");
            _masterTrackerState.State.PendingImageGenerationRequests.Add(requestId, info);
            _logger.LogWarning($"[SchedulerGrain] Requested {requestId}");
            requestIdToRemove.Add(requestId);
        }

        return requestIdToRemove;
    }

    private List<RequestAccountUsageInfo> PruneRequestsWithMaxAttempts(IReadOnlyList<RequestAccountUsageInfo> requests)
    {
        var maxAttemptsReached = requests
            .Where(info => info.Attempts >= MAX_ATTEMPTS)
            .Select(p=>p.RequestId)
            .ToHashSet();
        if (maxAttemptsReached.Count > 0)
        {
            _logger.LogError($"Requests {string.Join(",", maxAttemptsReached)} has reached max attempts");
        }

        var validRequests = requests.ToList();
        validRequests.RemoveAll(info => maxAttemptsReached.Contains(info.RequestId));
        return validRequests;
    }
    
    private void AlarmWhenLowOnQuota(IDictionary<string, int> apiQuota)
    {
        var remainingQuota = apiQuota.Sum(pair => pair.Value);

        var apiInfoList = _masterTrackerState.State.ApiAccountInfoList;
        var totalQuota = apiInfoList.Sum(apiInfo => apiInfo.MaxQuota);

        if (remainingQuota / (float)totalQuota < QUOTA_THRESHOLD)
        {
            _logger.LogWarning($"[SchedulerGrain] API Keys low on quota, remaining quota: {remainingQuota}");
        }
    }

    private static string GetApiKey(IDictionary<string, int> apiQuota)
    {
       var (apiKey, quota)= apiQuota.MaxBy(pair => pair.Value);
        // var apiKey = FindKeyWithHighestValue(apiQuota);
        if (string.IsNullOrEmpty(apiKey) || quota <= 0)
        {
            return "";
        }
        
        apiQuota[apiKey] -= 1;
        return apiKey;
    }

    private RequestAccountUsageInfo PopFromPending(string requestId)
    {
        if (!_masterTrackerState.State.PendingImageGenerationRequests.ContainsKey(requestId))
        {
            _logger.LogError($"[SchedulerGrain] Request {requestId} not found in pending list");
            return null;
        }
        var info = _masterTrackerState.State.PendingImageGenerationRequests[requestId];
        _masterTrackerState.State.PendingImageGenerationRequests.Remove(requestId);
        return info;
    }

    void IDisposable.Dispose()
    {
        _timer?.Dispose();
        _flushTimer?.Dispose();
    }

    #endregion
}