using Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Shared;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PromptController : ControllerBase
    {
        private readonly IClusterClient _client;

        public PromptController(IClusterClient client)
        {
            _client = client;
        }

        [HttpPost("generate")]
        public async Task<ActionResult> generatePrompt(PromptGenerationRequest promptGenerationRequest)
        {
            var promptGeneratorGrain = _client.GetGrain<IPromptGeneratorGrain>("promptGeneratorGrain");
            var prompt = await promptGeneratorGrain.generatePrompt(promptGenerationRequest);
            return Ok(prompt);
        }   
    }
}