using Grains.types;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Timers;
using Shared;

namespace Grains.usage_tracker;

/// <summary>
/// It keeps track of how many jobs have been started per each account in the last minute (or jobs started more than
/// one minute ago but haven't completed). It will compare this count against the account's quota and choose the least
/// loaded account for the next job.
/// </summary>
public class SchedulerGrain : Grain, ISchedulerGrain, IImageGenerationRequestStatusReceiver, IDisposable
{
    private const string ReminderName = "SchedulingReminder";
    private const long RATE_LIMIT_DURATION = 60;
    private const long CLEANUP_INTERVAL = 180;
    private const int MAX_ATTEMPTS = 99999;
    private const float QUOTA_THRESHOLD = 0.15f;

    private readonly IReminderRegistry _reminderRegistry;
    private readonly IPersistentState<SchedulerState> _masterTrackerState;
    private readonly ILogger<SchedulerGrain> _logger;
    
    private IGrainReminder? _reminder;
    private IDisposable? _timer;

    public SchedulerGrain(
        [PersistentState("masterTrackerState", "MySqlSchrodingerImageStore")]
        IPersistentState<SchedulerState> masterTrackerState,
        //IReminderRegistry reminderRegistry,
        ILogger<SchedulerGrain> logger)
    {
        //_reminderRegistry = reminderRegistry;
        _logger = logger;
        
        _masterTrackerState = masterTrackerState;
    }
    
