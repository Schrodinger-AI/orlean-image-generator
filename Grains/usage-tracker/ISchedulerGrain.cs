using Grains.types;
using Shared;

namespace Grains.usage_tracker;

public interface ISchedulerGrain : ISchrodingerGrain, Orleans.IGrainWithStringKey, IImageGenerationRequestStatusReceiver
{
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetFailedImageGenerationRequestsAsync();
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetStartedImageGenerationRequestsAsync();
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetPendingImageGenerationRequestsAsync();
    Task<IReadOnlyDictionary<string, BlockedRequestInfo>> GetBlockedImageGenerationRequestsAsync();
    Task AddImageGenerationRequest(string requestId, string childId, long requestTimestamp);
    Task<List<ApiKey>> AddApiKeys(List<APIAccountInfo> apiKeyEntries);
    Task<List<ApiKey>> RemoveApiKeys(List<ApiKey> apiKeys);
    Task<IReadOnlyList<APIAccountInfo>> GetAllApiKeys();
    Task<SchedulerState> GetImageGenerationStates();
    Task<Dictionary<string, ApiKeyUsageInfo>> GetApiKeysUsageInfo();
    Task<bool> IsOverloaded();
    Task FlushAsync();
    Task TickAsync();
}