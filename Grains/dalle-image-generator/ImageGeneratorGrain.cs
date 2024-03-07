using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Orleans;
using Shared;

namespace Grains;

public class ImageGeneratorGrain : Grain<ImageGenerationState>
{
    private readonly PromptBuilder _promptBuilder;

    private Dictionary<string, Task<string>> imageMap = new Dictionary<string, Task<string>>();
    private Dictionary<string, ImageGenerationRequest> imageGenerationRequestMap = new Dictionary<string, ImageGenerationRequest>();


    public ImageGeneratorGrain(PromptBuilder promptBuilder)
    {
        _promptBuilder = promptBuilder;
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

    public async Task<ImageGenerationResponse> GenerateImage(ImageGenerationRequest request, string imageRequestId)
    {
        try
        {
            string finalPrompt = await generatePrompt(request);

            // Call the DALL-E API to generate an image
            // Generate the image data
            // Start the image data generation process
            Task<DalleResponse> imageDataTask = RunDalleAsync(finalPrompt);

            // Specify an action that will be executed when the image data generation process is complete
            #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            imageDataTask.ContinueWith(async completedTask =>
            {
                // Check if the task is faulted (an exception was thrown)
                if (completedTask.IsFaulted)
                {
                    // Handle the error
                    Console.Error.WriteLine(completedTask.Exception);
                }
                else
                {
                    DalleResponse result = completedTask.Result;

                    // Check if the Data list is not empty
                    if (result.Data != null && result.Data.Count > 0)
                    {
                        // Extract the URL from the result
                        string imageUrl = result.Data[0].Url;

                        // Update the image map with the URL of the image
                        this.State.ImageMap[imageRequestId] = Task.FromResult(imageUrl);

                        // Write the state to the storage provider
                        await this.WriteStateAsync();
                    }
                }
            });
            #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            // Store the traits in the image generation request map
            this.State.ImageGenerationRequestMap[imageRequestId] = request;

            // Write the state to the storage provider
            await this.WriteStateAsync();

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

    public Task<ImageQueryResponseOk> QueryImages(ImageQueryRequest request)
    {
        throw new NotImplementedException();
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