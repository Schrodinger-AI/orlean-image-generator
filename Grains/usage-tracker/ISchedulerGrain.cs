using Grains.types;
using Shared;

namespace Grains.usage_tracker;

public interface ISchedulerGrain : ISchrodingerGrain, Orleans.IGrainWithStringKey, IImageGenerationRequestStatusReceiver
{
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetFailedImageGenerationRequestsAsync();
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetStartedImageGenerationRequestsAsync();
    Task<IReadOnlyDictionary<string, RequestAccountUsageInfo>> GetPendingImageGenerationRequestsAsync();
    Task AddImageGenerationRequest(string requestId, string childId, long requestTimestamp);
    Task<List<string>> AddApiKeys(List<ApiKeyEntry> apiKeyEntries);
    Task<List<string>> RemoveApiKeys(List<string> apiKey);
    Task<bool> ForceRequestExecution(string childId);
    Task<IReadOnlyList<APIAccountInfo>> GetAllApiKeys();
    Task<SchedulerState> GetImageGenerationStates();
    Task<Dictionary<string, ApiKeyUsageInfo>> GetApiKeysUsageInfo();
    Task<bool> IsOverloaded();
    Task FlushAsync();
    Task TickAsync();
    Task ClearFailedRequest();
    Task ClearPendingRequest();
}