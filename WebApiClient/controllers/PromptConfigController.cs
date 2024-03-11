using Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Shared;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PromptConfigController : ControllerBase
    {
        private readonly IClusterClient _client;

        public PromptConfigController(IClusterClient client)
        {
            _client = client;
        }

        [HttpPost("config")]
        public async Task<PromptConfigResponse> ConfigPrompt(PromptConfigRequest promptConfigRequest)
        {
            var promptConfigGrain = _client.GetGrain<IPromptConfigGrain>("promptConfigGrain");
            return await promptConfigGrain.ConfigPrompt(promptConfigRequest);
        }
    }
}