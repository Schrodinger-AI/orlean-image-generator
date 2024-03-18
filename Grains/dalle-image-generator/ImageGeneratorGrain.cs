using Orleans;
using Orleans.Runtime;
using SixLabors.ImageSharp.Processing;
using Shared;
using Grains.usage_tracker;
using Grains.types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Grains;

public class ImageGeneratorGrain : Grain, IImageGeneratorGrain, IDisposable
{
    private string _apiKey;
    private ImageGenerationServiceProvider _imageGenerationServiceProvider;
    private IDisposable _timer;

    private readonly IPersistentState<ImageGenerationState> _imageGenerationState;

    private readonly ImageSettings _imageSettings;

    private readonly DalleOpenAIImageGenerator _dalleOpenAiImageGenerator;
    
    private readonly AzureOpenAIImageGenerator _azureOpenAiImageGenerator;

    private readonly ILogger<ImageGeneratorGrain> _logger;

    public ImageGeneratorGrain(
        [PersistentState("imageGenerationState", "MySqlSchrodingerImageStore")]
        IPersistentState<ImageGenerationState> imageGeneratorState,
        IOptions<ImageSettings> imageSettingsOptions,
        DalleOpenAIImageGenerator dalleOpenAiImageGenerator,
        AzureOpenAIImageGenerator azureOpenAiImageGenerator,
        ILogger<ImageGeneratorGrain> logger)
    {
        _imageGenerationState = imageGeneratorState;
        _logger = logger;
        _imageSettings = imageSettingsOptions.Value;
        _dalleOpenAiImageGenerator = dalleOpenAiImageGenerator;
        _azureOpenAiImageGenerator = azureOpenAiImageGenerator;
        var imgS = Newtonsoft.Json.JsonConvert.SerializeObject(_imageSettings);
        _logger.LogInformation($"ImageGeneratorGrain Constructor : _imageSettings are: ${imgS}");
    }

