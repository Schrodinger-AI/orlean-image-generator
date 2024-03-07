using Orleans;
using Shared;

namespace Grains;

public class ImageGeneratorGrain : Grain
{
    private readonly PromptBuilder _promptBuilder;

    public ImageGeneratorGrain(PromptBuilder promptBuilder)
    {
        _promptBuilder = promptBuilder;
    }

    public async Task<string> generatePrompt(ImageGenerationRequest imageGenerationRequest) {

        List<Trait> newTraits = imageGenerationRequest.NewTraits;
        List<Trait> baseTraits = imageGenerationRequest.BaseImage.Traits;

        //collect the newTraits from the request and combine it with trats from the base image
        IEnumerable<Trait> traits = newTraits.Concat(baseTraits);

        // Extract trait names from the request
        Dictionary<string, TraitEntry> traitDefinitions = await lookupTraitDefinitions(traits.ToList());

        string prompt = await generatePrompt(traits.ToList(), traitDefinitions);
            
        return prompt;
    }

    public Task<ImageGenerationResponse> GenerateImage(ImageGenerationRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<ImageQueryResponseOk> QueryImages(ImageQueryRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<Dictionary<string, TraitEntry>> lookupTraitDefinitions(List<Trait> requestTraits)
    {
        // Extract trait names from the request
        var traitNames = requestTraits.Select(t => t.Name).ToList();

        // Get a reference to the TraitConfigGrain
        var traitConfigGrain = GrainFactory.GetGrain<ITraitConfigGrain>("traitConfigGrain");

        // Retrieve the trait definitions from the TraitConfigGrain
        var traitDefinitions = await traitConfigGrain.GetTraitsMap(traitNames);

        return traitDefinitions;
    }

    public async Task<String> generatePrompt(List<Trait> requestTraits, Dictionary<string, TraitEntry> traitDefinitions)
    {
        var sentences = await _promptBuilder.GenerateSentences(requestTraits, traitDefinitions);
        var prompt = await _promptBuilder.GenerateFinalPromptFromSentences(ImageGenerationConstants.DALLE_BASE_PROMPT, sentences);
        return prompt;
    }

}