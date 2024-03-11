using Orleans;
using Shared;

namespace Grains;

public class PromptGeneratorGrain : Grain, IPromptGeneratorGrain
{
    private readonly PromptBuilder _promptBuilder;

    public PromptGeneratorGrain(PromptBuilder promptBuilder)
    {
        _promptBuilder = promptBuilder;
    }

    public async Task<string> GeneratePrompt(PromptGenerationRequest promptGenerationRequest) {

        List<Trait> newTraits = promptGenerationRequest.NewTraits;
        List<Trait> baseTraits = promptGenerationRequest.BaseImage.Traits;

        //collect the newTraits from the request and combine it with trats from the base image
        IEnumerable<Trait> traits = newTraits.Concat(baseTraits);

        // Extract trait names from the request
        Dictionary<string, TraitEntry> traitDefinitions = await LookupTraitDefinitions(traits.ToList());

        string prompt = await GeneratePrompt(traits.ToList(), traitDefinitions);
            
        return prompt;
    }

    public async Task<Dictionary<string, TraitEntry>> LookupTraitDefinitions(List<Trait> requestTraits)
    {
        // Extract trait names from the request
        var traitNames = requestTraits.Select(t => t.Name).ToList();

        // Get a reference to the TraitConfigGrain
        var traitConfigGrain = GrainFactory.GetGrain<ITraitConfigGrain>("traitConfigGrain");

        // Retrieve the trait definitions from the TraitConfigGrain
        var response = await traitConfigGrain.GetTraitsMap(traitNames);

        return response;
    }

    public async Task<String> GeneratePrompt(List<Trait> requestTraits, Dictionary<string, TraitEntry> traitDefinitions)
    {
        var sentences = await _promptBuilder.GenerateSentences(requestTraits, traitDefinitions);
        var prompt = await _promptBuilder.GenerateFinalPromptFromSentences(ImageGenerationConstants.DALLE_BASE_PROMPT, sentences);
        return prompt;
    }

}