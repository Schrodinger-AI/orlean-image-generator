using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Shared;

namespace Grains;

public class ImageGeneratorGrain : Grain, IImageGeneratorGrain
{
    private readonly PromptBuilder _promptBuilder;

    private Dictionary<string, Task<DalleResponse>> _imageDataTaskMap = new Dictionary<string, Task<DalleResponse>>();

    private readonly IPersistentState<ImageGenerationState> _imageGenerationState;

    public ImageGeneratorGrain([PersistentState("imageGenerationState", "MySqlSchrodingerImageStore")] IPersistentState<ImageGenerationState> imageGeneratorState, PromptBuilder promptBuilder)
    {
        _promptBuilder = promptBuilder;
        _imageGenerationState = imageGeneratorState;
    }

    public async Task<string> generatePrompt(ImageGenerationRequest imageGenerationRequest)
    {
        List<Trait> newTraits = imageGenerationRequest.NewTraits;
        List<Trait> baseTraits = imageGenerationRequest.BaseImage.Traits;

        //collect the newTraits from the request and combine it with trats from the base image
        IEnumerable<Trait> traits = newTraits.Concat(baseTraits);

        // Extract trait names from the request
        Dictionary<string, TraitEntry> traitDefinitions = await lookupTraitDefinitions(traits.ToList());

        string prompt = await generatePrompt(traits.ToList(), traitDefinitions);

        return prompt;
    }

    public async Task<ImageGenerationResponse> generateImageAsync(ImageGenerationRequest request, string imageRequestId)
    {
        try
        {
            string finalPrompt = await generatePrompt(request);

            // Start the image data generation process
            Task<DalleResponse> imageDataTask = RunDalleAsync(finalPrompt);

            // Store the task in a non-persistent dictionary
            _imageDataTaskMap[imageRequestId] = imageDataTask;

            // Store the traits in the image generation request map
            _imageGenerationState.State.ImageGenerationRequestMap[imageRequestId] = request;

            // Write the state to the storage provider
            await _imageGenerationState.WriteStateAsync();

            var response = new ImageGenerationResponseOk
            {
                RequestId = imageRequestId
            };
            return response;
        }
        catch (Exception e)
        {
            return new ImageGenerationResponseNotOk { Error = e.Message };
        }
    }

    public async Task<DalleResponse> RunDalleAsync(string prompt)
    {
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

            return dalleResponse;
        }
    }


    public async Task<ImageQueryResponse> queryImage(string imageRequestId)
    {
        if (_imageDataTaskMap.TryGetValue(imageRequestId, out Task<DalleResponse> imageDataTask))
        {
            try
            {
                // Wait for the task to complete and get the result
                DalleResponse result = await imageDataTask;

                // Extract the URL from the result
                string imageUrl = result.Data[0].Url;

                // Convert the image URL to base64
                string base64Image = await ConvertImageUrlToBase64(imageUrl);

                // Store the base64 image in the grain state
                _imageGenerationState.State.ImageMap[imageRequestId] = base64Image;
                await _imageGenerationState.WriteStateAsync();

                // Get the traits from the ImageGenerationRequest
                var request = _imageGenerationState.State.ImageGenerationRequestMap[imageRequestId];
                var traits = request.NewTraits.Concat(request.BaseImage.Traits).ToList();

                // Generate the ImageQueryResponseOk
                var response = new ImageQueryResponseOk
                {
                    Images = new List<ImageDescription>
                {
                    new ImageDescription
                    {
                        ExtraData = imageUrl,
                        Image = base64Image,
                        Traits = traits
                    }
                }
                };
                return response;
            }
            catch (Exception e)
            {
                // Handle the error and return an ImageQueryResponseNotOk
                return new ImageQueryResponseNotOk { Error = e.Message };
            }
        }
        else
        {
            // Handle the error
            return new ImageQueryResponseNotOk { Error = "Image request not found" };
        }
    }

    public async Task<string> ConvertImageUrlToBase64(string imageUrl)
    {
        using (var client = new HttpClient())
        {
            byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);
            string base64Image = Convert.ToBase64String(imageBytes);
            return base64Image;
        }
    }

    public async Task<Dictionary<string, TraitEntry>> lookupTraitDefinitions(List<Trait> requestTraits)
    {
        // Extract trait names from the request
        var traitNames = requestTraits.Select(t => t.Name).ToList();

        // Get a reference to the TraitConfigGrain
        var traitConfigGrain = GrainFactory.GetGrain<ITraitConfigGrain>("traitConfigGrain");

        // Retrieve the trait definitions from the TraitConfigGrain
        var traitDefinitions = await traitConfigGrain.GetTraitsMap(traitNames);

        return traitDefinitions;
    }

    public async Task<String> generatePrompt(List<Trait> requestTraits, Dictionary<string, TraitEntry> traitDefinitions)
    {
        var sentences = await _promptBuilder.GenerateSentences(requestTraits, traitDefinitions);
        var prompt = await _promptBuilder.GenerateFinalPromptFromSentences(ImageGenerationConstants.DALLE_BASE_PROMPT, sentences);
        return prompt;
    }

}