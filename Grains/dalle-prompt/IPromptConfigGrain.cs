using Orleans;
using Shared;

namespace Grains;

public interface IPromptConfigGrain : ISchrodingerGrain, IGrainWithStringKey
{
    Task<PromptConfigResponse> ConfigPrompt(PromptConfigRequest promptConfigRequest);
    Task<PromptConfigResponse> QueryPromptConfig();
}