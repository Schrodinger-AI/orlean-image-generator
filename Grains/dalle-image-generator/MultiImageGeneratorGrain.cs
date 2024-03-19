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
    private readonly ILogger<MultiImageGeneratorGrain> _logger;

    private readonly IPersistentState<MultiImageGenerationState> _multiImageGenerationState;

    public MultiImageGeneratorGrain(
        [PersistentState("multiImageGenerationState", "MySqlSchrodingerImageStore")]
        IPersistentState<MultiImageGenerationState> multiImageGenerationState,
        ILogger<MultiImageGeneratorGrain> logger)
    {
        _multiImageGenerationState = multiImageGenerationState;
        _logger = logger;
    }

    public async Task NotifyImageGenerationStatus(string imageRequestId, ImageGenerationStatus status, string? error)
    {
        _logger.LogInformation($"NotifyImageGenerationStatus called with requestId: {imageRequestId}, status: {status}, error: {error}");

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
            $"GenerateMultipleImagesAsync called with traits: {traits}, NumberOfImages: {NumberOfImages}, multiImageRequestId: {multiImageRequestId}");

        try
        {
            _multiImageGenerationState.State.RequestId = multiImageRequestId;
            var IsSuccessful = true;

            // Extract trait names from the request
            var prompt = "I NEED to test how the tool works with extremely simple prompts. DO NOT add any detail, just use it AS-IS: A pixel art image of a cat standing like a human with both feet visible on the ground, facing directly at the viewer,. The main character is wearing Hoodie. The image should contain the full-body shot of the main character. The image should contain one and only one cat. The generated image should not contain any text or labels.";

            _logger.LogInformation($"For MultiImageRequest: {multiImageRequestId} Prompt generated: {prompt}");

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

                _multiImageGenerationState.State.imageGenerationTrackers[imageRequestId] = new ImageGenerationTracker
                {
                    RequestId = imageRequestId,
                    Status = ImageGenerationStatus.InProgress
                };

                _multiImageGenerationState.State.ImageGenerationRequestIds.Add(imageRequestId);

                _logger.LogInformation(
                    $"For MultiImageRequest: {multiImageRequestId} ImageRequest: {imageRequestId} added to the list of imageGenerationRequestIds");

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
            _logger.LogError(ex, $"Error occurred in GenerateMultipleImagesAsync for MultiImageRequest: {multiImageRequestId}");
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
        _logger.LogInformation($"lookupTraitDefinitions called with requestTraits: {requestTraits}");
        // Extract trait names from the request
        var traitNames = requestTraits.Select(t => t.TraitType).ToList();

        // Get a reference to the TraitConfigGrain
        var traitConfigGrain = GrainFactory.GetGrain<ITraitConfigGrain>("traitConfigGrain");

        // Retrieve the trait definitions from the TraitConfigGrain
        var response = await traitConfigGrain.GetTraitsMap(traitNames);

        return response;
    }

    private ImageGenerationStatus GetCurrentImageGenerationStatus()
    {
        ImageGenerationStatus finalStatus = ImageGenerationStatus.Dormant;

        var statusArray = new List<ImageGenerationStatus>();
        
        //get child grain references
        foreach (var imageGenerationRequestId in _multiImageGenerationState.State.ImageGenerationRequestIds)
        {
            //check the imageTracker for imageGenerationRequestId
            var imageGenerationTracker = _multiImageGenerationState.State.imageGenerationTrackers[imageGenerationRequestId];

            //if the status is not successful, return false
            // if status is inProgress, break loop and return the Status as InProgress
            if(imageGenerationTracker.Status == ImageGenerationStatus.InProgress)
            {
                return imageGenerationTracker.Status;
            }
            
            statusArray.Add(imageGenerationTracker.Status);
        }
        
        // if all statuses are successful, return true
        if (statusArray.All(status => status == ImageGenerationStatus.SuccessfulCompletion))
        {
            return ImageGenerationStatus.SuccessfulCompletion;
        }
        
        // if all statuses are failed, return false
        if (statusArray.All(status => status == ImageGenerationStatus.FailedCompletion))
        {
            return ImageGenerationStatus.FailedCompletion;
        }
        
        // else return inProgress
        return ImageGenerationStatus.InProgress;
    }

    public async Task<MultiImageQueryGrainResponse> QueryMultipleImagesAsync()
    {
        _logger.LogInformation($"QueryMultipleImagesAsync called for MultiImageRequest: {_multiImageGenerationState.State.RequestId}");

        if (string.IsNullOrEmpty(_multiImageGenerationState.State.Prompt))
        {
            _logger.LogInformation($"MultiImageRequest: {_multiImageGenerationState.State.RequestId} is uninitialized");
            return new MultiImageQueryGrainResponse()
            {
                Uninitialized = true
            };
        }
        
        ImageGenerationStatus imageGenerationStatus = GetCurrentImageGenerationStatus();

        if (imageGenerationStatus == ImageGenerationStatus.InProgress)
        {
            _logger.LogInformation($"Some images for MultiImageRequest: {_multiImageGenerationState.State.RequestId} are still in progress");
            return new MultiImageQueryGrainResponse
            {
                Status = ImageGenerationStatus.InProgress
            };
        }

        var allImages = new List<ImageDescription>();
        var imageGenerationStates = new List<ImageGenerationStatus>();
        var errorMessages = new List<string>();

        foreach (var imageGenerationRequestId in _multiImageGenerationState.State.ImageGenerationRequestIds)
        {
            _logger.LogInformation($"Querying ImageGeneratorGrain for ImageGenerationRequestId: {imageGenerationRequestId}");
            
            var imageGeneratorGrain = GrainFactory.GetGrain<IImageGeneratorGrain>(imageGenerationRequestId);

            var response = await imageGeneratorGrain.QueryImageAsync();
            
            _logger.LogInformation($"Query response for ImageGenerationRequestId: {imageGenerationRequestId} is: {response}");
            _logger.LogInformation("t-02 Query response for ImageGenerationRequestId: {imageGenerationRequestId} is: {response}", imageGenerationRequestId, response);

            if (response is not ImageQueryGrainResponse grainResponse)
            {
                _logger.LogError($"Query response for ImageGenerationRequestId: {imageGenerationRequestId} is not of type ImageQueryGrainResponse");
                continue;
            }

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
            _logger.LogInformation($"Computed finalStatus : Some images for MultiImageRequest: {_multiImageGenerationState.State.RequestId} are still in progress");
            // If any of the statuses is InProgress, mark the final status as InProgress
            finalStatus = ImageGenerationStatus.InProgress;
        }
        else if (imageGenerationStates.All(state => state == ImageGenerationStatus.SuccessfulCompletion))
        {
            _logger.LogInformation($"Computed finalStatus : All images for MultiImageRequest: {_multiImageGenerationState.State.RequestId} are generated successfully");
            // If all statuses are SuccessfulCompletion, mark the final status as SuccessfulCompletion
            finalStatus = ImageGenerationStatus.SuccessfulCompletion;
        }
        else if (imageGenerationStates.All(state => state == ImageGenerationStatus.FailedCompletion))
        {
            _logger.LogInformation($"Computed finalStatus : All images for MultiImageRequest: {_multiImageGenerationState.State.RequestId} failed to generate");
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
            _logger.LogInformation($"All images for MultiImageRequest: {_multiImageGenerationState.State.RequestId} are generated successfully");
            return new MultiImageQueryGrainResponse
            {
                Images = allImages,
                Status = ImageGenerationStatus.SuccessfulCompletion
            };
        }
        else if (finalStatus == ImageGenerationStatus.InProgress)
        {
            _logger.LogInformation($"Some images for MultiImageRequest: {_multiImageGenerationState.State.RequestId} are still in progress");
            return new MultiImageQueryGrainResponse
            {
                Status = ImageGenerationStatus.InProgress
            };
        }
        else
        {
            _logger.LogInformation($"Some images for MultiImageRequest: {_multiImageGenerationState.State.RequestId} failed to generate");
            return new MultiImageQueryGrainResponse
            {
                Status = ImageGenerationStatus.FailedCompletion,
                Errors = errorMessages
            };
        }
    }
}