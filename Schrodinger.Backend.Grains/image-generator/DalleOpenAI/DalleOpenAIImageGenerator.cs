using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Schrodinger.Backend.Abstractions.ApiKeys;
using Schrodinger.Backend.Grains.Errors;
using Schrodinger.Backend.Abstractions.Constants;
using Schrodinger.Backend.Abstractions.Images;

namespace Schrodinger.Backend.Grains.image_generator.DalleOpenAI;

public class DalleOpenAIImageGenerator : IDalleOpenAIImageGenerator
{
    private readonly ILogger<ImageGeneratorGrain> _logger;
    
    public DalleOpenAIImageGenerator(
        ILogger<ImageGeneratorGrain> logger)
    {
        _logger = logger;
    }

    public async Task<AIImageGenerationResponse> RunImageGenerationAsync(string prompt, ApiKey apikey, int numberOfImages, ImageSettings imageSettings, string requestId)
    {
        _logger.LogInformation($"DalleOpenAIImageGenerator - generatorId: {requestId} , about to call Dalle API to generate image for prompt: {prompt}");
        var response = new HttpResponseMessage();
        var jsonResponse = "";
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apikey.ApiKeyString);
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
                "ImageGeneratorGrain - generatorId: {requestId} , DalleOpenAI API - responseStatusCode: {responseStatusCode} - responseReasonPhrase: {responseReasonPhrase} - response: {response}", requestId, response.StatusCode, response.ReasonPhrase, response);
            
            jsonResponse = await response.Content.ReadAsStringAsync();
        } catch (Exception e)
        {
            _logger.LogError("DalleOpenAIImageGenerator - generatorId: {requestId} , DalleOpenAI API call failed with error: {errorMessage}", requestId, e.Message);
            throw new ImageGenerationException(ImageGenerationErrorCode.internal_error, e.Message);
        }

        _logger.LogInformation($"DalleOpenAIImageGenerator - generatorId: {requestId} , DalleOpenAI API call response Content string: {jsonResponse}");

        AIImageGenerationError dalleError;
        
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
        
        AIImageGenerationResponse aiImageGenerationResponse;
        try
        {
            aiImageGenerationResponse = JsonConvert.DeserializeObject<AIImageGenerationResponse>(jsonResponse);
        }
        catch (Exception e)
        {
            _logger.LogError($"DalleOpenAIImageGenerator - generatorId: {requestId} , DalleOpenAI API call failed with error: {e.Message}");
            throw new ImageGenerationException(ImageGenerationErrorCode.api_call_failed, e.Message);
        }
        
        _logger.LogInformation($"DalleOpenAIImageGenerator ImageGeneration ResponseCode : {response.StatusCode}");
        
        if(aiImageGenerationResponse.Error != null)
        {
            _logger.LogError($"DalleOpenAIImageGenerator - generatorId: {requestId} , DalleOpenAI API call failed with error code: {aiImageGenerationResponse.Error.Code}");
            throw new ImageGenerationException(ImageGenerationErrorCode.internal_error, aiImageGenerationResponse.Error.Message);
        }

        _logger.LogInformation($"DalleOpenAIImageGenerator - generatorId: {requestId} , DalleOpenAI API deserialized response: {aiImageGenerationResponse}");

        return aiImageGenerationResponse;
    }

    public AIImageGenerationError HandleImageGenerationError(HttpStatusCode httpStatusCode, string responseJson)
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
                else if (dalleOpenAiImageGenerationError?.Code is "content_policy" or "content_policy_violation")
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

        AIImageGenerationError aiImageGenerationError = new AIImageGenerationError
        {
            HttpStatusCode = (int)httpStatusCode,
            ImageGenerationErrorCode = imageGenerationErrorCode,
            Message = dalleOpenAiImageGenerationError!.Message
        };

        return aiImageGenerationError;
    }

}