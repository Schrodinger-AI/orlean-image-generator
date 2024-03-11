using Orleans;
using Shared;

namespace Grains;

public interface IPromptCreatorGrain : ISchrodingerGrain, IGrainWithStringKey
{
    void SetPromptState(SetPromptStateRequest setPromptStateRequest);
    Task<PromptConfigOptions> ReadPromptState();
    Task<string> Generate(PromptGenerationRequest promptGenerationRequest);
}