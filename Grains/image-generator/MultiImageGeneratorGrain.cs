using Grains.Constants;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Schrodinger.Backend.Abstractions.Constants;
using Schrodinger.Backend.Abstractions.Images;
using Schrodinger.Backend.Abstractions.Interfaces;
using Schrodinger.Backend.Abstractions.Prompter;
using Attribute = Schrodinger.Backend.Abstractions.Images.Attribute;

namespace Grains.image_generator;

public class MultiImageGeneratorGrain : Grain, IMultiImageGeneratorGrain
{
    private readonly ILogger<MultiImageGeneratorGrain> _logger;

    private readonly IPersistentState<MultiImageGenerationState> _multiImageGenerationState;

    public MultiImageGeneratorGrain(
        [PersistentState(
            "multiImageGenerationState",
            "MySqlSchrodingerImageStore"
        )]
            IPersistentState<MultiImageGenerationState> multiImageGenerationState,
        ILogger<MultiImageGeneratorGrain> logger
    )
    {
        _multiImageGenerationState = multiImageGenerationState;
        _logger = logger;
    }

    public new virtual IGrainFactory GrainFactory
    {
        get => base.GrainFactory;
    }

    public Task<ImageGenerationStatus> GetCurrentImageGenerationStatus()
    {
        var statusArray = new List<ImageGenerationStatus>();

        if (
            _multiImageGenerationState.State.ErrorCode
            == ImageGenerationErrorCode.content_violation
        )
        {
            _logger.LogInformation(
                $"Computed finalStatus : MultiImageRequest: {_multiImageGenerationState.State.RequestId} failed due to content violation"
            );
            return Task.FromResult(ImageGenerationStatus.FailedCompletion);
        }

        //get child grain references
        foreach (
            var imageGenerationRequestId in _multiImageGenerationState
                .State
                .ImageGenerationRequestIds
        )
        {
            //check the imageTracker for imageGenerationRequestId
            var imageGenerationTracker = _multiImageGenerationState
                .State
                .imageGenerationTrackers[imageGenerationRequestId];

            //if the status is not successful, return false
            // if status is inProgress, break loop and return the Status as InProgress
            if (
                imageGenerationTracker.Status
                == ImageGenerationStatus.InProgress
            )
            {
                return Task.FromResult(imageGenerationTracker.Status);
            }

            statusArray.Add(imageGenerationTracker.Status);
        }

        // if all statuses are successful, return true
        if (
            statusArray.All(status =>
                status == ImageGenerationStatus.SuccessfulCompletion
            )
        )
        {
            return Task.FromResult(ImageGenerationStatus.SuccessfulCompletion);
        }

        if (
            statusArray.Any(state =>
                state == ImageGenerationStatus.FailedCompletion
            )
        )
        {
            // loop thru imageGenerationTrackers and check for value of property ErrorCode and if any one is equals to content_violation then set finalStatus as FailedCompletion
            foreach (
                var tracker in _multiImageGenerationState
                    .State
                    .imageGenerationTrackers
                    .Values
            )
            {
                if (
                    tracker.ErrorCode
                    == ImageGenerationErrorCode.content_violation
                )
                {
                    return Task.FromResult(
                        ImageGenerationStatus.FailedCompletion
                    );
                }
            }
        }

        // else return inProgress
        return Task.FromResult(ImageGenerationStatus.InProgress);
    }

    public virtual async Task NotifyImageGenerationStatus(
        string imageRequestId,
        ImageGenerationStatus status,
        string? error,
        ImageGenerationErrorCode? imageGenerationErrorCode
    )
    {
        _logger.LogInformation(
            $"NotifyImageGenerationStatus called with requestId: {imageRequestId}, status: {status}, error: {error}"
        );

        var imageGenerationNotification = new ImageGenerationTracker
        {
            RequestId = imageRequestId,
            Status = status,
            Error = error
        };

        if (
            imageGenerationErrorCode
            == ImageGenerationErrorCode.content_violation
        )
        {
            _multiImageGenerationState.State.ErrorCode =
                ImageGenerationErrorCode.content_violation;
        }

        _multiImageGenerationState.State.imageGenerationTrackers[
            imageGenerationNotification.RequestId
        ] = imageGenerationNotification;

        ImageGenerationStatus currentStatus =
            await GetCurrentImageGenerationStatus();
        _multiImageGenerationState.State.ImageGenerationStatus = currentStatus;

        await _multiImageGenerationState.WriteStateAsync();
    }

