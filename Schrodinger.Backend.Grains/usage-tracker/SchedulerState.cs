using Schrodinger.Backend.Grains.types;

namespace Schrodinger.Backend.Grains.usage_tracker;

public class SchedulerState
{
    public Dictionary<
        string,
        RequestAccountUsageInfo
    > PendingImageGenerationRequests { get; set; } = new();
    public Dictionary<
        string,
        RequestAccountUsageInfo
    > StartedImageGenerationRequests { get; set; } = new();
    public Dictionary<
        string,
        RequestAccountUsageInfo
    > FailedImageGenerationRequests { get; set; } = new();

    /// <summary>
    /// Completed will be purged periodically by the scheduler
    /// </summary>
    public Dictionary<
        string,
        RequestAccountUsageInfo
    > CompletedImageGenerationRequests { get; set; } = new();
    public Dictionary<
        string,
        BlockedRequestInfo
    > BlockedImageGenerationRequests { get; set; } = new();
    public List<APIAccountInfo> ApiAccountInfoList { get; set; } = [];
}
