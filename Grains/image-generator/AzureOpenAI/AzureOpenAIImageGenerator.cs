using System.Net;
using Azure;
using Microsoft.Extensions.Logging;
using Azure.AI.OpenAI;
using Grains.Errors;
using Schrodinger.Backend.Abstractions.ApiKeys;
using Newtonsoft.Json;
using Schrodinger.Backend.Abstractions.Constants;
using Schrodinger.Backend.Abstractions.Images;

namespace Grains.image_generator.AzureOpenAI;

public class AzureOpenAIImageGenerator : IAzureOpenAIImageGenerator
{
    private readonly ILogger<ImageGeneratorGrain> _logger;

    public AzureOpenAIImageGenerator(
        ILogger<ImageGeneratorGrain> logger)
    {
        _logger = logger;
    }

    public async Task<AIImageGenerationResponse> RunImageGenerationAsync(string prompt, ApiKey apikey, int numberOfImages, ImageSettings imageSettings, string requestId)
    {
        Response<ImageGenerations> imageGenerationsResponse;
        Response rawResponse;
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
        
        ImageGenerations imageGenerations = null;

        try
        {
            OpenAIClient client = new(new Uri(apikey.Url),
                new AzureKeyCredential(apikey.ApiKeyString));
            
            imageGenerationsResponse = await client.GetImageGenerationsAsync(
                new ImageGenerationOptions()
                {
                    Prompt = prompt,
                    Size = imageSize,
                    ImageCount = numberOfImages
                });

            rawResponse = imageGenerationsResponse.GetRawResponse();
            httpStatusCode = (int)rawResponse.Status;

            if (httpStatusCode >= 200 && httpStatusCode < 300)
            {
                imageGenerations = imageGenerationsResponse.Value;
            }
        }
        catch (RequestFailedException reqExc)
        {
            AIImageGenerationError azureError;
            try
            {
                azureError = HandleAzureRequestFailedException(reqExc);
            }
            catch (Exception e)
            {
                _logger.LogError("AzureImageGenerator - generatorId: {requestId} , Azure API call failed with error: {errorMessage}", requestId, e.Message);
                throw new ImageGenerationException(ImageGenerationErrorCode.internal_error, e.Message);
            }
            
            _logger.LogError("AzureImageGenerator - generatorId: {requestId} , Azure API call failed with error: {azureErrorMessage}", requestId, azureError.Message);
            
            throw new ImageGenerationException(azureError.ImageGenerationErrorCode, azureError.Message);
        } catch (Exception e)
        {
            _logger.LogError("AzureImageGenerator - generatorId: {requestId} , Azure API call failed with error: {errorMessage}", requestId, e.Message);
            throw new ImageGenerationException(ImageGenerationErrorCode.internal_error, e.Message);
        }

        AIImageGenerationResponse aiImageGenerationResponse;
        _logger.LogError("AzureImageGenerator - ImageGeneration ResponseCode : {httpStatusCode}", httpStatusCode);

        if (httpStatusCode == (int)HttpStatusCode.OK)
        {
            try
            {
                aiImageGenerationResponse = new AIImageGenerationResponse
                {
                    // get latest timestamp in seconds
                    Created =  (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                    Data = imageGenerations.Data.Select(data => new ImageGenerationData
                    {
                        Url = data.Url.AbsoluteUri,
                        RevisedPrompt = prompt
                    }).ToList()
                };
            }
            catch (Exception e)
            {
                _logger.LogError("AzureImageGenerator - generatorId: {requestId} , Azure API call failed with error: {errorMessage}", requestId, e.Message);
                throw new ImageGenerationException(ImageGenerationErrorCode.api_call_failed, e.Message);
            }
        } else {
            AIImageGenerationError azureError;
            try
            {
                azureError = HandleImageGenerationError(rawResponse);
            }
            catch (Exception e)
            {
                _logger.LogError("AzureImageGenerator - generatorId: {requestId} , Azure API call failed with error: {errorMessage}", requestId, e.Message);
                throw new ImageGenerationException(ImageGenerationErrorCode.internal_error, e.Message);
            }
            
            _logger.LogError("AzureImageGenerator - generatorId: {requestId} , Azure API call failed with error: {azureErrorMessage}", requestId, azureError.Message);
            
            throw new ImageGenerationException(azureError.ImageGenerationErrorCode, azureError.Message);
        }

        _logger.LogInformation("AzureImageGenerator - generatorId: {requestId} , Azure API deserialized response: {imageGenerationResponse}", requestId, aiImageGenerationResponse);

        return aiImageGenerationResponse;
    }

    public AIImageGenerationError HandleAzureRequestFailedException(Azure.RequestFailedException requestFailedException)
    {
        ImageGenerationErrorCode imageGenerationErrorCode;

        if (requestFailedException.ErrorCode is "content_filter" or "contentFilter")
        {
            imageGenerationErrorCode = ImageGenerationErrorCode.content_violation;
        }
        else
        {
            imageGenerationErrorCode = ImageGenerationErrorCode.bad_request;
        }

        AIImageGenerationError azureError = new AIImageGenerationError
        {
            HttpStatusCode = requestFailedException.Status,
            ImageGenerationErrorCode = imageGenerationErrorCode,
            Message = requestFailedException!.Message
        };

        return azureError;

    }

    public AIImageGenerationError HandleImageGenerationError(Response rawResponse)
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
                else if (azureError?.Code == "content_filter" || azureError?.Code == "contentFilter")
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

        AIImageGenerationError azureAiImageGenerationError = new AIImageGenerationError
        {
            HttpStatusCode = rawResponse.Status,
            ImageGenerationErrorCode = imageGenerationErrorCode,
            Message = azureError!.Message
        };

        return azureAiImageGenerationError;
    }
}