    public override async Task OnActivateAsync()
    {
        _logger.LogInformation("ImageGeneratorGrain - OnActivateAsync");
        _timer = RegisterTimer(asyncCallback: _ => this.AsReference<IImageGeneratorGrain>().TriggerImageGenerationAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        await CheckAndReportForInvalidStates();
        _logger.LogInformation($"ImageGeneratorGrain - OnActivateAsync - ImageSettings are: {_imageSettings}");
        await base.OnActivateAsync();
    }

    public async Task SetImageGenerationRequestData(string prompt, string imageRequestId, string parentRequestId)
    {
        _imageGenerationState.State.ParentRequestId = parentRequestId;
        _imageGenerationState.State.RequestId = imageRequestId;
        _imageGenerationState.State.Prompt = prompt;
        await _imageGenerationState.WriteStateAsync();
    }

    public async Task SetImageGenerationServiceProvider(string apiKey, ImageGenerationServiceProvider serviceProvider)
    {
        _logger.LogInformation($"ImageGeneratorGrain - Setting ImageGenerationServiceProvider: {serviceProvider} for imageGeneratorId: {_imageGenerationState.State.RequestId}");
        _apiKey = apiKey;
        _imageGenerationServiceProvider = serviceProvider;
        await Task.CompletedTask;
    }

    private async Task CheckAndReportForInvalidStates()
    {
        if (string.IsNullOrEmpty(_apiKey) && _imageGenerationState.State.Status == ImageGenerationStatus.InProgress)
        {
            _logger.LogInformation($"ImageGeneratorGrain - generatorId: {_imageGenerationState.State.RequestId} : ApiKey is null");

            // Handle the case where the API key does not exist or image-generation in-progress
            _imageGenerationState.State.Status = ImageGenerationStatus.FailedCompletion;
            _imageGenerationState.State.Error = "ImageGeneration is in invalidState - resetting to FailedCompletion";
            await _imageGenerationState.WriteStateAsync();

            var parentGeneratorGrain = GrainFactory.GetGrain<IMultiImageGeneratorGrain>(_imageGenerationState.State.ParentRequestId);
            var schedulerGrain = GrainFactory.GetGrain<IImageGenerationRequestStatusReceiver>("SchedulerGrain");

            // notify about failed completion to parentGrain
            await parentGeneratorGrain.NotifyImageGenerationStatus(_imageGenerationState.State.RequestId, ImageGenerationStatus.FailedCompletion, _imageGenerationState.State.Error);
            // notify the scheduler grain about the failed completion
            var requestStatus = new RequestStatus
            {
                RequestId = _imageGenerationState.State.RequestId,
                Status = RequestStatusEnum.Failed,
                Message = _imageGenerationState.State.Error
            };
            await schedulerGrain.ReportFailedImageGenerationRequestAsync(requestStatus);
            return;
        }
    }

    public async Task TriggerImageGenerationAsync()
    {
        _logger.LogInformation($"ImageGeneratorGrain - TriggerImageGenerationAsync with ApiKey: {_apiKey}");

        // Check if the API key exists in memory
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogInformation($"ImageGeneratorGrain - generatorId: {_imageGenerationState.State.RequestId} : ApiKey is null");
            // Handle the case where the API key does not exist or image-generation in-progress
            return;
        }

        if (_imageGenerationState.State.Status == ImageGenerationStatus.InProgress)
        {
            _logger.LogInformation($"ImageGeneratorGrain - generatorId: {_imageGenerationState.State.RequestId} , image generation is in progress");
            // Handle the case where the image generation is already in progress
            return;
        }

        // Check if the image generation is not already completed
        if (_imageGenerationState.State.Status == ImageGenerationStatus.SuccessfulCompletion)
        {
            _logger.LogInformation($"ImageGeneratorGrain - generatorId: {_imageGenerationState.State.RequestId} , image generation is successful");
            // Handle the case where the image generation is already completed
            _timer.Dispose();
            return;
        }

        _imageGenerationState.State.Status = ImageGenerationStatus.InProgress;
        _imageGenerationState.State.ServiceProvider = _imageGenerationServiceProvider;
        await _imageGenerationState.WriteStateAsync();

        // Call GenerateImageFromPromptAsync with its arguments taken from the state and the API key taken from memory
        var imageGenerationResponse = await GenerateImageFromPromptAsync(
            _imageGenerationState.State.Prompt, _imageGenerationState.State.RequestId,
            _imageGenerationState.State.ParentRequestId);

        _logger.LogInformation($"ImageGeneratorGrain - generatorId: {_imageGenerationState.State.RequestId} , imageGenerationResponse: {imageGenerationResponse}");

        //load the scheduler Grain and update with 
        var parentGeneratorGrain =
            GrainFactory.GetGrain<IMultiImageGeneratorGrain>(_imageGenerationState.State.ParentRequestId);
        var schedulerGrain = GrainFactory.GetGrain<IImageGenerationRequestStatusReceiver>("SchedulerGrain");

        if (imageGenerationResponse.IsSuccessful)
        {
            // Handle the case where the image generation is successful
            _timer.Dispose();

            _logger.LogInformation($"ImageGeneratorGrain - generatorId: {_imageGenerationState.State.RequestId} , image generation is successful");

            // notify about successful completion to parentGrain
            await parentGeneratorGrain.NotifyImageGenerationStatus(_imageGenerationState.State.RequestId,
                ImageGenerationStatus.SuccessfulCompletion, null);


            //notify the scheduler grain about the successful completion
            var requestStatus = new RequestStatus
            {
                RequestId = _imageGenerationState.State.RequestId,
                Status = RequestStatusEnum.Completed,
                RequestTimestamp = imageGenerationResponse.ImageGenerationRequestTimestamp
            };

            await schedulerGrain.ReportCompletedImageGenerationRequestAsync(requestStatus);
        }
        else
        {
            _logger.LogInformation($"ImageGeneratorGrain - generatorId: {_imageGenerationState.State.RequestId} , image generation failed with Error: {imageGenerationResponse.Error}");

            // notify about failed completion to parentGrain
            await parentGeneratorGrain.NotifyImageGenerationStatus(_imageGenerationState.State.RequestId,
                ImageGenerationStatus.FailedCompletion, imageGenerationResponse.Error);

            // notify the scheduler grain about the failed completion
            var requestStatus = new RequestStatus
            {
                RequestId = _imageGenerationState.State.RequestId,
                Status = RequestStatusEnum.Failed,
                Message = imageGenerationResponse.Error,
                RequestTimestamp = imageGenerationResponse.ImageGenerationRequestTimestamp,
                ErrorCode = imageGenerationResponse.ErrorCode
            };

            await schedulerGrain.ReportFailedImageGenerationRequestAsync(requestStatus);
        }

        // set apiKey to null
        _apiKey = null;
    }

    private long GetCurrentUTCTimeInSeconds()
    {
        //get timestamp
        var requestTimestamp = DateTime.UtcNow;
        var unixTimestamp = ((DateTimeOffset)requestTimestamp).ToUnixTimeSeconds();
        return unixTimestamp;
    }

