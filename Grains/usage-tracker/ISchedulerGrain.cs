using Grains.types;
using Shared;

namespace Grains.usage_tracker;

public interface ISchedulerGrain : Orleans.IGrainWithStringKey
{
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetFailedImageGenerationRequestsAsync();
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetStartedImageGenerationRequestsAsync();
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetPendingImageGenerationRequestsAsync();
    Task AddImageGenerationRequest(string requestId, string childId, long requestTimestamp);
    Task<List<string>> AddApiKeys(List<ApiKeyEntry> apiKeyEntries);
    Task<List<string>> RemoveApiKeys(List<string> apiKey);
    Task<IReadOnlyList<APIAccountInfo>> GetAllApiKeys();
}