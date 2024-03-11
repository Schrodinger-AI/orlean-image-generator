using Grains.types;

namespace Grains.usage_tracker;

public class MasterTrackerState
{
    public Dictionary<string, RequestAccountUsageInfo> PendingImageGenerationRequests { get; set; }
    public Dictionary<string, RequestAccountUsageInfo> StartedImageGenerationRequests { get; set; }
    public Dictionary<string, RequestAccountUsageInfo> FailedImageGenerationRequests { get; set; }
    /// <summary>
    /// Completed will be purged periodically by the scheduler
    /// </summary>
    public Dictionary<string, RequestAccountUsageInfo> CompletedImageGenerationRequests { get; set; }
    
    public List<ApiInformation> ApiInformationList { get; set; }
    
}