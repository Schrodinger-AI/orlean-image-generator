using System.Globalization;
using Grains.types;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Timers;
using Schrodinger.Backend.Abstractions.AccountUsage;
using Schrodinger.Backend.Abstractions.ApiKeys;
using Schrodinger.Backend.Abstractions.Constants;
using Schrodinger.Backend.Abstractions.Interfaces;
using Schrodinger.Backend.Abstractions.UsageTracker;

// ReSharper disable TooManyChainedReferences

namespace Grains.usage_tracker;

/// <summary>
/// It keeps track of how many jobs have been started per each account in the last minute (or jobs started more than
/// one minute ago but haven't completed). It will compare this count against the account's quota and choose the least
/// loaded account for the next job.
/// </summary>
[KeepAlive]
public class SchedulerGrain : Grain, ISchedulerGrain, IDisposable, IRemindable
{
    private const string REMINDER_NAME = "SchedulerReminder";
    public const long RATE_LIMIT_DURATION = 63; // 1 minute and 3 seconds, 3 seconds for the buffer
    public const long CLEANUP_INTERVAL = 180; // 3 minutes
    public const int MAX_ATTEMPTS = 99999;
    public const float QUOTA_THRESHOLD = 0.2f;
    public const long PENDING_EXPIRY_THRESHOLD = 43200; // 12 hours

    private readonly IPersistentState<SchedulerState> _masterTrackerState;
    private readonly ILogger<SchedulerGrain> _logger;
    private readonly IReminderRegistry _reminderRegistry;
    private readonly utilities.TimeProvider _timeProvider;

    private IGrainReminder? _reminder;
    private IDisposable? _timer;
    private IDisposable? _flushTimer;

    private readonly Dictionary<string, ApiKeyUsageInfo> _apiKeyStatus = new();

    public SchedulerGrain(
        [PersistentState("masterTrackerState", "MySqlSchrodingerImageStore")]
            IPersistentState<SchedulerState> masterTrackerState,
        IReminderRegistry reminderRegistry,
        utilities.TimeProvider timeProvider,
        ILogger<SchedulerGrain> logger
    )
    {
        _reminderRegistry = reminderRegistry;
        _logger = logger;
        _masterTrackerState = masterTrackerState;
        _timeProvider = timeProvider;
    }

