using Orleans;
using Shared;

namespace Grains;

public interface IPrompterGrain : ISchrodingerGrain, IGrainWithStringKey
{
    Task<bool> SetConfigAsync(PrompterConfig config);
    Task<PrompterConfig> GetConfigAsync();
    Task<string> GeneratePromptAsync(PromptGenerationRequest promptGenerationRequest);
}