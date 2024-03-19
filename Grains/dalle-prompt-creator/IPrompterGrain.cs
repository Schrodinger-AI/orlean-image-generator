using Grains.interfaces;
using Orleans;
using Shared;
using Shared.Prompter;

namespace Grains;

public interface IPrompterGrain : ISchrodingerGrain, IGrainWithStringKey
{
    Task<bool> SetConfigAsync(PrompterConfig config);
    Task<PrompterConfig> GetConfigAsync();
    Task<string> GeneratePromptAsync(PromptGenerationRequest promptGenerationRequest);
}