using Orleans;
using Shared;
namespace Grains;

    public interface IPromptGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
    {

        Task<string> generatePrompt(PromptGenerationRequest promptGenerationRequest);
    }
