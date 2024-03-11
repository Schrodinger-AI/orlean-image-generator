using Orleans;
using Orleans.Runtime;
using Shared;

namespace Grains;

public class MultiImageGeneratorGrain : Grain, IMultiImageGeneratorGrain
{
    private readonly PromptBuilder _promptBuilder;

    private readonly IPersistentState<MultiImageGenerationState> _multiImageGenerationState;

    public MultiImageGeneratorGrain([PersistentState("multiImageGenerationState", "MySqlSchrodingerImageStore")] IPersistentState<MultiImageGenerationState> multiImageGenerationState, PromptBuilder promptBuilder)
    {
        _multiImageGenerationState = multiImageGenerationState;
        _promptBuilder = promptBuilder;
    }

    public async Task<MultiImageGenerationGrainResponse> GenerateMultipleImagesAsync(List<Trait> traits, int NumberOfImages, string multiImageRequestId)
    {
        _multiImageGenerationState.State.RequestId = multiImageRequestId;
        await _multiImageGenerationState.WriteStateAsync();
        bool IsSuccessful = true;

        // Extract trait names from the request
        string prompt = await generatePrompt([.. traits]);

        _multiImageGenerationState.State.Prompt = prompt;
        _multiImageGenerationState.State.Traits = traits;

        for (int i = 0; i < NumberOfImages; i++)
        {
            //generate a new UUID with a prefix of "imageRequest"        
            string imageRequestId = "ImageRequest_" + Guid.NewGuid().ToString();

            var imageGeneratorGrain = GrainFactory.GetGrain<IImageGeneratorGrain>(imageRequestId);

            var imageGenerationGrainResponse = await imageGeneratorGrain.GenerateImageFromPromptAsync(imageRequestId, prompt);

            Console.WriteLine("Image generation submitted for request: " + imageRequestId + " with response: " + imageGenerationGrainResponse);

            IsSuccessful = IsSuccessful && imageGenerationGrainResponse.IsSuccessful;

            if (!imageGenerationGrainResponse.IsSuccessful)
            {
                // Add the error to the state
                if (_multiImageGenerationState.State.Errors == null)
                {
                    _multiImageGenerationState.State.Errors = new List<string>();
                }

                _multiImageGenerationState.State.Errors.Add(imageGenerationGrainResponse.Error);
            }

            else
            {
                // Extract the requestId from the response
                var requestId = imageGenerationGrainResponse.RequestId;

                // Add the requestId to the state
                _multiImageGenerationState.State.ImageGenerationRequestIds.Add(requestId);
            }
        }

        _multiImageGenerationState.State.IsSuccessful = IsSuccessful;
        await _multiImageGenerationState.WriteStateAsync();

        //TODO refactor this to return a single response
        if (!IsSuccessful)
        {
            return new MultiImageGenerationGrainResponse
            {
                RequestId = multiImageRequestId,
                Traits = traits,
                Prompt = prompt,
                IsSuccessful = false,
                Errors = _multiImageGenerationState.State.Errors
            };
        }
        else
        {
            return new MultiImageGenerationGrainResponse
            {
                RequestId = multiImageRequestId,
                Traits = traits,
                Prompt = prompt,
                IsSuccessful = true,
            };
        }
    }

    public async Task<Dictionary<string, TraitEntry>> lookupTraitDefinitions(List<Trait> requestTraits)
    {
        // Extract trait names from the request
        var traitNames = requestTraits.Select(t => t.Name).ToList();

        // Get a reference to the TraitConfigGrain
        var traitConfigGrain = GrainFactory.GetGrain<ITraitConfigGrain>("traitConfigGrain");

        // Retrieve the trait definitions from the TraitConfigGrain
        var response = await traitConfigGrain.GetTraitsMap(traitNames);

        return response;
    }

    public async Task<string> generatePrompt(List<Trait> requestTraits)
    {
        Dictionary<string, TraitEntry> traitDefinitions = await lookupTraitDefinitions([.. requestTraits]);
        var sentences = await _promptBuilder.GenerateSentences(requestTraits, traitDefinitions);
        var prompt = await _promptBuilder.GenerateFinalPromptFromSentences(ImageGenerationConstants.DALLE_BASE_PROMPT, sentences);
        return prompt;
    }

    public async Task<MultiImageQueryGrainResponse> QueryMultipleImagesAsync()
    {
        var allImages = new List<ImageDescription>();
        var isSuccessful = true;
        var errorMessages = new List<string>();

        foreach (var imageGenerationRequestId in _multiImageGenerationState.State.ImageGenerationRequestIds)
        {
            var imageGeneratorGrain = GrainFactory.GetGrain<IImageGeneratorGrain>(imageGenerationRequestId);

            var response = await imageGeneratorGrain.QueryImageAsync();

            if (response is ImageQueryGrainResponse grainResponse)
            {
                if (grainResponse.IsSuccessful && grainResponse.Image != null)
                {
                    grainResponse.Image.Traits = _multiImageGenerationState.State.Traits;
                    allImages.Add(grainResponse.Image);
                }
            }
            else
            {
                isSuccessful = false;
                errorMessages.Add(response.Error);
            }
        }

        if (isSuccessful)
        {
            return new MultiImageQueryGrainResponse
            {
                Images = allImages,
                IsSuccessful = true
            };
        }
        else
        {
            return new MultiImageQueryGrainResponse
            {
                IsSuccessful = false,
                Errors = errorMessages
            };
        }
    }
}
