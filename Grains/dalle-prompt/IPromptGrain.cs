using Orleans;
using Shared;

namespace Grains;

public interface IPromptGrain : ISchrodingerGrain, IGrainWithStringKey
{
    Task<PromptGenerationResponse> generatePrompt(PromptGenerationRequest promptGenerationRequest);
}