using Orleans;
using Schrodinger.Backend.Abstractions.Prompter;

namespace Schrodinger.Backend.Abstractions.Interfaces {
    public interface IPrompterGrain : ISchrodingerGrain, IGrainWithStringKey
    {
        Task<bool> SetConfigAsync(PrompterConfigDto config);
        Task<PrompterConfigDto> GetConfigAsync();
        Task<string> GeneratePromptAsync(PromptGenerationRequestDto promptGenerationRequestDto);
    }
}