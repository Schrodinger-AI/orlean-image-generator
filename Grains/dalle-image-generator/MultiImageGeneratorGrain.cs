using Grains.usage_tracker;
using Orleans;
using Orleans.Runtime;
using Shared;
using UnitTests.Grains;
using Attribute = Shared.Attribute;

namespace Grains;

public class MultiImageGeneratorGrain : Grain, IMultiImageGeneratorGrain
{
    private readonly PromptBuilder _promptBuilder;

    private readonly IPersistentState<MultiImageGenerationState> _multiImageGenerationState;

    public MultiImageGeneratorGrain(
        [PersistentState("multiImageGenerationState", "MySqlSchrodingerImageStore")]
        IPersistentState<MultiImageGenerationState> multiImageGenerationState, PromptBuilder promptBuilder)
    {
        _multiImageGenerationState = multiImageGenerationState;
        _promptBuilder = promptBuilder;
    }

    public async Task NotifyImageGenerationStatus(string imageRequestId, ImageGenerationStatus status, string? error)
    {
        var imageGenerationNotification = new ImageGenerationTracker
        {
            RequestId = imageRequestId,
            Status = status,
            Error = error
        };

        _multiImageGenerationState.State.imageGenerationTrackers[imageGenerationNotification.RequestId] =
            imageGenerationNotification;

        await _multiImageGenerationState.WriteStateAsync();
    }

    private async Task<string> GeneratePromptAsync(List<Attribute> attributes)
    {
        var grain = GrainFactory.GetGrain<IConfiguratorGrain>(Constants.ConfiguratorIdentifier);
        var curConfigId = await grain.GetCurrentConfigIdAsync();
        var prompterGrain = GrainFactory.GetGrain<IPrompterGrain>(curConfigId);
        return await prompterGrain.GeneratePromptAsync(new PromptGenerationRequest
        {
            NewAttributes = attributes
        });
    }

    public async Task<MultiImageGenerationGrainResponse> GenerateMultipleImagesAsync(List<Attribute> traits,
        int NumberOfImages, string multiImageRequestId)
    {
        try
        {
            _multiImageGenerationState.State.RequestId = multiImageRequestId;
            bool IsSuccessful = true;

            // Extract trait names from the request
            string prompt = await GeneratePromptAsync(traits);

            _multiImageGenerationState.State.Prompt = prompt;
            _multiImageGenerationState.State.Traits = traits;
            var schedulerGrain = GrainFactory.GetGrain<ISchedulerGrain>("SchedulerGrain");

            //get timestamp
            var requestTimestamp = DateTime.UtcNow;
            long unixTimestamp = ((DateTimeOffset)requestTimestamp).ToUnixTimeSeconds();

            for (int i = 0; i < NumberOfImages; i++)
            {
                //generate a new UUID with a prefix of "imageRequest"        
                string imageRequestId = "ImageRequest_" + Guid.NewGuid().ToString();

                var imageGeneratorGrain = GrainFactory.GetGrain<IImageGeneratorGrain>(imageRequestId);

                await imageGeneratorGrain.SetImageGenerationRequestData(prompt, imageRequestId, multiImageRequestId);

                _multiImageGenerationState.State.ImageGenerationRequestIds.Add(imageRequestId);

                await schedulerGrain.AddImageGenerationRequest(multiImageRequestId, imageRequestId, unixTimestamp);
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

    public async Task<Dictionary<string, TraitEntry>> lookupTraitDefinitions(List<Attribute> requestTraits)
    {
        // Extract trait names from the request
        var traitNames = requestTraits.Select(t => t.TraitType).ToList();

        // Get a reference to the TraitConfigGrain
        var traitConfigGrain = GrainFactory.GetGrain<ITraitConfigGrain>("traitConfigGrain");

        // Retrieve the trait definitions from the TraitConfigGrain
        var response = await traitConfigGrain.GetTraitsMap(traitNames);

        return response;
    }

    public async Task<string> generatePrompt(List<Attribute> requestTraits)
    {
        Dictionary<string, TraitEntry> traitDefinitions = await lookupTraitDefinitions([.. requestTraits]);
        var sentences = await _promptBuilder.GenerateSentences(requestTraits, traitDefinitions);
        var prompt =
            await _promptBuilder.GenerateFinalPromptFromSentences(ImageGenerationConstants.DALLE_BASE_PROMPT,
                sentences);
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
                    grainResponse.Image.Attributes = _multiImageGenerationState.State.Traits;
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