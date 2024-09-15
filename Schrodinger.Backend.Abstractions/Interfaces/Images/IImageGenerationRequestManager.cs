using Schrodinger.Backend.Abstractions.Types.AccountUsage;

namespace Schrodinger.Backend.Abstractions.Interfaces.Images;

public interface IImageGenerationRequestManager : Orleans.IGrainWithStringKey
{
    Task<bool> IsOverloaded();
    
    Task<bool> ForceRequestExecution(string childId);
    
    Task<List<BlockedRequestInfoDto>> GetBlockedImageGenerationRequestsAsync();

    Task<Dictionary<string, List<RequestAccountUsageInfoDto>>> GetImageGenerationStates();
}