    public new virtual IGrainFactory GrainFactory
    {
        get => base.GrainFactory;
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

    public override async Task OnActivateAsync(
        CancellationToken cancellationToken
    )
    {
        _timer = RegisterTimer(
            asyncCallback: _ => this.AsReference<ISchedulerGrain>().TickAsync(),
            null,
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromSeconds(1)
        );

        _flushTimer = RegisterTimer(
            asyncCallback: _ => FlushTimerAsync(),
            null,
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromSeconds(1)
        );

        CleanUpPendingRequests();

        _reminder = await _reminderRegistry.RegisterOrUpdateReminder(
            callingGrainId: this.GetGrainId(),
            reminderName: REMINDER_NAME,
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromMinutes(5)
        );

        await base.OnActivateAsync(cancellationToken);
    }

    Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
    {
        _logger.LogInformation(
            $"SchedulerGrain ({reminderName}) has been reminded!"
        );
        return Task.CompletedTask;
    }

    public Task ReportFailedImageGenerationRequestAsync(
        RequestStatus requestStatus
    )
    {
        _logger.LogError(
            $"[SchedulerGrain] Image generation failed with message: {requestStatus.Message}"
        );
        var info = PopFromPending(requestStatus.RequestId);
        if (info == null)
        {
            return Task.CompletedTask;
        }
        var unixTimestamp = (
            (DateTimeOffset)_timeProvider.UtcNow
        ).ToUnixTimeSeconds();
        info.FailedTimestamp = unixTimestamp;
        info.StartedTimestamp =
            (requestStatus.RequestTimestamp == 0)
                ? info.StartedTimestamp
                : requestStatus.RequestTimestamp;
        _masterTrackerState.State.FailedImageGenerationRequests.Add(
            requestStatus.RequestId,
            info
        );

        HandleErrorCode(
            info.ApiKey,
            info.StartedTimestamp,
            requestStatus.ErrorCode
        );

        return Task.CompletedTask;
    }

    public Task ReportCompletedImageGenerationRequestAsync(
        RequestStatus requestStatus
    )
    {
        _logger.LogInformation(
            $"[SchedulerGrain] Report Completed Image Generation Request with ID: {requestStatus.RequestId}"
        );

        var info = PopFromPending(requestStatus.RequestId);
        if (info == null)
        {
            return Task.CompletedTask;
        }

        var unixTimestamp = (
            (DateTimeOffset)_timeProvider.UtcNow
        ).ToUnixTimeSeconds();
        info.CompletedTimestamp = unixTimestamp;
        info.StartedTimestamp = requestStatus.RequestTimestamp;
        _masterTrackerState.State.CompletedImageGenerationRequests.Add(
            requestStatus.RequestId,
            info
        );

        ResetApiUsageInfo(info.ApiKey);

        return Task.CompletedTask;
    }

    public Task ReportBlockedImageGenerationRequestAsync(
        RequestStatus requestStatus
    )
    {
        var info = PopFromPending(requestStatus.RequestId);
        if (info == null)
        {
            return Task.CompletedTask;
        }

        info.FailedTimestamp = (
            (DateTimeOffset)_timeProvider.UtcNow
        ).ToUnixTimeSeconds();
        info.StartedTimestamp =
            (requestStatus.RequestTimestamp == 0)
                ? info.StartedTimestamp
                : requestStatus.RequestTimestamp;

        var blockedRequest = new BlockedRequestInfo()
        {
            RequestAccountUsageInfo = info,
            BlockedReason = requestStatus.ErrorCode?.ToString()
        };

        _masterTrackerState.State.BlockedImageGenerationRequests.Add(
            requestStatus.RequestId,
            blockedRequest
        );

        return Task.CompletedTask;
    }

    public Task<
        List<RequestAccountUsageInfoDto>
    > GetFailedImageGenerationRequestsAsync()
    {
        var ret = GetRequestAccountUsageInfoDtoList(
            _masterTrackerState.State.FailedImageGenerationRequests
        );
        return Task.FromResult(ret.ToList());
    }

    public Task<
        List<RequestAccountUsageInfoDto>
    > GetStartedImageGenerationRequestsAsync()
    {
        var ret = GetRequestAccountUsageInfoDtoList(
            _masterTrackerState.State.StartedImageGenerationRequests
        );
        return Task.FromResult(ret.ToList());
    }

    public Task<
        List<RequestAccountUsageInfoDto>
    > GetPendingImageGenerationRequestsAsync()
    {
        var ret = GetRequestAccountUsageInfoDtoList(
            _masterTrackerState.State.PendingImageGenerationRequests
        );
        return Task.FromResult(ret.ToList());
    }

    public Task<
        List<RequestAccountUsageInfoDto>
    > GetCompletedImageGenerationRequestsAsync()
    {
        var ret = GetRequestAccountUsageInfoDtoList(
            _masterTrackerState.State.CompletedImageGenerationRequests
        );
        return Task.FromResult(ret.ToList());
    }

    public Task<
        List<BlockedRequestInfoDto>
    > GetBlockedImageGenerationRequestsAsync()
    {
        var requestList = new List<BlockedRequestInfo>(
            _masterTrackerState.State.BlockedImageGenerationRequests.Values
        );
        var result = requestList.Select(i => new BlockedRequestInfoDto
        {
            BlockedReason = i.BlockedReason?.ToString(),
            RequestInfo = new RequestAccountUsageInfoDto
            {
                RequestId = i.RequestAccountUsageInfo.RequestId,
                RequestTimestamp = UnixTimeStampInSecondsToDateTime(
                        i.RequestAccountUsageInfo.RequestTimestamp
                    )
                    .ToString(CultureInfo.InvariantCulture),
                StartedTimestamp = UnixTimeStampInSecondsToDateTime(
                        i.RequestAccountUsageInfo.StartedTimestamp
                    )
                    .ToString(CultureInfo.InvariantCulture),
                FailedTimestamp = UnixTimeStampInSecondsToDateTime(
                        i.RequestAccountUsageInfo.FailedTimestamp
                    )
                    .ToString(CultureInfo.InvariantCulture),
                CompletedTimestamp = UnixTimeStampInSecondsToDateTime(
                        i.RequestAccountUsageInfo.CompletedTimestamp
                    )
                    .ToString(CultureInfo.InvariantCulture),
                Attempts = i.RequestAccountUsageInfo.Attempts,
                ApiKey =
                    i.RequestAccountUsageInfo.ApiKey == null
                        ? null
                        : new ApiKeyDto(i.RequestAccountUsageInfo.ApiKey),
                ChildId = i.RequestAccountUsageInfo.ChildId,
            }
        });

        return Task.FromResult(result.ToList());
    }

    public Task AddImageGenerationRequest(
        string requestId,
        string childId,
        long requestTimestamp
    )
    {
        _logger.LogInformation(
            $"[SchedulerGrain] Adding image generation request with ID: {requestId} for child ID: {childId}"
        );
        _masterTrackerState.State.StartedImageGenerationRequests.Add(
            childId,
            new RequestAccountUsageInfo
            {
                RequestId = requestId,
                RequestTimestamp = requestTimestamp,
                Attempts = 0,
                ChildId = childId
            }
        );

        return Task.CompletedTask;
    }

    public async Task<AddApiKeysResponseDto> AddApiKeys(
        List<ApiKeyEntryDto> apiKeyEntries
    )
    {
        try
        {
            // duplicateAPiKey ValidationLogic
            // Create an empty list for valid API keys and another for invalid API keys.
            // If the API key does not exist in the state, add it to the valid API keys list.
            // If the invalid API keys list is not empty, then return new AddApiKeysResponseNotOk(invalidApiKeys)

            var apiKeyDict =
                _masterTrackerState.State.ApiAccountInfoList.ToDictionary(
                    info => info.ApiKey.GetConcatApiKeyString(),
                    info => info
                );
            var apiKeyToAdd = apiKeyEntries
                .Select(entry => new APIAccountInfo
                {
                    ApiKey = new ApiKey(
                        entry.ApiKey.ApiKeyString,
                        entry.ApiKey.ServiceProvider,
                        entry.ApiKey.Url
                    ),
                    Email = entry.Email,
                    Tier = entry.Tier,
                    MaxQuota = entry.MaxQuota
                })
                .ToList();

            // Check for duplicate API keys in apiKeyDict from accountEntries
            var duplicateKeys = (
                from entry in apiKeyToAdd
                where
                    apiKeyDict.ContainsKey(entry.ApiKey.GetConcatApiKeyString())
                select entry.ApiKey.GetConcatApiKeyString()
            ).ToList();
            apiKeyToAdd.RemoveAll(entry =>
                duplicateKeys.Contains(entry.ApiKey.GetConcatApiKeyString())
            );

            // get APIAccountInfo from apiKeyDict for keys in duplicateKeys
            var duplicateApiKeys = duplicateKeys
                .Select(key => apiKeyDict[key])
                .Select(info => new ApiKeyDto(info.ApiKey))
                .ToList();

            // If all API keys are duplicates, return an error
            if (apiKeyToAdd.Count == 0)
            {
                return new AddApiKeysResponseDto
                {
                    IsSuccessful = false,
                    ValidApiKeys = [],
                    Error = "DUPLICATE_API_KEYS",
                    DuplicateApiKeys = duplicateApiKeys
                };
            }

            _masterTrackerState.State.ApiAccountInfoList.AddRange(apiKeyToAdd);
            await _masterTrackerState.WriteStateAsync();

            return new AddApiKeysResponseDto
            {
                IsSuccessful = true,
                ValidApiKeys = apiKeyToAdd
                    .Select(entry => new ApiKeyDto(entry.ApiKey))
                    .ToList(),
                DuplicateApiKeys = duplicateApiKeys
            };
        }
        catch (Exception e)
        {
            return new AddApiKeysResponseDto
            {
                IsSuccessful = false,
                Error = e.Message
            };
        }
    }

    //returns a list of apikeys that were removed
    public async Task<List<ApiKey>> RemoveApiKeys(List<ApiKey> apiKey)
    {
        List<ApiKey> removedApiKeys = [];

        _masterTrackerState.State.ApiAccountInfoList.RemoveAll(apiInfo =>
        {
            if (
                apiKey.Any(key =>
                    key.GetConcatApiKeyString()
                    == apiInfo.ApiKey.GetConcatApiKeyString()
                )
            )
            {
                removedApiKeys.Add(apiInfo.ApiKey);
                return true;
            }

            return false;
        });

        await _masterTrackerState.WriteStateAsync();

        return removedApiKeys;
    }

    public Task<IReadOnlyList<ApiKeyEntryDto>> GetAllApiKeys()
    {
        var ret = new List<ApiKeyEntryDto>();
        foreach (var info in _masterTrackerState.State.ApiAccountInfoList)
        {
            var newInfo = new ApiKeyEntryDto
            {
                ApiKey = new ApiKeyDto(info.ApiKey),
                Email = info.Email,
                MaxQuota = info.MaxQuota,
                Tier = info.Tier
            };
            ret.Add(newInfo);
        }

        return Task.FromResult<IReadOnlyList<ApiKeyEntryDto>>(ret);
    }

    public Task<
        Dictionary<string, List<RequestAccountUsageInfoDto>>
    > GetImageGenerationStates()
    {
        var startedImageGenerationRequests = GetRequestAccountUsageInfoDtoList(
            _masterTrackerState.State.StartedImageGenerationRequests
        );
        var pendingImageGenerationRequests = GetRequestAccountUsageInfoDtoList(
            _masterTrackerState.State.PendingImageGenerationRequests
        );
        var completedImageGenerationRequests =
            GetRequestAccountUsageInfoDtoList(
                _masterTrackerState.State.CompletedImageGenerationRequests
            );
        var failedImageGenerationRequests = GetRequestAccountUsageInfoDtoList(
            _masterTrackerState.State.FailedImageGenerationRequests
        );

        var imageGenerationRequests = new Dictionary<
            string,
            List<RequestAccountUsageInfoDto>
        >()
        {
            { "startedRequests", startedImageGenerationRequests.ToList() },
            { "pendingRequests", pendingImageGenerationRequests.ToList() },
            { "completedRequests", completedImageGenerationRequests.ToList() },
            { "failedRequests", failedImageGenerationRequests.ToList() }
        };

        return Task.FromResult(imageGenerationRequests);
    }

    public Task<Dictionary<string, ApiKeyUsageInfo>> GetApiKeysUsageInfo()
    {
        return Task.FromResult(_apiKeyStatus);
    }

    public Task<bool> IsOverloaded()
    {
        return Task.FromResult(IsCurrentlyOverloaded());
    }

    public async Task TickAsync()
    {
        // 1. Purge completed requests
        // 2. Add failed requests to pending queue
        // 3. Check remaining quota for all accounts
        // 4. For all pending tasks, find the account with the most remaining quota
        // 5. Schedule the task, update the account usage info

        UpdateApiUsageStatus();
        UpdateExpiredPendingRequestsToBlocked();

        if (_masterTrackerState.State.ApiAccountInfoList.Count == 0)
        {
            return;
        }

        var now = ((DateTimeOffset)_timeProvider.UtcNow).ToUnixTimeSeconds();

        var remainingQuotaByApiKey = GetRemainingQuotaByApiKeys();

        CleanUpExpiredCompletedRequests();

        var retryableFailedRequests = _masterTrackerState
            .State.FailedImageGenerationRequests.Select(i => new KeyValuePair<
                long,
                RequestAccountUsageInfo
            >(
                i.Value.FailedTimestamp
                    + (long)Math.Min(Math.Pow(2, i.Value.Attempts), 8.0),
                i.Value
            ))
            .Where(i => i.Key < now)
            .ToList();
        var startedRequests = _masterTrackerState
            .State.StartedImageGenerationRequests.Select(i => new KeyValuePair<
                long,
                RequestAccountUsageInfo
            >(i.Value.RequestTimestamp, i.Value))
            .ToList();

        var sortedRequests = retryableFailedRequests
            .Concat(startedRequests)
            .OrderBy(i => i.Key)
            .Select(i => i.Value)
            .ToList();

        var prunedSortedRequests = PruneRequestsWithMaxAttempts(sortedRequests);
        var processedRequests = await ProcessRequest(
            prunedSortedRequests,
            remainingQuotaByApiKey
        );

        var removedFailedRequests = RemoveProcessedRequests(
                _masterTrackerState.State.FailedImageGenerationRequests,
                processedRequests
            )
            .ToHashSet();
        var removedStartedRequest = RemoveProcessedRequests(
                _masterTrackerState.State.StartedImageGenerationRequests,
                processedRequests
            )
            .ToHashSet();

        //monitor for requests that are not removed
        var unremovedRequests = processedRequests.ToHashSet();
        unremovedRequests.ExceptWith(removedFailedRequests);
        unremovedRequests.ExceptWith(removedStartedRequest);

        if (unremovedRequests.Count > 0)
        {
            _logger.LogError(
                $"[SchedulerGrain] Requests {string.Join(",", unremovedRequests)} not found in failed or started requests"
            );
        }
    }

    public Task<bool> ForceRequestExecution(string childId)
    {
        RequestAccountUsageInfo? requestInfo = null;
        if (
            _masterTrackerState.State.PendingImageGenerationRequests.ContainsKey(
                childId
            )
        )
        {
            requestInfo = _masterTrackerState
                .State
                .PendingImageGenerationRequests[childId];
            _masterTrackerState.State.PendingImageGenerationRequests.Remove(
                childId
            );
        }
        else if (
            _masterTrackerState.State.BlockedImageGenerationRequests.ContainsKey(
                childId
            )
        )
        {
            requestInfo = _masterTrackerState
                .State
                .BlockedImageGenerationRequests[childId]
                .RequestAccountUsageInfo;
            _masterTrackerState.State.BlockedImageGenerationRequests.Remove(
                childId
            );
        }

        // not found in pending or blocked
        if (requestInfo == null)
        {
            return Task.FromResult(false);
        }

        _masterTrackerState.State.StartedImageGenerationRequests.Add(
            childId,
            requestInfo
        );
        return Task.FromResult(true);
    }

    #region Private Methods

    private void CleanUpPendingRequests()
    {
        _masterTrackerState
            .State.PendingImageGenerationRequests.ToList()
            .ForEach(ActivateImageGeneratorGrain);

        return;

        async void ActivateImageGeneratorGrain(
            KeyValuePair<string, RequestAccountUsageInfo> request
        )
        {
            var imageGenerationGrain =
                GrainFactory.GetGrain<IImageGeneratorGrain>(
                    request.Value.ChildId
                );
            await imageGenerationGrain.Activate();
        }
    }

    private void UpdateExpiredPendingRequestsToBlocked()
    {
        var now = ((DateTimeOffset)_timeProvider.UtcNow).ToUnixTimeSeconds();
        var cutoff = now - PENDING_EXPIRY_THRESHOLD;
        var expiredPendingRequests = _masterTrackerState
            .State.PendingImageGenerationRequests.Where(i =>
                i.Value.StartedTimestamp < cutoff
            )
            .ToList();
        foreach (var (requestId, info) in expiredPendingRequests)
        {
            _masterTrackerState.State.PendingImageGenerationRequests.Remove(
                requestId
            );
            _masterTrackerState.State.BlockedImageGenerationRequests.Add(
                requestId,
                new BlockedRequestInfo
                {
                    RequestAccountUsageInfo = info,
                    BlockedReason = "Pending request expired"
                }
            );
        }
    }

    private Dictionary<string, int> GetRemainingQuotaByApiKeys()
    {
        var now = ((DateTimeOffset)_timeProvider.UtcNow).ToUnixTimeSeconds();

        //get all requests that are within the rate limit duration
        var cutoff = now - RATE_LIMIT_DURATION;
        var allRequests = _masterTrackerState
            .State.CompletedImageGenerationRequests.Concat(
                _masterTrackerState.State.FailedImageGenerationRequests
            )
            .Concat(_masterTrackerState.State.PendingImageGenerationRequests)
            .Concat(_masterTrackerState.State.StartedImageGenerationRequests)
            .ToList();
        var usedQuota = allRequests
            .Where(i => i.Value.StartedTimestamp > cutoff)
            .GroupBy(x => x.Value.ApiKey?.GetConcatApiKeyString(), x => x)
            .ToDictionary(x => x.Key, x => x.Count());
        var remainingQuotaByApiKey =
            _masterTrackerState.State.ApiAccountInfoList.ToDictionary(
                i => i.ApiKey.GetConcatApiKeyString(),
                i =>
                    i.MaxQuota
                    - usedQuota.GetValueOrDefault(
                        i.ApiKey.GetConcatApiKeyString(),
                        0
                    )
            );

        //remove on hold api keys
        var apiKeysOnHold = _apiKeyStatus
            .Where(pair => pair.Value.Status == ApiKeyStatus.OnHold)
            .Select(pair => pair.Key)
            .ToList();
        apiKeysOnHold.ForEach(key => remainingQuotaByApiKey.Remove(key));

        return remainingQuotaByApiKey;
    }

    private void ResetApiUsageInfo(ApiKey apiKey)
    {
        if (
            _apiKeyStatus.TryGetValue(
                apiKey.GetConcatApiKeyString(),
                out var usageInfo
            )
        )
        {
            usageInfo.Attempts = 0;
            usageInfo.LastUsedTimestamp = 0;
            usageInfo.Status = ApiKeyStatus.Active;
            usageInfo.ErrorCode = null;
        }
        else
        {
            _logger.LogWarning(
                $"[SchedulerGrain] API key: {apiKey.ApiKeyString} not found in usage info"
            );
        }
    }

    private void UpdateApiUsageStatus()
    {
        var now = ((DateTimeOffset)_timeProvider.UtcNow).ToUnixTimeSeconds();
        foreach (
            var usageInfo in _apiKeyStatus
                .Select(pair => pair.Value)
                .Where(usageInfo => usageInfo.GetReactivationTimestamp() < now)
        )
        {
            usageInfo.Status = ApiKeyStatus.Active;
        }
    }

    private void HandleErrorCode(
        ApiKey apiKey,
        long lastUsedTimestamp,
        ImageGenerationErrorCode? errorCode
    )
    {
        if (errorCode == null)
            return;

        _logger.LogError(
            $"[SchedulerGrain] Error code: {errorCode.ToString()} for API key: {apiKey.ApiKeyString}"
        );

        var apiInfo = _masterTrackerState.State.ApiAccountInfoList.Find(info =>
            info.ApiKey.GetConcatApiKeyString()
            == apiKey.GetConcatApiKeyString()
        );
        if (apiInfo == null)
        {
            _logger.LogError(
                $"[SchedulerGrain] API key: {apiKey} not found in the list"
            );
            return;
        }

        if (
            !_apiKeyStatus.TryGetValue(
                apiKey.GetConcatApiKeyString(),
                out var usageInfo
            )
        )
        {
            usageInfo = new ApiKeyUsageInfo { ApiKey = apiKey };
            _apiKeyStatus.Add(apiKey.GetConcatApiKeyString(), usageInfo);
        }

        usageInfo.Attempts = errorCode
            is ImageGenerationErrorCode.rate_limit_reached
                or ImageGenerationErrorCode.invalid_api_key
            ? 1
            : usageInfo.Attempts + 1;
        usageInfo.LastUsedTimestamp = lastUsedTimestamp;
        usageInfo.Status = ApiKeyStatus.OnHold;
        usageInfo.ErrorCode = errorCode;
    }

    private async Task FlushTimerAsync()
    {
        await this.AsReference<ISchedulerGrain>().FlushAsync();
    }

    private static List<string> RemoveProcessedRequests(
        Dictionary<string, RequestAccountUsageInfo> requests,
        List<string> processedRequests
    )
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
        var now = ((DateTimeOffset)_timeProvider.UtcNow).ToUnixTimeSeconds();

        //clean up completed requests
        foreach (
            var info in _masterTrackerState
                .State
                .CompletedImageGenerationRequests
        )
        {
            var difference = now - info.Value.StartedTimestamp;

            if (difference > CLEANUP_INTERVAL)
            {
                _masterTrackerState.State.CompletedImageGenerationRequests.Remove(
                    info.Key
                );
            }
        }
    }

