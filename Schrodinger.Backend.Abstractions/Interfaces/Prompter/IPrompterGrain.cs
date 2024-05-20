using Orleans;
using Schrodinger.Backend.Abstractions.Types.Prompter;

namespace Schrodinger.Backend.Abstractions.Interfaces.Prompter {
    public interface IPrompterGrain : ISchrodingerGrain, IGrainWithStringKey
    {
        Task<bool> SetConfigAsync(PrompterConfigDto config);
        Task<PrompterConfigDto> GetConfigAsync();
        Task<string> GeneratePromptAsync(PromptGenerationRequestDto promptGenerationRequestDto);
    }
}