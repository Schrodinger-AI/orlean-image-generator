using Grains.types;

namespace Grains.usage_tracker;

[GenerateSerializer]
public class SchedulerState
{
    [Id(0)]
    public Dictionary<string, RequestAccountUsageInfo> PendingImageGenerationRequests { get; set; } = new();
    [Id(1)]
    public Dictionary<string, RequestAccountUsageInfo> StartedImageGenerationRequests { get; set; } = new();
    [Id(2)]
    public Dictionary<string, RequestAccountUsageInfo> FailedImageGenerationRequests { get; set; } = new();

    /// <summary>
    /// Completed will be purged periodically by the scheduler
    /// </summary>
    [Id(3)]
    public Dictionary<string, RequestAccountUsageInfo> CompletedImageGenerationRequests { get; set; } = new();
    [Id(4)]
    public Dictionary<string, BlockedRequestInfo> BlockedImageGenerationRequests { get; set; } = new();

    [Id(5)]
    public List<APIAccountInfo> ApiAccountInfoList { get; set; } = [];
    
}