    public async Task<ImageGenerationGrainResponse> GenerateImageFromPromptAsync(string prompt, string imageRequestId,
        string parentRequestId)
    {
        _logger.LogInformation(
            $"ImageGeneratorGrain - generatorId: {imageRequestId} , GenerateImageFromPromptAsync invoked with prompt: {prompt}");
        var imageGenerationRequestTimestamp = GetCurrentUTCTimeInSeconds();
        try
        {
            _imageGenerationState.State.ParentRequestId = parentRequestId;
            _imageGenerationState.State.RequestId = imageRequestId;
            _imageGenerationState.State.Prompt = prompt;

            ImageGenerationResponse imageGenerationResponse;
            
            if (_imageGenerationServiceProvider == ImageGenerationServiceProvider.DalleOpenAI)
            {
                // Start the image data generation process
                imageGenerationResponse = await _dalleOpenAiImageGenerator.RunImageGenerationAsync(prompt, _apiKey, 1, _imageGenerationState.State.RequestId);
            }
            else if (_imageGenerationServiceProvider == ImageGenerationServiceProvider.AzureOpenAI)
            {
                // Start the image data generation process
                imageGenerationResponse = await _azureOpenAiImageGenerator.RunImageGenerationAsync(prompt, _apiKey, 1, _imageGenerationState.State.RequestId);
            }
            else
            {
                throw new Exception("Invalid ImageGenerationServiceProvider");
            }

            _logger.LogInformation($"ImageGeneratorGrain - generatorId: {imageRequestId} , imageGenerationResponse: {imageGenerationResponse}");

            _logger.LogInformation(
                $"ImageGeneratorGrain - generatorId: {imageRequestId} , imageGenerationResponse: {imageGenerationResponse}");

            _logger.LogDebug(imageGenerationResponse.ToString());
            // Extract the URL from the result
            var imageUrl = imageGenerationResponse.Data[0].Url;

            // Convert the image URL to base64
            var base64Image = await ConvertImageUrlToBase64(imageUrl);

            // Generate the ImageQueryResponseOk
            var image = new ImageDescription
            {
                ExtraData = prompt,
                Image = base64Image,
            };

            // Store the image in the state
            _imageGenerationState.State.Image = image;
            _imageGenerationState.State.Status = ImageGenerationStatus.SuccessfulCompletion;
            _imageGenerationState.State.ImageGenerationTimestamp = imageGenerationRequestTimestamp;

            // Store the task in a non-persistent dictionary
            await _imageGenerationState.WriteStateAsync();

            return new ImageGenerationGrainResponse
            {
                RequestId = imageRequestId,
                IsSuccessful = true,
                Error = null,
                ImageGenerationRequestTimestamp = imageGenerationRequestTimestamp,
                ErrorCode = null
            };
        }
        catch (Exception e)
        {
            _logger.LogError(
                $"ImageGeneratorGrain - generatorId: {imageRequestId} , GenerateImageFromPromptAsync failed with Error: {e.Message}");
            _imageGenerationState.State.Status = ImageGenerationStatus.FailedCompletion;
            _imageGenerationState.State.Error = e.Message;
            await _imageGenerationState.WriteStateAsync();
            
            //load the scheduler Grain and update with 
            var parentGeneratorGrain = GrainFactory.GetGrain<IMultiImageGeneratorGrain>(_imageGenerationState.State.ParentRequestId);
            await parentGeneratorGrain.NotifyImageGenerationStatus(_imageGenerationState.State.RequestId, ImageGenerationStatus.FailedCompletion, e.Message);

            ImageGenerationErrorCode? imageGenerationErrorCode = null;
            var imageGenerationException = e as ImageGenerationException;
            if (imageGenerationException != null)
            {
                imageGenerationErrorCode = imageGenerationException.ErrorCode;
            }
            
            return new ImageGenerationGrainResponse
            {
                RequestId = imageRequestId,
                IsSuccessful = false,
                Error = e.Message,
                ImageGenerationRequestTimestamp = imageGenerationRequestTimestamp,
                ErrorCode = imageGenerationErrorCode
            };
        }
    }
    
    public async Task<ImageQueryGrainResponse> QueryImageAsync()
    {
        return _imageGenerationState.State.Status switch
        {
            ImageGenerationStatus.Dormant => new ImageQueryGrainResponse
            {
                Image = null, Status = ImageGenerationStatus.Dormant, Error = "Image generation not started"
            },
            ImageGenerationStatus.InProgress => new ImageQueryGrainResponse
            {
                Image = null, Status = _imageGenerationState.State.Status, Error = "Image generation in progress"
            },
            // Check if the ImageQueryResponse exists in the state
            ImageGenerationStatus.SuccessfulCompletion when _imageGenerationState.State.Image != null => new
                ImageQueryGrainResponse
                {
                    Image = _imageGenerationState.State.Image, Status = _imageGenerationState.State.Status, Error = null
                },
            ImageGenerationStatus.FailedCompletion => new ImageQueryGrainResponse
            {
                Image = null, Status = _imageGenerationState.State.Status, Error = _imageGenerationState.State.Error
            },
            _ => new ImageQueryGrainResponse
            {
                Image = null,
                Status = ImageGenerationStatus.FailedCompletion,
                Error = "Unknown error - Image Not available"
            }
        };
    }

    public async Task<ImageGenerationState> GetStateAsync()
    {
        return await Task.FromResult(_imageGenerationState.State);
    }

    private async Task<string> ConvertImageUrlToBase64(string imageUrl)
    {
        using var httpClient = new HttpClient();
        var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
        using var ms = new MemoryStream(imageBytes);
        using var output = new MemoryStream();
        using var image = SixLabors.ImageSharp.Image.Load(ms);
        image.Mutate(x => x.Resize(_imageSettings.Width, _imageSettings.Height));
        image.Save(output, new SixLabors.ImageSharp.Formats.Webp.WebpEncoder { Quality = _imageSettings.Quality });
        return "data:image/webp;base64," + Convert.ToBase64String(output.ToArray());
    }


    public void Dispose()
    {
        _timer?.Dispose();
    }
}