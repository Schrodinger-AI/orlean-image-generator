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
        try
        {
            _multiImageGenerationState.State.RequestId = multiImageRequestId;
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

                await imageGeneratorGrain.SetImageGenerationRequestData(prompt, imageRequestId, multiImageRequestId);

                _multiImageGenerationState.State.ImageGenerationRequestIds.Add(imageRequestId);
            }

            _multiImageGenerationState.State.IsSuccessful = IsSuccessful;
            await _multiImageGenerationState.WriteStateAsync();

            return new MultiImageGenerationGrainResponse
            {
                RequestId = multiImageRequestId,
                Traits = traits,
                Prompt = prompt,
                IsSuccessful = true,
            };
        }
        catch (Exception ex)
        {
            if (_multiImageGenerationState.State.Errors == null)
            {
                _multiImageGenerationState.State.Errors = [];
            }

            _multiImageGenerationState.State.Errors.Add(ex.Message);
            _multiImageGenerationState.State.IsSuccessful = false;
            await _multiImageGenerationState.WriteStateAsync();
            return new MultiImageGenerationGrainResponse
            {
                RequestId = multiImageRequestId,
                Traits = traits,
                Prompt = "",
                IsSuccessful = false,
                Errors = _multiImageGenerationState.State.Errors
            };
        }
    }

    public async Task<string> HandleImageGenerationNotification(ImageGenerationNotification imageGenerationNotification)
    {
        _multiImageGenerationState.State.imageGenerationTracker[imageGenerationNotification.RequestId] = imageGenerationNotification;

        await _multiImageGenerationState.WriteStateAsync();

        return "Notification received";
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
        List<ImageGenerationStatus> imageGenerationStates = [];
        var errorMessages = new List<string>();

        foreach (var imageGenerationRequestId in _multiImageGenerationState.State.ImageGenerationRequestIds)
        {
            var imageGeneratorGrain = GrainFactory.GetGrain<IImageGeneratorGrain>(imageGenerationRequestId);

            var response = await imageGeneratorGrain.QueryImageAsync();

            if (response is ImageQueryGrainResponse grainResponse)
            {
                if (grainResponse.Status == ImageGenerationStatus.SuccessfulCompletion && grainResponse.Image != null)
                {
                    grainResponse.Image.Traits = _multiImageGenerationState.State.Traits;
                    imageGenerationStates.Add(grainResponse.Status);
                    allImages.Add(grainResponse.Image);
                }

                else
                {
                    imageGenerationStates.Add(response.Status);
                }
            }

        }

        ImageGenerationStatus finalStatus;

        // loop thru the imageGenerationStates and determine the final status by below logc
        // if any of the statuses is InProgress, mark the final status as InProgress
        // if all statuses are SuccessfulCompletion, mark the final status as SuccessfulCompletion
        // if all statuses are FailedCompletion, mark the final status as FailedCompletion
        if (imageGenerationStates.Any(state => state == ImageGenerationStatus.InProgress))
        {
            // If any of the statuses is InProgress, mark the final status as InProgress
            finalStatus = ImageGenerationStatus.InProgress;
        }
        else if (imageGenerationStates.All(state => state == ImageGenerationStatus.SuccessfulCompletion))
        {
            // If all statuses are SuccessfulCompletion, mark the final status as SuccessfulCompletion
            finalStatus = ImageGenerationStatus.SuccessfulCompletion;
        }
        else if (imageGenerationStates.All(state => state == ImageGenerationStatus.FailedCompletion))
        {
            // If all statuses are FailedCompletion, mark the final status as FailedCompletion
            finalStatus = ImageGenerationStatus.FailedCompletion;
        }
        else
        {
            // Handle the case where the statuses are a mix of SuccessfulCompletion and FailedCompletion
            finalStatus = ImageGenerationStatus.InProgress;
        }

        if (finalStatus == ImageGenerationStatus.SuccessfulCompletion)
        {
            return new MultiImageQueryGrainResponse
            {
                Images = allImages,
                Status = ImageGenerationStatus.SuccessfulCompletion
            };
        }
        else if (finalStatus == ImageGenerationStatus.InProgress)
        {
            return new MultiImageQueryGrainResponse
            {
                Status = ImageGenerationStatus.InProgress
            };
        }
        else
        {
            return new MultiImageQueryGrainResponse
            {
                Status = ImageGenerationStatus.FailedCompletion,
                Errors = errorMessages
            };
        }
    }
}
