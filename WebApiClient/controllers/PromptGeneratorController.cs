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
            var prompt = await promptGeneratorGrain.GeneratePrompt(promptGenerationRequest);
            return Ok(prompt);
        }
        
        [HttpPost("generateTest")]
        public async Task<PromptGenerationResponse> generatePromptTest(PromptGenerationRequest promptGenerationRequest)
        {
            var promptGrain = _client.GetGrain<IPromptGrain>("promptGrain");
            var promptGenerationResponse = await promptGrain.generatePrompt(promptGenerationRequest);
            return promptGenerationResponse;
        }
    }
}