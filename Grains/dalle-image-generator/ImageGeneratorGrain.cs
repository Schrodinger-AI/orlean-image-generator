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

namespace Grains;

public class ImageGeneratorGrain : Grain, IImageGeneratorGrain
{
    private string apiKey;
    private IDisposable _timer;

    private readonly IPersistentState<ImageGenerationState> _imageGenerationState;

    public ImageGeneratorGrain([PersistentState("imageGenerationState", "MySqlSchrodingerImageStore")] IPersistentState<ImageGenerationState> imageGeneratorState, PromptBuilder promptBuilder)
    {
        _imageGenerationState = imageGeneratorState;
    }

    public override Task OnActivateAsync()
    {
        _timer = RegisterTimer(TriggerImageGenerationAsync, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
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
        apiKey = key;
    }

    private async Task TriggerImageGenerationAsync(object state)
    {
        // Check if the API key exists in memory
        if (string.IsNullOrEmpty(apiKey))
        {
            // Handle the case where the API key does not exist
            return;
        }

        // Check if the image generation is not already completed
        if (_imageGenerationState.State.Status == ImageGenerationStatus.SuccessfulCompletion)
        {
            // Handle the case where the image generation is already completed
            _timer.Dispose();
            return;
        }

        // Call GenerateImageFromPromptAsync with its arguments taken from the state and the API key taken from memory
        ImageGenerationGrainResponse imageGenerationResponse = await GenerateImageFromPromptAsync(_imageGenerationState.State.Prompt, _imageGenerationState.State.RequestId, _imageGenerationState.State.ParentRequestId);

        //load the scheduler Grain and update with 
        var parentGeneratorGrain = GrainFactory.GetGrain<IMultiImageGeneratorGrain>(_imageGenerationState.State.ParentRequestId);
        var schedulerGrain = GrainFactory.GetGrain<IImageGenerationRequestStatusReceiver>("SchedulerGrain");

        if (imageGenerationResponse.IsSuccessful)
        {
            // Handle the case where the image generation is successful
            _timer.Dispose();

            // notify about successful completion to parentGrain
            await parentGeneratorGrain.NotifyImageGenerationStatus(_imageGenerationState.State.RequestId, ImageGenerationStatus.SuccessfulCompletion, null);


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
            // notify about failed completion to parentGrain
            await parentGeneratorGrain.NotifyImageGenerationStatus(_imageGenerationState.State.RequestId, ImageGenerationStatus.FailedCompletion, imageGenerationResponse.Error);

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

    public async Task<ImageGenerationGrainResponse> GenerateImageFromPromptAsync(string prompt, string imageRequestId, string parentRequestId)
    {
        try
        {
            _imageGenerationState.State.ParentRequestId = parentRequestId;
            _imageGenerationState.State.RequestId = imageRequestId;
            _imageGenerationState.State.Prompt = prompt;
            await _imageGenerationState.WriteStateAsync();

            // Start the image data generation process
            DalleResponse dalleResponse = await RunDalleAsync(prompt);

            // Extract the URL from the result
            string imageUrl = dalleResponse.Data[0].Url;

            // Convert the image URL to base64
            string base64Image = await ConvertImageUrlToBase64(imageUrl);

            Console.WriteLine("Size of base64 string: " + GetSizeOfBase64String(base64Image) + " bytes");

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
            _imageGenerationState.State.Status = ImageGenerationStatus.FailedCompletion;
            await _imageGenerationState.WriteStateAsync();

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
        Console.WriteLine("about to call Dalle API to generate image for prompt: " + prompt);

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

            Console.WriteLine($"response.data from dalle: {jsonResponse}");

            DalleResponse dalleResponse = JsonConvert.DeserializeObject<DalleResponse>(jsonResponse);

            Console.WriteLine("dalleResponse: " + dalleResponse);

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
        if (_imageGenerationState.State.Status == ImageGenerationStatus.SuccessfulCompletion && _imageGenerationState.State.Image != null)
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
                        image.SaveAsJpeg(output);
                        return Convert.ToBase64String(output.ToArray());
                    }
                }
            }
        }
    }

    public static int GetSizeOfBase64String(string base64String)
    {
        return (int)Math.Ceiling(base64String.Length * 4 / 3.0);
    }
}