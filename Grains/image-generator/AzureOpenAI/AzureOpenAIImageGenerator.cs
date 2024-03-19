using System.Net;
using Azure;
using Microsoft.Extensions.Logging;
using Azure.AI.OpenAI;
using Shared;
using Grains.ImageGenerator;
using Newtonsoft.Json;

namespace Grains.AzureOpenAI;

public class AzureOpenAIImageGenerator : IImageGenerator
{
    private readonly ILogger<ImageGeneratorGrain> _logger;

    public AzureOpenAIImageGenerator(
        ILogger<ImageGeneratorGrain> logger)
    {
        _logger = logger;
    }

    public async Task<ImageGenerationResponse> RunImageGenerationAsync(string prompt, string apikey, int numberOfImages, ImageSettings imageSettings, string requestId)
    {
        AzureImageGenerationResponse azureImageGenerationResponse = null;
        Response<ImageGenerations> imageGenerations;
        Response rawResponse = null;
        int httpStatusCode = 0;
        
        //Image Settings contain width and height of the image
        // lookup for those values and generate the ImageSize constant for the API call
        ImageSize imageSize = ImageSize.Size512x512;
        
        if (imageSettings != null)
        {
            if (imageSettings.Width == 256 && imageSettings.Height == 256)
            {
                imageSize = ImageSize.Size256x256;
            }
            else if (imageSettings.Width == 512 && imageSettings.Height == 512)
            {
                imageSize = ImageSize.Size512x512;
            }
            else if (imageSettings.Width == 1024 && imageSettings.Height == 1024)
            {
                imageSize = ImageSize.Size1024x1024;
            }
        }

        try
        {
            OpenAIClient client = new(new Uri("https://schrodinger-east-us.openai.azure.com/"), new AzureKeyCredential(apikey));

            imageGenerations = await client.GetImageGenerationsAsync(
                new ImageGenerationOptions()
                {
                    Prompt = prompt,
                    Size = imageSize,
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

        if (httpStatusCode == (int)HttpStatusCode.OK)
        {
            try
            {
                imageGenerationResponse = new ImageGenerationResponse
                {
                    // get latest timestamp in seconds
                    Created =  (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                    Data = azureImageGenerationResponse.Data.Select(data => new ImageGenerationData
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
                dalleError = HandleImageGenerationError(rawResponse);
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

    public ImageGenerationError HandleImageGenerationError(Response rawResponse)
    {
        //get error message from rawResponse
        string responseJson = rawResponse.Content.ToString();
        
        //get http status code
        HttpStatusCode httpStatusCode = (HttpStatusCode)rawResponse.Status;
        
        ImageGenerationErrorCode imageGenerationErrorCode;
        
        var azureErrorWrapper = JsonConvert.DeserializeObject<AzureErrorWrapper>(responseJson);
        
        // get code from AzureErrorWrapper
        var azureError = azureErrorWrapper?.Error;
        
        switch (httpStatusCode)
        {
            case HttpStatusCode.Unauthorized:
                imageGenerationErrorCode = ImageGenerationErrorCode.invalid_api_key;
                break;
            case HttpStatusCode.TooManyRequests:
            { 
                imageGenerationErrorCode = ImageGenerationErrorCode.rate_limit_reached;
                break;
            }
            case HttpStatusCode.ServiceUnavailable:
                imageGenerationErrorCode = ImageGenerationErrorCode.engine_unavailable;
                break;
            case HttpStatusCode.BadRequest:
            {
                if (azureError?.Code == "billing_hard_limit_reached")
                {
                    imageGenerationErrorCode = ImageGenerationErrorCode.billing_quota_exceeded;
                }
                else if (azureError?.Code == "content_filter")
                {
                    imageGenerationErrorCode = ImageGenerationErrorCode.content_violation;
                }
                else
                {
                    imageGenerationErrorCode = ImageGenerationErrorCode.bad_request;
                }
                break;
            }
            case HttpStatusCode.InternalServerError:
                imageGenerationErrorCode = ImageGenerationErrorCode.internal_error;
                break;
            default:
                imageGenerationErrorCode = ImageGenerationErrorCode.internal_error;
                break;
        }

        ImageGenerationError dalleError = new ImageGenerationError
        {
            HttpStatusCode = httpStatusCode,
            ImageGenerationErrorCode = imageGenerationErrorCode,
            Message = azureError!.Message
        };

        return dalleError;
    }
}