    private async Task<List<string>> ProcessRequest(
        IEnumerable<RequestAccountUsageInfo> sortedRequests,
        IDictionary<string, int> apiQuota
    )
    {
        List<string> requestIdToRemove = new();
        foreach (var info in sortedRequests)
        {
            var requestId = info.ChildId;

            var imageGenerationGrain =
                GrainFactory.GetGrain<IImageGeneratorGrain>(info.ChildId);
            if (imageGenerationGrain == null)
            {
                _logger.LogError(
                    $"[SchedulerGrain] Cannot find ImageGeneratorGrain with ID: {info.ChildId}"
                );
                continue;
            }

            var selectedApiKey = GetApiKey(apiQuota);
            if (
                selectedApiKey == null
                || string.IsNullOrEmpty(selectedApiKey.ApiKeyString)
            )
            {
                _logger.LogWarning(
                    "[SchedulerGrain] No available API keys, will try again in the next scheduling"
                );
                break;
            }

            info.ApiKey = selectedApiKey;
            info.Attempts++;

            AlarmWhenLowOnQuota(apiQuota);

            info.StartedTimestamp = (
                (DateTimeOffset)_timeProvider.UtcNow
            ).ToUnixTimeSeconds();
            // Get child gen grain to process failed request again with the new api key
            await imageGenerationGrain.SetImageGenerationServiceProvider(
                info.ApiKey
            );

            // remove from list to add to pending
            _logger.LogWarning(
                $"[SchedulerGrain] Request {requestId} is pending"
            );
            _masterTrackerState.State.PendingImageGenerationRequests.Add(
                requestId,
                info
            );
            _logger.LogWarning($"[SchedulerGrain] Requested {requestId}");
            requestIdToRemove.Add(requestId);
        }

        return requestIdToRemove;
    }

