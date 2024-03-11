using Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Shared;

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

    [HttpPost("generate")]
    public async Task<ImageGenerationResponse> GenerateImage(ImageGenerationRequest imageGenerationRequest)
    {
        //generate a new UUID with a prefix of "imageRequest"        
        string imageRequestId = "ImageRequest_" + Guid.NewGuid().ToString();

        var multiImageGeneratorGrain = _client.GetGrain<IMultiImageGeneratorGrain>(imageRequestId);

        var response = await multiImageGeneratorGrain.GenerateMultipleImagesAsync(imageGenerationRequest, imageRequestId);

        return response;
    }

    [HttpPost("query")]
    public async Task<ImageQueryResponse> QueryImage(ImageQueryRequest imageQueryRequest)
    {
        var multiImageGeneratorGrain = _client.GetGrain<IMultiImageGeneratorGrain>(imageQueryRequest.RequestId);

        var imageQueryResponse = await multiImageGeneratorGrain.QueryMultipleImagesAsync();

        return imageQueryResponse;
    }
}
