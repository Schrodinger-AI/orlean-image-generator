using Grains.types;
using Shared;

namespace Grains.usage_tracker;

public interface ISchedulerGrain : ISchrodingerGrain, Orleans.IGrainWithStringKey, IImageGenerationRequestStatusReceiver
{
    Task<List<RequestAccountUsageInfoDto>> GetFailedImageGenerationRequestsAsync();
    Task<List<RequestAccountUsageInfoDto>> GetStartedImageGenerationRequestsAsync();
    Task<List<RequestAccountUsageInfoDto>> GetPendingImageGenerationRequestsAsync();
    Task<List<RequestAccountUsageInfoDto>> GetCompletedImageGenerationRequestsAsync();
    Task<List<BlockedRequestInfoDto>> GetBlockedImageGenerationRequestsAsync();
    Task AddImageGenerationRequest(string requestId, string childId, long requestTimestamp);
    Task<AddApiKeysResponseDto> AddApiKeys(List<ApiKeyEntryDto> apiKeyEntries);
    Task<List<ApiKey>> RemoveApiKeys(List<ApiKey> apiKeys);
    Task<IReadOnlyList<ApiKeyEntryDto>> GetAllApiKeys();
    Task<Dictionary<string, List<RequestAccountUsageInfoDto>>> GetImageGenerationStates();
    Task<Dictionary<string, ApiKeyUsageInfo>> GetApiKeysUsageInfo();
    Task<bool> IsOverloaded();
    Task FlushAsync();
    Task TickAsync();
    Task<bool> ForceRequestExecution(string childId);
}