    private List<RequestAccountUsageInfo> PruneRequestsWithMaxAttempts(
        IReadOnlyList<RequestAccountUsageInfo> requests
    )
    {
        var maxAttemptsReached = requests
            .Where(info => info.Attempts >= MAX_ATTEMPTS)
            .Select(p => p.RequestId)
            .ToHashSet();
        if (maxAttemptsReached.Count > 0)
        {
            _logger.LogError(
                $"Requests {string.Join(",", maxAttemptsReached)} has reached max attempts"
            );
        }

        var validRequests = requests.ToList();
        validRequests.RemoveAll(info =>
            maxAttemptsReached.Contains(info.RequestId)
        );
        return validRequests;
    }

    private void AlarmWhenLowOnQuota(IDictionary<string, int> apiQuota)
    {
        var remainingQuota = apiQuota.Sum(pair => pair.Value);
        var totalQuota = GetTotalApiKeyQuota();

        if (remainingQuota / (float)totalQuota < QUOTA_THRESHOLD)
        {
            _logger.LogWarning(
                $"[SchedulerGrain] API Keys low on quota, remaining quota: {remainingQuota}"
            );
        }
    }

    private bool IsCurrentlyOverloaded()
    {
        var apiQuota = GetRemainingQuotaByApiKeys();

        var remainingQuota = apiQuota.Sum(pair => pair.Value);
        var totalQuota = GetTotalApiKeyQuota();

        return remainingQuota / (float)totalQuota < QUOTA_THRESHOLD;
    }

