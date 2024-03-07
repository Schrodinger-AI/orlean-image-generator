using Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Shared;

namespace WebApi.Controllers;

[ApiController]
[Route("image")]
public class ImageGeneratorController : ControllerBase
{
    private readonly IClusterClient _client;

    public ImageGeneratorController(IClusterClient client)
    {
        _client = client;
    }

    [HttpPost("generate")]
    public async Task<ImageGenerationResponse> generateImage(ImageGenerationRequest imageGenerationRequest)
    {
        //generate a new UUID with a prefix of "imageRequest"        
        string imageRequestId = "ImageRequest_" + Guid.NewGuid().ToString();

        var imageGeneratorGrain = _client.GetGrain<IImageGeneratorGrain>(imageRequestId);

        var response = await imageGeneratorGrain.generateImageAsync(imageGenerationRequest, imageRequestId);

        return response;
    }
}