    public virtual async Task<string> GeneratePromptAsync(
        List<Attribute> attributes
    )
    {
        var grain = GrainFactory.GetGrain<IConfiguratorGrain>(
            GrainConstants.ConfiguratorIdentifier
        );
        var curConfigId = await grain.GetCurrentConfigIdAsync();
        var prompterGrain = GrainFactory.GetGrain<IPrompterGrain>(curConfigId);
        return await prompterGrain.GeneratePromptAsync(
            new PromptGenerationRequestDto { NewAttributes = attributes }
        );
    }

    public async Task<MultiImageGenerationGrainResponseDto> GenerateMultipleImagesAsync(
        List<Attribute> traits,
        int NumberOfImages,
        string multiImageRequestId
    )
    {
        _logger.LogInformation(
            $"GenerateMultipleImagesAsync called with traits: {traits}, NumberOfImages: {NumberOfImages}, multiImageRequestId: {multiImageRequestId}"
        );

        try
        {
            _multiImageGenerationState.State.RequestId = multiImageRequestId;
            var IsSuccessful = true;

            // Extract trait names from the request
            _multiImageGenerationState.State.Traits = traits;
            var prompt = await GeneratePromptAsync(traits);

            _logger.LogInformation(
                $"For MultiImageRequest: {multiImageRequestId} Prompt generated: {prompt}"
            );

            _multiImageGenerationState.State.Prompt = prompt;
            var schedulerGrain = GrainFactory.GetGrain<ISchedulerGrain>(
                "SchedulerGrain"
            );

            //get timestamp
            var requestTimestamp = DateTime.UtcNow;
            var unixTimestamp = (
                (DateTimeOffset)requestTimestamp
            ).ToUnixTimeSeconds();

            for (var i = 0; i < NumberOfImages; i++)
            {
                //generate a new UUID with a prefix of "imageRequest"
                var imageRequestId =
                    "ImageRequest_" + Guid.NewGuid().ToString();

                var imageGeneratorGrain =
                    GrainFactory.GetGrain<IImageGeneratorGrain>(imageRequestId);

                await imageGeneratorGrain.SetImageGenerationRequestData(
                    prompt,
                    imageRequestId,
                    multiImageRequestId
                );

                _multiImageGenerationState.State.imageGenerationTrackers[
                    imageRequestId
                ] = new ImageGenerationTracker
                {
                    RequestId = imageRequestId,
                    Status = ImageGenerationStatus.InProgress
                };

                _multiImageGenerationState.State.ImageGenerationRequestIds.Add(
                    imageRequestId
                );

                _logger.LogInformation(
                    $"For MultiImageRequest: {multiImageRequestId} ImageRequest: {imageRequestId} added to the list of imageGenerationRequestIds"
                );

                await schedulerGrain.AddImageGenerationRequest(
                    multiImageRequestId,
                    imageRequestId,
                    unixTimestamp
                );
            }

            _multiImageGenerationState.State.IsSuccessful = IsSuccessful;
            await _multiImageGenerationState.WriteStateAsync();

            return new MultiImageGenerationGrainResponseDto
            {
                RequestId = multiImageRequestId,
                Traits = traits,
                Prompt = prompt,
                IsSuccessful = true,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                $"Error occurred in GenerateMultipleImagesAsync for MultiImageRequest: {multiImageRequestId}"
            );
            _multiImageGenerationState.State.IsSuccessful = false;
            await _multiImageGenerationState.WriteStateAsync();
            return new MultiImageGenerationGrainResponseDto
            {
                RequestId = multiImageRequestId,
                Traits = traits,
                Prompt = "",
                IsSuccessful = false,
                Errors = [ex.Message]
            };
        }
    }

    public async Task UpdatePromptAndAttributes(
        string prompt,
        List<Attribute> attributes
    )
    {
        _multiImageGenerationState.State.Prompt = prompt;
        _multiImageGenerationState.State.Traits = attributes;
        await _multiImageGenerationState.WriteStateAsync();
    }

