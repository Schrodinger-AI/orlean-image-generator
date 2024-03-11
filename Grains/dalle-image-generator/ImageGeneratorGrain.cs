using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Shared;

namespace Grains;

public class ImageGeneratorGrain : Grain, IImageGeneratorGrain
{
    private Task<DalleResponse> _imageDataTask;

    private readonly IPersistentState<ImageGenerationState> _imageGenerationState;

    public ImageGeneratorGrain([PersistentState("imageGenerationState", "MySqlSchrodingerImageStore")] IPersistentState<ImageGenerationState> imageGeneratorState, PromptBuilder promptBuilder)
    {
        _imageGenerationState = imageGeneratorState;
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
            Task<DalleResponse> imageDataTask = RunDalleAsync(prompt);

            var _ = imageDataTask.ContinueWith(async task =>
            {
                if (task.IsFaulted)
                {
                    // Handle the error
                    Exception ex = task.Exception;

                    // TODO Call a function on a grain
                    var ParentGrain = GrainFactory.GetGrain<IMultiImageGeneratorGrain>(_imageGenerationState.State.ParentRequestId);
                    await ParentGrain.HandleImageGenerationNotification(new ImageGenerationNotification
                    {
                        RequestId = imageRequestId,
                        Status = ImageGenerationStatus.FailedCompletion,
                        Error = ex.Message
                    });

                    _imageGenerationState.State.Status = ImageGenerationStatus.FailedCompletion;

                    await _imageGenerationState.WriteStateAsync();
                }
                else
                {
                    // The task completed successfully
                    DalleResponse response = task.Result;

                    // Call a function on ParentGrain
                    var ParentGrain = GrainFactory.GetGrain<IMultiImageGeneratorGrain>(_imageGenerationState.State.ParentRequestId);
                    await ParentGrain.HandleImageGenerationNotification(new ImageGenerationNotification
                    {
                        RequestId = imageRequestId,
                        Status = ImageGenerationStatus.SuccessfulCompletion,
                    });
                    _imageGenerationState.State.Status = ImageGenerationStatus.SuccessfulCompletion;
                    
                    await _imageGenerationState.WriteStateAsync();
                }
            });

            // Store the task in a non-persistent dictionary
            _imageDataTask = imageDataTask;

            _imageGenerationState.State.Status = ImageGenerationStatus.InProgress;

            // Write the state to the storage provider
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
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
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

        else if (_imageDataTask != null)
        {
            try
            {
                // Wait for the task to complete and get the result
                DalleResponse result = await _imageDataTask;

                // Extract the URL from the result
                string imageUrl = result.Data[0].Url;

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

                return new ImageQueryGrainResponse
                {
                    Image = _imageGenerationState.State.Image,
                    Status = _imageGenerationState.State.Status,
                    Error = null
                };
            }
            catch (Exception e)
            {
                // Handle the error and return an ImageQueryResponseNotOk
                return new ImageQueryGrainResponse
                {
                    Image = null,
                    Status = _imageGenerationState.State.Status,
                    Error = e.Message
                };
            }
        }
        else
        {
            // Handle the error
            return new ImageQueryGrainResponse
            {
                Image = null,
                Status = _imageGenerationState.State.Status,
                Error = "Image request not found"
            };
        }
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