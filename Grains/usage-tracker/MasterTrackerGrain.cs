using Grains.types;
using Orleans;
using Orleans.Runtime;

namespace Grains.usage_tracker;

/// <summary>
/// It keeps track of how many jobs have been started per each account in the last minute (or jobs started more than
/// one minute ago but haven't completed). It will compare this count against the account's quota and choose the least
/// loaded account for the next job.
/// </summary>
public class MasterTrackerGrain : Grain, IMasterTrackerGrain, IImageGenerationRequestStatusReceiver
{
    private readonly IPersistentState<MasterTrackerState> _masterTrackerState;

    public MasterTrackerGrain(
        [PersistentState("masterTrackerState", "MySqlSchrodingerImageStore")]
        IPersistentState<MasterTrackerState> masterTrackerState)
    {
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

    private RequestAccountUsageInfo PopFromStarted(string requestId)
    {
        var info = _masterTrackerState.State.StartedImageGenerationRequests[requestId];
        _masterTrackerState.State.StartedImageGenerationRequests.Remove(requestId);
        return info;
    }

    #endregion
}