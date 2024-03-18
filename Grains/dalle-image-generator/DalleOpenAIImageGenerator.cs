using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Shared;

namespace Grains;

public class DalleOpenAIImageGenerator : IImageGenerator
{
    private readonly ILogger<ImageGeneratorGrain> _logger;
    
    public DalleOpenAIImageGenerator(
        ILogger<ImageGeneratorGrain> logger)
    {
        _logger = logger;
    }

    public async Task<ImageGenerationResponse> RunImageGenerationAsync(string prompt, string apikey, int numberOfImages, string requestId)
    {
        _logger.LogInformation($"ImageGeneratorGrain - generatorId: {requestId} , about to call Dalle API to generate image for prompt: {prompt}");
        var response = new HttpResponseMessage();
        var jsonResponse = "";
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apikey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                model = "dall-e-3",
                prompt = prompt,
                quality = "standard",
                n = numberOfImages
            }), Encoding.UTF8, "application/json");

            response = await client.PostAsync("https://api.openai.com/v1/images/generations", content);

            _logger.LogInformation(
                $"ImageGeneratorGrain - generatorId: {requestId} , Dalle API response: {response} - responseCode: {response.StatusCode}");

            jsonResponse = await response.Content.ReadAsStringAsync();
        } catch (Exception e)
        {
            _logger.LogError($"ImageGeneratorGrain - generatorId: {requestId} , Dalle API call failed with error: {e.Message}");
            throw new ImageGenerationException(ImageGenerationErrorCode.internal_error, e.Message);
        }

        _logger.LogInformation($"ImageGeneratorGrain - generatorId: {requestId} , Dalle API call response Content string: {jsonResponse}");

        ImageGenerationError dalleError;
        
        if (response.StatusCode != HttpStatusCode.OK)
        {
            try
            {
                dalleError = HandleImageGenerationError(response.StatusCode, jsonResponse);
            } catch (Exception e)
            {
                _logger.LogError($"ImageGeneratorGrain - generatorId: {requestId} , Dalle API call failed with error: {e.Message}");
                throw new ImageGenerationException(ImageGenerationErrorCode.internal_error, e.Message);
            }
            
            _logger.LogError($"ImageGeneratorGrain - generatorId: {requestId} , Dalle API call failed with error: {dalleError.Message}");
            
            throw new ImageGenerationException(dalleError.ImageGenerationErrorCode, dalleError.Message);
        }
        
        ImageGenerationResponse imageGenerationResponse;
        try
        {
            imageGenerationResponse = JsonConvert.DeserializeObject<ImageGenerationResponse>(jsonResponse);
        }
        catch (Exception e)
        {
            _logger.LogError($"ImageGeneratorGrain - generatorId: {requestId} , Dalle API call failed with error: {e.Message}");
            throw new ImageGenerationException(ImageGenerationErrorCode.api_call_failed, e.Message);
        }
        
        _logger.LogError($"Dalle ImageGeneration ResponseCode : {response.StatusCode}");
        
        if(imageGenerationResponse.Error != null)
        {
            _logger.LogError($"ImageGeneratorGrain - generatorId: {requestId} , Dalle API call failed with error code: {imageGenerationResponse.Error.Code}");
            throw new ImageGenerationException(ImageGenerationErrorCode.internal_error, imageGenerationResponse.Error.Message);
        }

        _logger.LogInformation($"ImageGeneratorGrain - generatorId: {requestId} , Dalle API deserialized response: {imageGenerationResponse}");

        return imageGenerationResponse;
    }

    public ImageGenerationError HandleImageGenerationError(HttpStatusCode httpStatusCode, string responseJson)
    {
        ImageGenerationErrorCode dalleErrorCodes;
        
        var imageGenerationWrappedErrorObject = JsonConvert.DeserializeObject<ImageGenerationWrappedError>(responseJson);
        var dalleErrorObject = imageGenerationWrappedErrorObject?.Error;
        
        switch (httpStatusCode)
        {
            case HttpStatusCode.Unauthorized:
                dalleErrorCodes = ImageGenerationErrorCode.invalid_api_key;
                break;
            case HttpStatusCode.TooManyRequests:
                { 
                    dalleErrorCodes = ImageGenerationErrorCode.rate_limit_reached;
                    break;
                }
            case HttpStatusCode.ServiceUnavailable:
                dalleErrorCodes = ImageGenerationErrorCode.engine_unavailable;
                break;
            case HttpStatusCode.BadRequest:
            {
                if (dalleErrorObject?.Code == "billing_hard_limit_reached")
                {
                    dalleErrorCodes = ImageGenerationErrorCode.billing_quota_exceeded;
                }
                else
                {
                    dalleErrorCodes = ImageGenerationErrorCode.bad_request;
                }
                break;
            }
            case HttpStatusCode.InternalServerError:
                dalleErrorCodes = ImageGenerationErrorCode.internal_error;
                break;
            default:
                dalleErrorCodes = ImageGenerationErrorCode.internal_error;
                break;
        }

        ImageGenerationError dalleError = new ImageGenerationError
        {
            HttpStatusCode = httpStatusCode,
            ImageGenerationErrorCode = dalleErrorCodes,
            Message = dalleErrorObject!.Message
        };

        return dalleError;
    }

}