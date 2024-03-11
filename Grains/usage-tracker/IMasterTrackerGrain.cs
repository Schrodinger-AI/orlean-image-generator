namespace Grains.usage_tracker;

public interface IMasterTrackerGrain
{
    Task<IReadOnlyDictionary<string, int>> GetFailedImageGenerationRequestsAsync();
    Task<IReadOnlyDictionary<string, int>> GetStartedImageGenerationRequestsAsync();
}