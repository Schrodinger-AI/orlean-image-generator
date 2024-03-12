using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Shared;
using Grains.usage_tracker;
using Grains.types;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Grains;

public class ImageGeneratorGrain : Grain, IImageGeneratorGrain, IDisposable
{
    private string apiKey;
    private IDisposable _timer;

    private IDisposable _reconcileCheckerTimer;

    private readonly IPersistentState<ImageGenerationState> _imageGenerationState;

    private readonly ILogger<SchedulerGrain> _logger;

    public ImageGeneratorGrain(
        [PersistentState("imageGenerationState", "MySqlSchrodingerImageStore")]
        IPersistentState<ImageGenerationState> imageGeneratorState, PromptBuilder promptBuilder,
        ILogger<SchedulerGrain> logger)
    {
        _imageGenerationState = imageGeneratorState;
        _logger = logger;
    }

    public override Task OnActivateAsync()
    {
        _logger.LogInformation("ImageGeneratorGrain - OnActivateAsync");
        _timer = RegisterTimer(TriggerImageGenerationAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        _reconcileCheckerTimer = RegisterTimer(CheckAndReportForInvalidStates, null, TimeSpan.Zero, TimeSpan.FromSeconds(300));
        return base.OnActivateAsync();
    }

    public async Task SetImageGenerationRequestData(string prompt, string imageRequestId, string parentRequestId)
    {
        _imageGenerationState.State.ParentRequestId = parentRequestId;
        _imageGenerationState.State.RequestId = imageRequestId;
        _imageGenerationState.State.Prompt = prompt;
        await _imageGenerationState.WriteStateAsync();
    }

    public async Task SetApiKey(string key)
    {
        _logger.LogInformation("ImageGeneratorGrain - Settting ApiKey: {} for imageGeneratorId: {}", key,
            _imageGenerationState.State.RequestId);
        apiKey = key;
    }

    private async Task CheckAndReportForInvalidStates(object state)
    {
        if (string.IsNullOrEmpty(apiKey) && _imageGenerationState.State.Status == ImageGenerationStatus.InProgress)
        {
            _logger.LogInformation("ImageGeneratorGrain - generatorId: {} : ApiKey is null", _imageGenerationState.State.RequestId);

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

        else if (_imageGenerationState.State.Status == ImageGenerationStatus.SuccessfulCompletion)
        {
            _logger.LogInformation("ImageGeneratorGrain - About to dispose _reconcileCheckerTimer for generatorId: {} as image generation is Successful", _imageGenerationState.State.RequestId);
            _reconcileCheckerTimer.Dispose();
            return;
        }
    }

    private async Task TriggerImageGenerationAsync(object state)
    {
        _logger.LogInformation("ImageGeneratorGrain - TriggerImageGenerationAsync with ApiKey: {}", apiKey);

        // Check if the API key exists in memory
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogInformation("ImageGeneratorGrain - generatorId: {} : ApiKey is null",
                _imageGenerationState.State.RequestId);
            // Handle the case where the API key does not exist or image-generation in-progress
            return;
        }

        if (_imageGenerationState.State.Status == ImageGenerationStatus.InProgress)
        {
            _logger.LogInformation("ImageGeneratorGrain - generatorId: {} , image generation is in progress",
                _imageGenerationState.State.RequestId);
            // Handle the case where the image generation is already in progress
            return;
        }

        // Check if the image generation is not already completed
        if (_imageGenerationState.State.Status == ImageGenerationStatus.SuccessfulCompletion)
        {
            _logger.LogInformation("ImageGeneratorGrain - generatorId: {} , image generation is successful",
                _imageGenerationState.State.RequestId);
            // Handle the case where the image generation is already completed
            _timer.Dispose();
            return;
        }

        _imageGenerationState.State.Status = ImageGenerationStatus.InProgress;
        await _imageGenerationState.WriteStateAsync();

        // Call GenerateImageFromPromptAsync with its arguments taken from the state and the API key taken from memory
        ImageGenerationGrainResponse imageGenerationResponse = await GenerateImageFromPromptAsync(
            _imageGenerationState.State.Prompt, _imageGenerationState.State.RequestId,
            _imageGenerationState.State.ParentRequestId);

        _logger.LogInformation("ImageGeneratorGrain - generatorId: {} , imageGenerationResponse: {}",
            _imageGenerationState.State.RequestId, imageGenerationResponse);

        //load the scheduler Grain and update with 
        var parentGeneratorGrain =
            GrainFactory.GetGrain<IMultiImageGeneratorGrain>(_imageGenerationState.State.ParentRequestId);
        var schedulerGrain = GrainFactory.GetGrain<IImageGenerationRequestStatusReceiver>("SchedulerGrain");

        if (imageGenerationResponse.IsSuccessful)
        {
            // Handle the case where the image generation is successful
            _timer.Dispose();

            _logger.LogInformation("ImageGeneratorGrain - generatorId: {} , image generation is successful",
                _imageGenerationState.State.RequestId);

            // notify about successful completion to parentGrain
            await parentGeneratorGrain.NotifyImageGenerationStatus(_imageGenerationState.State.RequestId,
                ImageGenerationStatus.SuccessfulCompletion, null);


            //notify the scheduler grain about the successful completion
            var requestStatus = new RequestStatus
            {
                RequestId = _imageGenerationState.State.RequestId,
                Status = RequestStatusEnum.Completed
            };

            await schedulerGrain.ReportCompletedImageGenerationRequestAsync(requestStatus);
        }
        else
        {
            _logger.LogInformation("ImageGeneratorGrain - generatorId: {} , image generation failed with Error: {}",
                _imageGenerationState.State.RequestId, imageGenerationResponse.Error);

            // notify about failed completion to parentGrain
            await parentGeneratorGrain.NotifyImageGenerationStatus(_imageGenerationState.State.RequestId,
                ImageGenerationStatus.FailedCompletion, imageGenerationResponse.Error);

            // notify the scheduler grain about the failed completion
            var requestStatus = new RequestStatus
            {
                RequestId = _imageGenerationState.State.RequestId,
                Status = RequestStatusEnum.Failed,
                Message = imageGenerationResponse.Error
            };

            await schedulerGrain.ReportFailedImageGenerationRequestAsync(requestStatus);
        }

        // set apiKey to null
        apiKey = null;
    }

    public async Task<ImageGenerationGrainResponse> GenerateImageFromPromptAsync(string prompt, string imageRequestId,
        string parentRequestId)
    {
        _logger.LogInformation(
            "ImageGeneratorGrain - generatorId: {} , GenerateImageFromPromptAsync invoked with prompt: {} \n",
            imageRequestId, prompt);

        try
        {
            _imageGenerationState.State.ParentRequestId = parentRequestId;
            _imageGenerationState.State.RequestId = imageRequestId;
            _imageGenerationState.State.Prompt = prompt;
            await _imageGenerationState.WriteStateAsync();

            // Start the image data generation process
            DalleResponse dalleResponse = await RunDalleAsync(prompt);

            _logger.LogInformation("ImageGeneratorGrain - generatorId: {} , dalleResponse: {}", imageRequestId,
                dalleResponse);

            // Extract the URL from the result
            string imageUrl = dalleResponse.Data[0].Url;

            // Convert the image URL to base64
            string base64Image = await ConvertImageUrlToBase64(imageUrl);

            // Generate the ImageQueryResponseOk
            var image = new ImageDescription
            {
                ExtraData = imageUrl,
                Image = base64Image,
            };

            // Store the image in the state
            _imageGenerationState.State.Image = image;

            // Persist the state to the database
            await _imageGenerationState.WriteStateAsync();

            _imageGenerationState.State.Status = ImageGenerationStatus.SuccessfulCompletion;

            // Store the task in a non-persistent dictionary
            await _imageGenerationState.WriteStateAsync();

            return new ImageGenerationGrainResponse
            {
                RequestId = imageRequestId,
                IsSuccessful = true,
                Error = null
            };
        }
        catch (Exception e)
        {
            _logger.LogError(
                "ImageGeneratorGrain - generatorId: {} , GenerateImageFromPromptAsync failed with Error: {}",
                imageRequestId, e.Message);
            _imageGenerationState.State.Status = ImageGenerationStatus.FailedCompletion;
            _imageGenerationState.State.Error = e.Message;
            await _imageGenerationState.WriteStateAsync();


            //load the scheduler Grain and update with 
            var parentGeneratorGrain = GrainFactory.GetGrain<IMultiImageGeneratorGrain>(_imageGenerationState.State.ParentRequestId);
            await parentGeneratorGrain.NotifyImageGenerationStatus(_imageGenerationState.State.RequestId, ImageGenerationStatus.FailedCompletion, e.Message);

            // notify the scheduler grain about the failed completion
            var requestStatus = new RequestStatus
            {
                RequestId = _imageGenerationState.State.RequestId,
                Status = RequestStatusEnum.Failed,
                Message = e.Message
            };

            var schedulerGrain = GrainFactory.GetGrain<IImageGenerationRequestStatusReceiver>("SchedulerGrain");
            await schedulerGrain.ReportFailedImageGenerationRequestAsync(requestStatus);

            return new ImageGenerationGrainResponse
            {
                RequestId = imageRequestId,
                IsSuccessful = false,
                Error = e.Message
            };
        }
    }

    public async Task<DalleResponse> RunDalleAsync(string prompt)
    {
        _logger.LogInformation(
            "ImageGeneratorGrain - generatorId: {} , about to call Dalle API to generate image for prompt: {}",
            _imageGenerationState.State.RequestId, prompt);

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                model = "dall-e-3",
                prompt = prompt,
                quality = "standard",
                n = 1
            }), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.openai.com/v1/images/generations", content);

