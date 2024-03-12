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

    public MultiImageGeneratorController(IClusterClient client)
    {
        _client = client;
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
    public async Task<ImageQueryResponse> QueryImage(ImageQueryRequest imageQueryRequest)
    {
        var multiImageGeneratorGrain = _client.GetGrain<IMultiImageGeneratorGrain>(imageQueryRequest.RequestId);

        var imageQueryResponse = await multiImageGeneratorGrain.QueryMultipleImagesAsync();

        if (imageQueryResponse.Status == ImageGenerationStatus.SuccessfulCompletion ||
            imageQueryResponse.Status == ImageGenerationStatus.InProgress)
        {
            List<ImageDescription> images = imageQueryResponse.Images ?? [];
            return new ImageQueryResponseOk { Images = images };
        }
        else
        {
            List<string> errorMessages = imageQueryResponse.Errors ?? [];
            string errorMessage = string.Join(", ", errorMessages);
            return new ImageQueryResponseNotOk { Error = errorMessage };
        }
    }
}