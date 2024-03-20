using Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Shared;
using Attribute = Shared.Attribute;

namespace WebApi.Controllers;

[ApiController]
[Route("image")]
public class ImageUpdateController : ControllerBase
{
    private readonly IClusterClient _client;

    private readonly ILogger<ImageUpdateController> _logger;


    public ImageUpdateController(IClusterClient client, ILogger<ImageUpdateController> logger)
    {
        _client = client;
        _logger = logger;
    }

    [HttpPost("update")]
    public async Task UpdateImage(ImageUpdateRequest imageUpdateRequest)
    {
        //Update the MultiImageGeneratorGrain state - prompt
        var multiImageGeneratorGrain = _client.GetGrain<IMultiImageGeneratorGrain>(imageUpdateRequest.MultiImageRequestId);
        await multiImageGeneratorGrain.UpdatePrompt(imageUpdateRequest.Prompt, imageUpdateRequest.Attributes);
        
        //update ImageGeneratorGrain state - Image and prompt
        var grain = _client.GetGrain<IImageGeneratorGrain>(imageUpdateRequest.RequestId);
        await grain.UpdateImageAsync(imageUpdateRequest.Prompt);
    }
}