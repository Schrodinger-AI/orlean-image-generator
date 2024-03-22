using Grains.types;
using Shared;

namespace Grains.usage_tracker;

public interface ISchedulerGrain : ISchrodingerGrain, Orleans.IGrainWithStringKey, IImageGenerationRequestStatusReceiver
{
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetFailedImageGenerationRequestsAsync();
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetStartedImageGenerationRequestsAsync();
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetPendingImageGenerationRequestsAsync();
    Task<IEnumerable<BlockedRequestInfoDto>> GetBlockedImageGenerationRequestsAsync();
    Task AddImageGenerationRequest(string requestId, string childId, long requestTimestamp);
    Task<List<ApiKey>> AddApiKeys(List<ApiKeyEntryDto> apiKeyEntries);
    Task<List<ApiKey>> RemoveApiKeys(List<ApiKey> apiKeys);
    Task<IReadOnlyList<ApiKeyEntryDto>> GetAllApiKeys();
    Task<Dictionary<string, IEnumerable<RequestAccountUsageInfoDto>>> GetImageGenerationStates();
    Task<Dictionary<string, ApiKeyUsageInfo>> GetApiKeysUsageInfo();
    Task<bool> IsOverloaded();
    Task FlushAsync();
    Task TickAsync();
    Task<bool> ForceRequestExecution(string childId);
}