using Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Shared;
using Attribute = Shared.Attribute;

namespace WebApi.Controllers;

[ApiController]
[Route("image")]
public class MultiImageGeneratorController : ControllerBase
{
    private readonly IClusterClient _client;

    private readonly ILogger<MultiImageGeneratorController> _logger;


    public MultiImageGeneratorController(IClusterClient client, ILogger<MultiImageGeneratorController> logger)
    {
        _client = client;
        _logger = logger;
    }


    [HttpPost("inspect")]
    public async Task<ImageGenerationState> Inspect(InspectGeneratorRequest request)
    {
        var grain = _client.GetGrain<IImageGeneratorGrain>(request.RequestId);
        var state = await grain.GetStateAsync();
        return state;
    }

    [HttpPost("generate")]
    public async Task<ImageGenerationResponse> GenerateImage(ImageGenerationRequest imageGenerationRequest)
    {
        List<Attribute> newTraits = imageGenerationRequest.NewTraits;
        List<Attribute> baseTraits = imageGenerationRequest.BaseImage.Attributes;

        //collect the newTraits from the request and combine it with trats from the base image
        IEnumerable<Attribute> traits = newTraits.Concat(baseTraits);

        //generate a new UUID with a prefix of "imageRequest"        
        string imageRequestId = "MultiImageRequest_" + Guid.NewGuid().ToString();

        var multiImageGeneratorGrain = _client.GetGrain<IMultiImageGeneratorGrain>(imageRequestId);

        var response = await multiImageGeneratorGrain.GenerateMultipleImagesAsync(traits.ToList(),
            imageGenerationRequest.NumberOfImages, imageRequestId);

        if (response.IsSuccessful)
        {
            return new ImageGenerationResponseOk { RequestId = imageRequestId };
        }
        else
        {
            List<string> errorMessages = response.Errors ?? new List<string>();
            string errorMessage = string.Join(", ", errorMessages);
            return new ImageGenerationResponseNotOk { Error = errorMessage };
        }
    }

    [HttpPost("query")]
    public async Task<ObjectResult> QueryImage(ImageQueryRequest imageQueryRequest)
    {
        _logger.LogInformation("MultiImageGeneratorController - Querying image with request id: " + imageQueryRequest.RequestId);

        var multiImageGeneratorGrain = _client.GetGrain<IMultiImageGeneratorGrain>(imageQueryRequest.RequestId);
        var imageQueryResponse = await multiImageGeneratorGrain.QueryMultipleImagesAsync();

        _logger.LogInformation("$MultiImageGeneratorController - Querying image with request id: " + imageQueryRequest.RequestId + " - Response: " + imageQueryResponse.Status);
        
        if (imageQueryResponse.Uninitialized)
            return StatusCode(404, new ImageQueryResponseNotOk { Error = "Request not found" });

        if (imageQueryResponse.Status != ImageGenerationStatus.SuccessfulCompletion)
            return StatusCode(202, new ImageQueryResponseNotOk { Error = "The result is not ready." });
        var images = imageQueryResponse.Images ?? [];
        return StatusCode(200, new ImageQueryResponseOk { Images = images });
    }
    
    [HttpPost("generations/{requestId}")]
    public async Task<ImageGenerationResponse> ImageGenerations(ImageGenerationRequest imageGenerationRequest, string requestId)
    {
        List<Attribute> newTraits = imageGenerationRequest.NewTraits;
        List<Attribute> baseTraits = imageGenerationRequest.BaseImage.Attributes;

        //collect the newTraits from the request and combine it with trats from the base image
        IEnumerable<Attribute> traits = newTraits.Concat(baseTraits);

        var multiImageGeneratorGrain = _client.GetGrain<IMultiImageGeneratorGrain>(requestId);
        var isAlreadySubmitted = await multiImageGeneratorGrain.IsAlreadySubmitted();
        if (isAlreadySubmitted)
        {
            return new ImageGenerationResponseNotOk { Error = "Duplicate request" };
        }

        var response = await multiImageGeneratorGrain.GenerateMultipleImagesAsync(traits.ToList(),
            imageGenerationRequest.NumberOfImages, requestId);

        if (response.IsSuccessful)
        {
            return new ImageGenerationResponseOk { RequestId = requestId };
        }
        else
        {
            List<string> errorMessages = response.Errors ?? new List<string>();
            string errorMessage = string.Join(", ", errorMessages);
            return new ImageGenerationResponseNotOk { Error = errorMessage };
        }
    }

    [HttpGet("generations/{requestId}")]
    public async Task<ObjectResult> ImageGenerationsQuery(string requestId)
    {
        _logger.LogInformation("MultiImageGeneratorController - Querying image with request id: " + requestId);

        var multiImageGeneratorGrain = _client.GetGrain<IMultiImageGeneratorGrain>(requestId);
        var imageQueryResponse = await multiImageGeneratorGrain.QueryMultipleImagesAsync();

        _logger.LogInformation("$MultiImageGeneratorController - Querying image with request id: " + requestId + " - Response: " + imageQueryResponse.Status);
        
        if (imageQueryResponse.Uninitialized)
            return StatusCode(404, new ImageQueryResponseNotOk { Error = "Request not found" });

        if (imageQueryResponse.Status != ImageGenerationStatus.SuccessfulCompletion)
            return StatusCode(202, new ImageQueryResponseNotOk { Error = "The result is not ready." });
        var images = imageQueryResponse.Images ?? [];
        return StatusCode(200, new ImageQueryResponseOk { Images = images });
    }
    
}