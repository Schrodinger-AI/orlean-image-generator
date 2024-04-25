using Schrodinger.Backend.Abstractions.AccountUsage;

namespace Schrodinger.Backend.Abstractions.Interfaces;

public interface IImageGenerationRequestManager : Orleans.IGrainWithStringKey
{
    Task<bool> IsOverloaded();
    
    Task<bool> ForceRequestExecution(string childId);
    
    Task<List<BlockedRequestInfoDto>> GetBlockedImageGenerationRequestsAsync();

    Task<Dictionary<string, List<RequestAccountUsageInfoDto>>> GetImageGenerationStates();
}