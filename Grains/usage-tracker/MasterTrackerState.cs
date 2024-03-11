namespace Grains.usage_tracker;

public class MasterTrackerState
{
    public Dictionary<string, int> StartedImageGenerationRequests { get; set; }
    public Dictionary<string, int> FailedImageGenerationRequests { get; set; }
}