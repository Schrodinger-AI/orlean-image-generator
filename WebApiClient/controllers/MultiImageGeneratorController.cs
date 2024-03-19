using Grains;
using Grains.interfaces;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using WebApi.ImageGeneration.Models;
using ModelAttribute = WebApi.Models.Attribute;
using SharedAttribute = Shared.Attribute;
using SharedImageDescription = Shared.ImageDescription;
using ModelImageDescription = WebApi.Models.ImageDescription;

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

    
    public static SharedAttribute ConvertToSharedAttribute(ModelAttribute modelAttribute)
    {
        return new SharedAttribute
        {
            TraitType = modelAttribute.TraitType,
            Value = modelAttribute.Value
        };
    }

    [HttpPost("inspect")]
    public async Task<ImageGenerationState> Inspect(InspectGeneratorRequest request)
    {
        var grain = _client.GetGrain<IImageGeneratorGrain>(request.RequestId);
        var state = await grain.GetStateAsync();
        return state;
    }

    [HttpPost("generate")]
    public async Task<ImageGenerationAPIResponse> GenerateImage(ImageGenerationAPIRequest imageGenerationAPIRequest)
    {
        List<ModelAttribute> newTraits = imageGenerationAPIRequest.NewTraits;
        List<ModelAttribute> baseTraits = imageGenerationAPIRequest.BaseImage.Attributes;

        //collect the newTraits from the request and combine it with trats from the base image
        IEnumerable<ModelAttribute> traits = newTraits.Concat(baseTraits);

        //generate a new UUID with a prefix of "imageRequest"        
        string imageRequestId = "MultiImageRequest_" + Guid.NewGuid().ToString();

        var multiImageGeneratorGrain = _client.GetGrain<IMultiImageGeneratorGrain>(imageRequestId);

        List<SharedAttribute> sharedTraits = traits.Select(ConvertToSharedAttribute).ToList();

        var response = await multiImageGeneratorGrain.GenerateMultipleImagesAsync(sharedTraits,
            imageGenerationAPIRequest.NumberOfImages, imageRequestId);
        
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

    
    public static ModelImageDescription ConvertToModelImageDescription(SharedImageDescription sharedImageDescription)
    {
        return new ModelImageDescription
        {
            Image = sharedImageDescription.Image,
            Attributes = sharedImageDescription.Attributes.Select(ConvertToModelAttribute).ToList(),
            ExtraData = sharedImageDescription.ExtraData
        };
    }
    
    
    public static ModelAttribute ConvertToModelAttribute(SharedAttribute sharedAttribute)
    {
        return new ModelAttribute
        {
            TraitType = sharedAttribute.TraitType,
            Value = sharedAttribute.Value
        };
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

        if (imageQueryResponse.Status != Shared.ImageGenerationStatus.SuccessfulCompletion)
            return StatusCode(202, new ImageQueryResponseNotOk { Error = "The result is not ready." });
        var images = imageQueryResponse.Images ?? [];
        List<Shared.ImageDescription> sharedImages = imageQueryResponse.Images ?? new List<Shared.ImageDescription>();
        List<ModelImageDescription> modelImages = sharedImages.Select(ConvertToModelImageDescription).ToList();
        return StatusCode(200, new ImageQueryResponseOk { Images = modelImages });
    }
}