using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Grains.ImageGenerator;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Shared;

namespace Grains.DalleOpenAI;

public class DalleOpenAIImageGenerator : IImageGenerator
{
    private readonly ILogger<ImageGeneratorGrain> _logger;
    
    public DalleOpenAIImageGenerator(
        ILogger<ImageGeneratorGrain> logger)
    {
        _logger = logger;
    }

    public async Task<ImageGenerationResponse> RunImageGenerationAsync(string prompt, string apikey, int numberOfImages, ImageSettings imageSettings, string requestId)
    {
        _logger.LogInformation($"DalleOpenAIImageGenerator - generatorId: {requestId} , about to call Dalle API to generate image for prompt: {prompt}");
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
                $"ImageGeneratorGrain - generatorId: {requestId} , DalleOpenAI API response: {response} - responseCode: {response.StatusCode}");

            jsonResponse = await response.Content.ReadAsStringAsync();
        } catch (Exception e)
        {
            _logger.LogError($"DalleOpenAIImageGenerator - generatorId: {requestId} , DalleOpenAI API call failed with error: {e.Message}");
            throw new ImageGenerationException(ImageGenerationErrorCode.internal_error, e.Message);
        }

        _logger.LogInformation($"DalleOpenAIImageGenerator - generatorId: {requestId} , DalleOpenAI API call response Content string: {jsonResponse}");

        ImageGenerationError dalleError;
        
        if (response.StatusCode != HttpStatusCode.OK)
        {
            try
            {
                dalleError = HandleImageGenerationError(response.StatusCode, jsonResponse);
            } catch (Exception e)
            {
                _logger.LogError($"DalleOpenAIImageGenerator - generatorId: {requestId} , DalleOpenAI API call failed with error: {e.Message}");
                throw new ImageGenerationException(ImageGenerationErrorCode.internal_error, e.Message);
            }
            
            _logger.LogError($"DalleOpenAIImageGenerator - generatorId: {requestId} , DalleOpenAI API call failed with error: {dalleError.Message}");
            
            throw new ImageGenerationException(dalleError.ImageGenerationErrorCode, dalleError.Message);
        }
        
        ImageGenerationResponse imageGenerationResponse;
        try
        {
            imageGenerationResponse = JsonConvert.DeserializeObject<ImageGenerationResponse>(jsonResponse);
        }
        catch (Exception e)
        {
            _logger.LogError($"DalleOpenAIImageGenerator - generatorId: {requestId} , DalleOpenAI API call failed with error: {e.Message}");
            throw new ImageGenerationException(ImageGenerationErrorCode.api_call_failed, e.Message);
        }
        
        _logger.LogError($"DalleOpenAIImageGenerator ImageGeneration ResponseCode : {response.StatusCode}");
        
        if(imageGenerationResponse.Error != null)
        {
            _logger.LogError($"DalleOpenAIImageGenerator - generatorId: {requestId} , DalleOpenAI API call failed with error code: {imageGenerationResponse.Error.Code}");
            throw new ImageGenerationException(ImageGenerationErrorCode.internal_error, imageGenerationResponse.Error.Message);
        }

        _logger.LogInformation($"DalleOpenAIImageGenerator - generatorId: {requestId} , DalleOpenAI API deserialized response: {imageGenerationResponse}");

        return imageGenerationResponse;
    }

    public ImageGenerationError HandleImageGenerationError(HttpStatusCode httpStatusCode, string responseJson)
    {
        ImageGenerationErrorCode imageGenerationErrorCode;

        var dalleOpenAiImageGenerationWrappedErrorObject = JsonConvert.DeserializeObject<DalleOpenAIImageGenerationWrappedError>(responseJson);
        var dalleOpenAiImageGenerationError = dalleOpenAiImageGenerationWrappedErrorObject?.Error;
        
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
                if (dalleOpenAiImageGenerationError?.Code == "billing_hard_limit_reached")
                {
                    imageGenerationErrorCode = ImageGenerationErrorCode.billing_quota_exceeded;
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

        ImageGenerationError imageGenerationError = new ImageGenerationError
        {
            HttpStatusCode = httpStatusCode,
            ImageGenerationErrorCode = imageGenerationErrorCode,
            Message = dalleOpenAiImageGenerationError!.Message
        };

        return imageGenerationError;
    }

}