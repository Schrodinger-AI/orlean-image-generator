using Orleans;
using Shared.Abstractions.Prompter;

namespace Shared.Abstractions.Interfaces {
    public interface IPrompterGrain : ISchrodingerGrain, IGrainWithStringKey
    {
        Task<bool> SetConfigAsync(PrompterConfigDto config);
        Task<PrompterConfigDto> GetConfigAsync();
        Task<string> GeneratePromptAsync(PromptGenerationRequestDto promptGenerationRequestDto);
    }
}