    private int GetTotalApiKeyQuota()
    {
        var apiInfoList = _masterTrackerState.State.ApiAccountInfoList;
        var totalQuota = apiInfoList.Sum(apiInfo => apiInfo.MaxQuota);

        return totalQuota;
    }

    private ApiKey? GetApiKey(IDictionary<string, int> apiQuota)
    {
        var (apiKey, quota) = apiQuota.MaxBy(pair => pair.Value);
        if (string.IsNullOrEmpty(apiKey) || quota <= 0)
        {
            return null;
        }

        var apiAccountInfo = _masterTrackerState.State.ApiAccountInfoList.Find(
            info => info.ApiKey.GetConcatApiKeyString() == apiKey
        );
        if (apiAccountInfo == null)
        {
            return null;
        }

        apiQuota[apiKey] -= 1;
        return apiAccountInfo.ApiKey;
    }

    private RequestAccountUsageInfo? PopFromPending(string requestId)
    {
        if (
            !_masterTrackerState.State.PendingImageGenerationRequests.ContainsKey(
                requestId
            )
        )
        {
            _logger.LogWarning(
                $"[SchedulerGrain] Request {requestId} not found in pending list"
            );
            return null;
        }
        var info = _masterTrackerState.State.PendingImageGenerationRequests[
            requestId
        ];
        _masterTrackerState.State.PendingImageGenerationRequests.Remove(
            requestId
        );
        return info;
    }

