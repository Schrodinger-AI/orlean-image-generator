using Orleans;
using Orleans.Runtime;

namespace Grains.usage_tracker;

public class MasterTrackerGrain : Grain, IMasterTrackerGrain, IImageGenerationRequestStatusReceiver
{
    private readonly IPersistentState<MasterTrackerState> _masterTrackerState;

    public MasterTrackerGrain(
        [PersistentState("masterTrackerState", "MySqlSchrodingerImageStore")]
        IPersistentState<MasterTrackerState> masterTrackerState)
    {
        _masterTrackerState = masterTrackerState;
    }

    public Task ReportFailedImageGenerationRequestAsync(string requestId)
    {
        _masterTrackerState.State.FailedImageGenerationRequests.Add(requestId, 1);
        return Task.CompletedTask;
    }

    public Task ReportCompletedImageGenerationRequestAsync(string requestId)
    {
        _masterTrackerState.State.StartedImageGenerationRequests.Remove(requestId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, int>> GetFailedImageGenerationRequestsAsync()
    {
        return Task.FromResult<IReadOnlyDictionary<string, int>>(
            _masterTrackerState.State.FailedImageGenerationRequests);
    }

    public Task<IReadOnlyDictionary<string, int>> GetStartedImageGenerationRequestsAsync()
    {
        return Task.FromResult<IReadOnlyDictionary<string, int>>(_masterTrackerState.State
            .StartedImageGenerationRequests);
    }
}