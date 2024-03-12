using Grains.usage_tracker;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Shared;
using UnitTests.Grains;
using Attribute = Shared.Attribute;

namespace Grains;

public class MultiImageGeneratorGrain : Grain, IMultiImageGeneratorGrain
{
    private readonly ILogger<SchedulerGrain> _logger;

    private readonly IPersistentState<MultiImageGenerationState> _multiImageGenerationState;

    public MultiImageGeneratorGrain(
        [PersistentState("multiImageGenerationState", "MySqlSchrodingerImageStore")]
        IPersistentState<MultiImageGenerationState> multiImageGenerationState,
        ILogger<SchedulerGrain> logger)
    {
        _multiImageGenerationState = multiImageGenerationState;
        _logger = logger;
    }

    public async Task NotifyImageGenerationStatus(string imageRequestId, ImageGenerationStatus status, string? error)
    {
        _logger.LogInformation("NotifyImageGenerationStatus called with requestId: {}, status: {}, error: {}",
            imageRequestId, status, error);

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
        _logger.LogInformation(
            "GenerateMultipleImagesAsync called with traits: {}, NumberOfImages: {}, multiImageRequestId: {}", traits,
            NumberOfImages, multiImageRequestId);

        try
        {
            _multiImageGenerationState.State.RequestId = multiImageRequestId;
            var IsSuccessful = true;

            // Extract trait names from the request
            var prompt = await GeneratePromptAsync(traits);

            _logger.LogInformation("For MultiImageRequest: {} Prompt generated: {}", multiImageRequestId, prompt);

            _multiImageGenerationState.State.Prompt = prompt;
            _multiImageGenerationState.State.Traits = traits;
            var schedulerGrain = GrainFactory.GetGrain<ISchedulerGrain>("SchedulerGrain");

            //get timestamp
            var requestTimestamp = DateTime.UtcNow;
            var unixTimestamp = ((DateTimeOffset)requestTimestamp).ToUnixTimeSeconds();

            for (var i = 0; i < NumberOfImages; i++)
            {
                //generate a new UUID with a prefix of "imageRequest"        
                var imageRequestId = "ImageRequest_" + Guid.NewGuid().ToString();

                var imageGeneratorGrain = GrainFactory.GetGrain<IImageGeneratorGrain>(imageRequestId);

                await imageGeneratorGrain.SetImageGenerationRequestData(prompt, imageRequestId, multiImageRequestId);

                _multiImageGenerationState.State.ImageGenerationRequestIds.Add(imageRequestId);

                _logger.LogInformation(
                    "For MultiImageRequest: {} ImageRequest: {} added to the list of imageGenerationRequestIds",
                    multiImageRequestId, imageRequestId);

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
            _logger.LogError(ex, "Error occurred in GenerateMultipleImagesAsync for MultiImageRequest: {}",
                multiImageRequestId);
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
        _logger.LogInformation("lookupTraitDefinitions called with requestTraits: {}", requestTraits);
        // Extract trait names from the request
        var traitNames = requestTraits.Select(t => t.TraitType).ToList();

        // Get a reference to the TraitConfigGrain
        var traitConfigGrain = GrainFactory.GetGrain<ITraitConfigGrain>("traitConfigGrain");

        // Retrieve the trait definitions from the TraitConfigGrain
        var response = await traitConfigGrain.GetTraitsMap(traitNames);

        return response;
    }

    public async Task<MultiImageQueryGrainResponse> QueryMultipleImagesAsync()
    {
        if (string.IsNullOrEmpty(_multiImageGenerationState.State.Prompt))
        {
            return new MultiImageQueryGrainResponse()
            {
                Initialized = false
            };
        }

        var allImages = new List<ImageDescription>();
        var imageGenerationStates = new List<ImageGenerationStatus>();
        var errorMessages = new List<string>();

        foreach (var imageGenerationRequestId in _multiImageGenerationState.State.ImageGenerationRequestIds)
        {
            var imageGeneratorGrain = GrainFactory.GetGrain<IImageGeneratorGrain>(imageGenerationRequestId);

            var response = await imageGeneratorGrain.QueryImageAsync();

            if (response is not ImageQueryGrainResponse grainResponse) continue;
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