    void IDisposable.Dispose()
    {
        _timer?.Dispose();
        _flushTimer?.Dispose();

        if (_reminder is not null)
        {
            _reminderRegistry.UnregisterReminder(this.GetGrainId(), _reminder);
        }
    }

    private static IEnumerable<RequestAccountUsageInfoDto> GetRequestAccountUsageInfoDtoList(
        Dictionary<string, RequestAccountUsageInfo> requests
    )
    {
        var imageGenerationRequests = new List<RequestAccountUsageInfo>(
            requests.Values
        );
        var ret = imageGenerationRequests.Select(
            i => new RequestAccountUsageInfoDto
            {
                RequestId = i.RequestId,
                RequestTimestamp = UnixTimeStampInSecondsToDateTime(
                        i.RequestTimestamp
                    )
                    .ToString(CultureInfo.InvariantCulture),
                StartedTimestamp = UnixTimeStampInSecondsToDateTime(
                        i.StartedTimestamp
                    )
                    .ToString(CultureInfo.InvariantCulture),
                FailedTimestamp = UnixTimeStampInSecondsToDateTime(
                        i.FailedTimestamp
                    )
                    .ToString(CultureInfo.InvariantCulture),
                CompletedTimestamp = UnixTimeStampInSecondsToDateTime(
                        i.CompletedTimestamp
                    )
                    .ToString(CultureInfo.InvariantCulture),
                Attempts = i.Attempts,
                ApiKey = i.ApiKey == null ? null : new ApiKeyDto(i.ApiKey),
                ChildId = i.ChildId
            }
        );
        return ret;
    }

    private static DateTime UnixTimeStampInSecondsToDateTime(long unixTimeStamp)
    {
        var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp);
        var dateTime = new DateTime(dateTimeOffset.Ticks, DateTimeKind.Utc);
        return dateTime;
    }

    #endregion
}
