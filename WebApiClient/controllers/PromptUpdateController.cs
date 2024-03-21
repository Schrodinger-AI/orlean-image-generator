using Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Shared;
using Attribute = Shared.Attribute;

namespace WebApi.Controllers;

[ApiController]
[Route("prompt-update")]
public class PromptUpdateController : ControllerBase
{
    private readonly IClusterClient _client;

    private readonly ILogger<PromptUpdateController> _logger;


    public PromptUpdateController(IClusterClient client, ILogger<PromptUpdateController> logger)
    {
        _client = client;
        _logger = logger;
    }

    [HttpPost]
    public async Task UpdatePrompt(PromptUpdateRequest promptUpdateRequest)
    {
        //Update the MultiImageGeneratorGrain state - prompt
        var multiImageGeneratorGrain = _client.GetGrain<IMultiImageGeneratorGrain>(promptUpdateRequest.MultiImageRequestId);
        await multiImageGeneratorGrain.UpdatePromptAndAttributes(promptUpdateRequest.Prompt, promptUpdateRequest.Attributes);
        
        //update ImageGeneratorGrain state - prompt
        var grain = _client.GetGrain<IImageGeneratorGrain>(promptUpdateRequest.RequestId);
        await grain.UpdatePromptAsync(promptUpdateRequest.Prompt);
    }
}