            var jsonResponse = await response.Content.ReadAsStringAsync();
            DalleResponse dalleResponse = JsonConvert.DeserializeObject<DalleResponse>(jsonResponse);

            _logger.LogInformation("ImageGeneratorGrain - generatorId: {} , Dalle API response: {}",
                _imageGenerationState.State.RequestId, dalleResponse);

            return dalleResponse;
        }
    }

    public async Task<ImageQueryGrainResponse> QueryImageAsync()
    {
        if (_imageGenerationState.State.Status == ImageGenerationStatus.Dormant)
        {
            return new ImageQueryGrainResponse
            {
                Image = null,
                Status = ImageGenerationStatus.Dormant,
                Error = "Image generation not started"
            };
        }


        if (_imageGenerationState.State.Status == ImageGenerationStatus.InProgress)
        {
            return new ImageQueryGrainResponse
            {
                Image = null,
                Status = _imageGenerationState.State.Status,
                Error = "Image generation in progress"
            };
        }

        // Check if the ImageQueryResponse exists in the state
        if (_imageGenerationState.State.Status == ImageGenerationStatus.SuccessfulCompletion &&
            _imageGenerationState.State.Image != null)
        {
            return new ImageQueryGrainResponse
            {
                Image = _imageGenerationState.State.Image,
                Status = _imageGenerationState.State.Status,
                Error = null
            };
        }

        else if (_imageGenerationState.State.Status == ImageGenerationStatus.FailedCompletion)
        {
            return new ImageQueryGrainResponse
            {
                Image = null,
                Status = _imageGenerationState.State.Status,
                Error = _imageGenerationState.State.Error
            };
        }

        // Handle the case where none of the above conditions are met
        return new ImageQueryGrainResponse
        {
            Image = null,
            Status = ImageGenerationStatus.FailedCompletion,
            Error = "Unknown error - Image Not available"
        };
    }

    public async Task<ImageGenerationState> GetStateAsync()
    {
        return await Task.FromResult(_imageGenerationState.State);
    }

    public async Task<string> ConvertImageUrlToBase64(string imageUrl)
    {
        using (var httpClient = new HttpClient())
        {
            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
            using (var ms = new MemoryStream(imageBytes))
            {
                using (var output = new MemoryStream())
                {
                    using (var image = SixLabors.ImageSharp.Image.Load(ms))
                    {
                        image.Mutate(x => x.Resize(512, 512));
                        image.SaveAsJpeg(output, new JpegEncoder { Quality = 30 });
                        return "data:image/webp;base64," + Convert.ToBase64String(output.ToArray());
                    }
                }
            }
        }
    }

    public static int GetSizeOfBase64String(string base64String)
    {
        return (int)Math.Ceiling(base64String.Length * 4 / 3.0);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _reconcileCheckerTimer?.Dispose();
    }
}