    public async Task<MultiImageQueryGrainResponseDto> QueryMultipleImagesAsync()
    {
        _logger.LogInformation(
            $"QueryMultipleImagesAsync called for MultiImageRequest: {_multiImageGenerationState.State.RequestId}"
        );

        if (string.IsNullOrEmpty(_multiImageGenerationState.State.Prompt))
        {
            _logger.LogInformation(
                $"MultiImageRequest: {_multiImageGenerationState.State.RequestId} is uninitialized"
            );
            return new MultiImageQueryGrainResponseDto()
            {
                Uninitialized = true
            };
        }

        ImageGenerationStatus imageGenerationStatus =
            await GetCurrentImageGenerationStatus();

        if (imageGenerationStatus == ImageGenerationStatus.InProgress)
        {
            _logger.LogInformation(
                $"Some images for MultiImageRequest: {_multiImageGenerationState.State.RequestId} are still in progress"
            );
            return new MultiImageQueryGrainResponseDto
            {
                Status = ImageGenerationStatus.InProgress
            };
        }

        if (imageGenerationStatus == ImageGenerationStatus.FailedCompletion)
        {
            _logger.LogInformation(
                $"Some images for MultiImageRequest: {_multiImageGenerationState.State.RequestId} failed to generate"
            );

            var errors = _multiImageGenerationState
                .State.imageGenerationTrackers.Select(imageGenerationTracker =>
                    imageGenerationTracker.Value.Error
                )
                .ToList();

            return new MultiImageQueryGrainResponseDto
            {
                Status = ImageGenerationStatus.FailedCompletion,
                Errors = errors,
                ErrorCode =
                    _multiImageGenerationState.State.ErrorCode.ToString()
            };
        }

        try
        {
            var allImages = new List<ImageDescription>();

            foreach (
                var imageGenerationRequestId in _multiImageGenerationState
                    .State
                    .ImageGenerationRequestIds
            )
            {
                _logger.LogInformation(
                    $"Querying ImageGeneratorGrain for ImageGenerationRequestId: {imageGenerationRequestId}"
                );

                var imageGeneratorGrain =
                    GrainFactory.GetGrain<IImageGeneratorGrain>(
                        imageGenerationRequestId
                    );

                var response = await imageGeneratorGrain.QueryImageAsync();

                _logger.LogInformation(
                    $"Query response for ImageGenerationRequestId: {imageGenerationRequestId} is: {response.Status}"
                );

                if (response is not { } grainResponse)
                {
                    _logger.LogError(
                        $"Query response for ImageGenerationRequestId: {imageGenerationRequestId} is not of type ImageQueryGrainResponse"
                    );
                    continue;
                }

                if (
                    grainResponse
                    is not {
                        Status: ImageGenerationStatus.SuccessfulCompletion,
                        Image: not null
                    }
                )
                    continue;
                grainResponse.Image.Attributes = _multiImageGenerationState
                    .State
                    .Traits;
                allImages.Add(grainResponse.Image);
            }

            if (
                allImages.Count
                != _multiImageGenerationState
                    .State
                    .ImageGenerationRequestIds
                    .Count
            )
            {
                return new MultiImageQueryGrainResponseDto
                {
                    Status = ImageGenerationStatus.FailedCompletion,
                    Errors = ["ImageData not found"]
                };
            }

            //prepare MultiImageQueryGrainResponse based on finalStatus computed from imageGenerationTrackers
            _logger.LogInformation(
                $"All images for MultiImageRequest: {_multiImageGenerationState.State.RequestId} are generated successfully"
            );
            return new MultiImageQueryGrainResponseDto
            {
                Images = allImages,
                Status = ImageGenerationStatus.SuccessfulCompletion
            };
        }
        catch (Exception e)
        {
            _logger.LogError(
                $"Error occurred in QueryMultipleImagesAsync for MultiImageRequest: {_multiImageGenerationState.State.RequestId}, exception: {e.Message}"
            );
            return new MultiImageQueryGrainResponseDto
            {
                Status = ImageGenerationStatus.InProgress
            };
        }
    }

    public Task<bool> IsAlreadySubmitted()
    {
        return Task.FromResult(
            !string.IsNullOrEmpty(_multiImageGenerationState.State.RequestId)
        );
    }
}
