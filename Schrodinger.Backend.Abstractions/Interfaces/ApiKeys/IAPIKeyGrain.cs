using Schrodinger.Backend.Abstractions.Types.ApiKeys;

namespace Schrodinger.Backend.Abstractions.Interfaces.ApiKeys;

public interface IAPIKeyGrain : ISchrodingerGrain, Orleans.IGrainWithStringKey
{
    Task<AddApiKeysResponseDto> AddApiKeys(List<ApiKeyEntryDto> apiKeyEntries);
    
    Task<List<ApiKey>> RemoveApiKeys(List<ApiKey> apiKeys);
    
    Task<IReadOnlyList<ApiKeyEntryDto>> GetAllApiKeys();
    
    Task<Dictionary<string, ApiKeyUsageInfo>> GetApiKeysUsageInfo();
}