    public override Task OnActivateAsync()
    {
        _timer = RegisterTimer(asyncCallback: static async state =>
            {
                var scheduler = (SchedulerGrain)state;
                await scheduler.DoScheduling();

                await Task.CompletedTask;
            },
            state: this,
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

    public async Task ReportFailedImageGenerationRequestAsync(RequestStatus requestStatus)
    {
        var info = PopFromPending(requestStatus.RequestId);
        var unixTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        info.FailedTimestamp = unixTimestamp;
        _masterTrackerState.State.FailedImageGenerationRequests.Add(requestStatus.RequestId, info);
        await _masterTrackerState.WriteStateAsync();
    }

    public async Task ReportCompletedImageGenerationRequestAsync(RequestStatus requestStatus)
    {
        var info = PopFromPending(requestStatus.RequestId);
        var unixTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        info.CompletedTimestamp = unixTimestamp;
        _masterTrackerState.State.CompletedImageGenerationRequests.Add(requestStatus.RequestId, info);
        await _masterTrackerState.WriteStateAsync();
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

    public async Task AddImageGenerationRequest(string requestId, string childId, long requestTimestamp)
    {
        //TODO: childId
        _masterTrackerState.State.StartedImageGenerationRequests.Add(requestId, new RequestAccountUsageInfo
        {
            RequestId = requestId,
            RequestTimestamp = requestTimestamp,
            Attempts = 0
        });
        
        await _masterTrackerState.WriteStateAsync();
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

    #region Private Methods

    private async Task DoScheduling()
    {
        // 1. Purge completed requests
        // 2. Add failed requests to pending queue
        // 3. Check remaining quota for all accounts
        // 4. For all pending tasks, find the account with the most remaining quota
        // 5. Schedule the task, update the account usage info
        
        Dictionary<string, int> apiQuota = new();
        
        if(_masterTrackerState.State.ApiAccountInfoList == null)
            return;
        
        foreach (var apiInfo in _masterTrackerState.State.ApiAccountInfoList)
        {
            apiQuota[apiInfo.ApiKey] = apiInfo.MaxQuota;
        }
        
        ComputeApiQuotaFromTimestamp(apiQuota, _masterTrackerState.State.CompletedImageGenerationRequests);
        ComputeApiQuotaFromTimestamp(apiQuota, _masterTrackerState.State.FailedImageGenerationRequests);
        ComputeApiQuotaFromTimestamp(apiQuota, _masterTrackerState.State.PendingImageGenerationRequests);
        
        CleanUpExpiredCompletedRequests();

        ProcessRequest(_masterTrackerState.State.FailedImageGenerationRequests, apiQuota);
        ProcessRequest(_masterTrackerState.State.StartedImageGenerationRequests, apiQuota);

        await _masterTrackerState.WriteStateAsync();
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

    private void ComputeApiQuotaFromTimestamp(Dictionary<string, int> apiQuota, Dictionary<string, RequestAccountUsageInfo> requests)
    {
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        
        foreach (var info in _masterTrackerState.State.FailedImageGenerationRequests)
        {
            var difference = now - info.Value.StartedTimestamp;
            //if difference is more than 60 seconds, we can remove it
            if (difference > RATE_LIMIT_DURATION)
            {
                continue;
            }
            
            apiQuota[info.Value.ApiKey]--;
        }
    }

    private void ProcessRequest(IDictionary<string, RequestAccountUsageInfo> requests, Dictionary<string, int> apiQuota)
    {
        List<string> requestIdToRemove = new();
        foreach (var (requestId, info) in requests)
        {
            if (info.Attempts > MAX_ATTEMPTS)
            {
                requestIdToRemove.Add(requestId);
                _logger.LogError("Request " + requestId + " has reached max attempts");
                continue;
            }
            
            info.Attempts++;

            info.ApiKey = GetApiKey(apiQuota);
            
            // if there are no available api keys, we will try again in the next scheduling
            if (string.IsNullOrEmpty(info.ApiKey))
            {
                _logger.LogError("No available API keys, will try again in the next scheduling");
                break;
            }
            
            AlarmWhenLowOnQuota(apiQuota);
            
            info.StartedTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
            // TODO: get child gen grain to process failed request again with the new api key

            // remove from list to add to pending
            _masterTrackerState.State.PendingImageGenerationRequests.Add(requestId, info);
            requestIdToRemove.Add(requestId);
        }

        foreach (var requestId in requestIdToRemove)
        {
            requests.Remove(requestId);
        }
    }
    
    private void AlarmWhenLowOnQuota(Dictionary<string, int> apiQuota)
    {
        var remainingQuota = 0;
        foreach (var pair in apiQuota)
        {
            remainingQuota += pair.Value;
        }

        var totalQuota = 0;
        _masterTrackerState.State.ApiAccountInfoList.ForEach(apiInfo =>
        {
            totalQuota += apiInfo.MaxQuota;
        });

        if (remainingQuota / (float)totalQuota < QUOTA_THRESHOLD)
        {
            _logger.LogWarning("API Keys low on quota, remaining quota: " + remainingQuota);
        }
    }

    private static string GetApiKey(IDictionary<string, int> apiQuota)
    {
       var (apiKey, _)= apiQuota.MaxBy(pair => pair.Value);
        // var apiKey = FindKeyWithHighestValue(apiQuota);
        if (string.IsNullOrEmpty(apiKey))
        {
            return "";
        }
        
        apiQuota[apiKey] -= 1;
        return apiKey;
    }

    private static string FindKeyWithHighestValue(Dictionary<string, int> dictionary)
    {
        if (dictionary == null || dictionary.Count == 0)
            throw new ArgumentException("Dictionary is empty or null");

        var keyWithHighestValue = "";
        var highestValue = int.MinValue;

        foreach (var kvp in dictionary)
        {
            if (kvp.Value <= highestValue) continue;
            
            highestValue = kvp.Value;
            keyWithHighestValue = kvp.Key;
        }

        return keyWithHighestValue;
    }

    private RequestAccountUsageInfo PopFromPending(string requestId)
    {
        var info = _masterTrackerState.State.PendingImageGenerationRequests[requestId];
        _masterTrackerState.State.PendingImageGenerationRequests.Remove(requestId);
        return info;
    }
    
    //keep alive TODO
    /*public async Task Ping()
    {
        _reminder = await _reminderRegistry.RegisterOrUpdateReminder(
            reminderName: ReminderName,
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromHours(1));
    }*/

    void IDisposable.Dispose()
    {
        /*if (_reminder is not null)
        {
            _reminderRegistry.UnregisterReminder(_reminder);
        }*/
        
        _timer?.Dispose();
    }

    #endregion
}