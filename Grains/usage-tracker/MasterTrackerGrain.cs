using Grains.types;
using Orleans;
using Orleans.Runtime;
using Orleans.Timers;

namespace Grains.usage_tracker;

/// <summary>
/// It keeps track of how many jobs have been started per each account in the last minute (or jobs started more than
/// one minute ago but haven't completed). It will compare this count against the account's quota and choose the least
/// loaded account for the next job.
/// </summary>
public class MasterTrackerGrain : Grain, IMasterTrackerGrain, IImageGenerationRequestStatusReceiver, IDisposable
{
    private const string ReminderName = "SchedulingReminder";

    private readonly IReminderRegistry _reminderRegistry;
    private readonly IPersistentState<MasterTrackerState> _masterTrackerState;
    
    private IGrainReminder? _reminder;

    public MasterTrackerGrain(
        [PersistentState("masterTrackerState", "MySqlSchrodingerImageStore")]
        IPersistentState<MasterTrackerState> masterTrackerState,
        ITimerRegistry timerRegistry,
        IReminderRegistry reminderRegistry)
    {
        // Register timer
        timerRegistry.RegisterTimer(
            this,
            asyncCallback: static async state =>
            {
                var scheduler = (MasterTrackerGrain)state;
                scheduler.DoScheduling();

                await Task.CompletedTask;
            },
            state: this,
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromSeconds(1));

        _reminderRegistry = reminderRegistry;
        
        _masterTrackerState = masterTrackerState;
    }

    public async Task ReportFailedImageGenerationRequestAsync(RequestStatus requestStatus)
    {
        var info = PopFromStarted(requestStatus.RequestId);
        var unixTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        info.FailedTimestamp = unixTimestamp;
        _masterTrackerState.State.FailedImageGenerationRequests.Add(requestStatus.RequestId, info);
        await Task.CompletedTask;
    }

    public async Task ReportCompletedImageGenerationRequestAsync(RequestStatus requestStatus)
    {
        var info = PopFromStarted(requestStatus.RequestId);
        var unixTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        info.CompletedTimestamp = unixTimestamp;
        _masterTrackerState.State.CompletedImageGenerationRequests.Add(requestStatus.RequestId, info);
        await Task.CompletedTask;
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

    #region Private Methods

    private void DoScheduling()
    {
        // 1. Purge completed requests
        // 2. Add failed requests to pending queue
        // 3. Check remaining quota for all accounts
        // 4. For all pending tasks, find the account with the most remaining quota
        // 5. Schedule the task, update the account usage info
        _masterTrackerState.State.CompletedImageGenerationRequests.Clear();

        ProcessRequest(_masterTrackerState.State.FailedImageGenerationRequests);
        ProcessRequest(_masterTrackerState.State.StartedImageGenerationRequests);
        
    }

    private void ProcessRequest(Dictionary<string, RequestAccountUsageInfo> requests)
    {
        foreach (var (requestId, info) in requests)
        {
            info.Attempts++;

            info.ApiKey = GetApiKey();
            // if there are no available api keys, we will try again in the next scheduling
            if (string.IsNullOrEmpty(info.ApiKey))
            {
                // TODO: log this and warn monitoring system
                return;
            }
            
            info.StartedTimestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
            // TODO: get child gen grain to process failed request again with the new api key

            // remove from list to add to pending
            _masterTrackerState.State.PendingImageGenerationRequests.Add(requestId, info);
        }
        
        requests.Clear();
    }

    private string GetApiKey()
    {
        _masterTrackerState.State.ApiInformationList.Sort((a, b) => a.Quota.CompareTo(b.Quota));
        
        foreach (var apiInfo in _masterTrackerState.State.ApiInformationList)
        {
            if (apiInfo.ReservedQuota > 0)
            {
                apiInfo.ReservedQuota--;
                return apiInfo.ApiKey;
            }
        }

        return "";
    }

    private RequestAccountUsageInfo PopFromStarted(string requestId)
    {
        var info = _masterTrackerState.State.StartedImageGenerationRequests[requestId];
        _masterTrackerState.State.StartedImageGenerationRequests.Remove(requestId);
        return info;
    }
    
    //keep alive
    public async Task Ping()
    {
        _reminder = await _reminderRegistry.RegisterOrUpdateReminder(
            reminderName: ReminderName,
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromHours(1));
    }

    void IDisposable.Dispose()
    {
        if (_reminder is not null)
        {
            _reminderRegistry.UnregisterReminder(_reminder);
        }
    }

    #endregion
}