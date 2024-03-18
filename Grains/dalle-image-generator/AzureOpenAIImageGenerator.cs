using System.Net;
using Azure;
using Microsoft.Extensions.Logging;
using Azure.AI.OpenAI;
using Shared;
using Newtonsoft.Json;

namespace Grains;

public class AzureOpenAIImageGenerator : IImageGenerator
{
    private readonly ILogger<ImageGeneratorGrain> _logger;

    public AzureOpenAIImageGenerator(
        ILogger<ImageGeneratorGrain> logger)
    {
        _logger = logger;
    }

    public async Task<ImageGenerationResponse> RunImageGenerationAsync(string prompt, string apikey, int numberOfImages, string requestId)
    {
        AzureImageGenerationResponse azureImageGenerationResponse = null;
        Response<ImageGenerations> imageGenerations;
        Response rawResponse = null;
        int httpStatusCode = 0;

        try
        {
            OpenAIClient client = new(new Uri("https://schrodinger-east-us.openai.azure.com/"), new AzureKeyCredential(apikey));

            imageGenerations = await client.GetImageGenerationsAsync(
                new ImageGenerationOptions()
                {
                    Prompt = prompt,
                    Size = ImageSize.Size256x256,
                    ImageCount = numberOfImages
                });
            
            rawResponse = imageGenerations.GetRawResponse();
            httpStatusCode = (int)rawResponse.Status;
            
            if (httpStatusCode >= 200 && httpStatusCode < 300)
            {
                // The request was successful
                azureImageGenerationResponse = JsonConvert.DeserializeObject<AzureImageGenerationResponse>(imageGenerations.Value.ToString());
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"AzureImageGenerator - generatorId: {requestId} , Azure-Dalle API call failed with error: {e.Message}");
            throw new ImageGenerationException(ImageGenerationErrorCode.internal_error, e.Message);
        }

        ImageGenerationResponse imageGenerationResponse = null;
        _logger.LogError($"AzureImageGenerator - ImageGeneration ResponseCode : {httpStatusCode}");

        if (httpStatusCode != (int)HttpStatusCode.OK)
        {
            try
            {
                imageGenerationResponse = new ImageGenerationResponse
                {
                
                    // get latest timestamp in seconds
                    Created =  (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                    Data = azureImageGenerationResponse.Data.Select(data => new ImageGenerationOpenAIData
                    {
                        Url = data.Url,
                        RevisedPrompt = data.RevisedPrompt
                    }).ToList()
                };
            }
            catch (Exception e)
            {
                _logger.LogError($"AzureImageGenerator - generatorId: {requestId} , Dalle API call failed with error: {e.Message}");
                throw new ImageGenerationException(ImageGenerationErrorCode.api_call_failed, e.Message);
            }
        } else {
            ImageGenerationError dalleError;
            try
            {
                dalleError = HandleImageGenerationError((HttpStatusCode)httpStatusCode, JsonConvert.SerializeObject(imageGenerationResponse));
            }
            catch (Exception e)
            {
                _logger.LogError($"AzureImageGenerator - generatorId: {requestId} , Dalle API call failed with error: {e.Message}");
                throw new ImageGenerationException(ImageGenerationErrorCode.internal_error, e.Message);
            }
            
            _logger.LogError($"AzureImageGenerator - generatorId: {requestId} , Dalle API call failed with error: {dalleError.Message}");
            
            throw new ImageGenerationException(dalleError.ImageGenerationErrorCode, dalleError.Message);
        }

        _logger.LogInformation($"AzureImageGenerator - generatorId: {requestId} , Dalle API deserialized response: {imageGenerationResponse}");

        return imageGenerationResponse;
    }

    public ImageGenerationError HandleImageGenerationError(HttpStatusCode httpStatusCode, string responseJson)
    {
        throw new Exception("Azure HandleImageGenerationError Not Implemented");
    }
}