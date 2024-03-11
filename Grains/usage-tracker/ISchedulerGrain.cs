using Grains.types;

namespace Grains.usage_tracker;

public interface ISchedulerGrain
{
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetFailedImageGenerationRequestsAsync();
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetStartedImageGenerationRequestsAsync();
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetPendingImageGenerationRequestsAsync();
    Task AddImageGenerationRequest(string requestId, string accountInfo, long requestTimestamp);
}