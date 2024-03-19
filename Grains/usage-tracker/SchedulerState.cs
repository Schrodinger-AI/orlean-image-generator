using Grains.types;

namespace Grains.usage_tracker;

[GenerateSerializer]
public class SchedulerState
{
    [Id(0)]
    public Dictionary<string, RequestAccountUsageInfo> PendingImageGenerationRequests { get; set; }
    [Id(1)]
    public Dictionary<string, RequestAccountUsageInfo> StartedImageGenerationRequests { get; set; }
    [Id(2)]
    public Dictionary<string, RequestAccountUsageInfo> FailedImageGenerationRequests { get; set; }

    /// <summary>
    /// Completed will be purged periodically by the scheduler
    /// </summary>
    [Id(3)]
    public Dictionary<string, RequestAccountUsageInfo> CompletedImageGenerationRequests { get; set; }
    [Id(4)]
    public Dictionary<string, BlockedRequestInfo> BlockedImageGenerationRequests { get; set; }

    [Id(5)]
    public List<APIAccountInfo> ApiAccountInfoList { get; set; }
    
}