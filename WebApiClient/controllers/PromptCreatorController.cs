using Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Shared;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("prompt")]
    public class PromptCreatorController : ControllerBase
    {
        private readonly IClusterClient _client;

        public PromptCreatorController(IClusterClient client)
        {
            _client = client;
        }

        [HttpPost("set")]
        public async Task<PromptCreatorResponse> SetPromptState(SetPromptStateRequest setPromptStateRequest)
        {
            try
            {
                var promptCreatorGrain = _client.GetGrain<IPrompterGrain>("promptCreatorGrain");
                await promptCreatorGrain.SetConfigAsync(new PrompterConfig
                {
                    ConfigText = setPromptStateRequest.ConfigText,
                    ScriptContent = setPromptStateRequest.ScriptContent,
                    ValidationTestCase = setPromptStateRequest.ValidationTestCase
                });
                return new PromptCreatorResponseOk { Result = "success" };
            }
            catch (Exception e)
            {
                return new PromptCreatorResponseNotOk { Error = "set prompt state error, msg: " + e.Message };
            }
        }

        [HttpPost("read")]
        public async Task<PromptCreatorResponse> ReadPromptState()
        {
            try
            {
                var promptCreatorGrain = _client.GetGrain<IPrompterGrain>("promptCreatorGrain");
                var result = await promptCreatorGrain.GetConfigAsync();
                return new PromptCreatorResponseOk { Result = result };
            }
            catch (Exception e)
            {
                return new PromptCreatorResponseNotOk { Error = "read prompt state error, msg: " + e.Message };
            }
        }

        [HttpPost("generate")]
        public async Task<PromptCreatorResponse> Generate(PromptGenerationRequest promptGenerationRequest)
        {
            try
            {
                var promptCreatorGrain = _client.GetGrain<IPrompterGrain>("promptCreatorGrain");
                var result = await promptCreatorGrain.GeneratePromptAsync(promptGenerationRequest);
                return new PromptCreatorResponseOk { Result = result };
            }
            catch (Exception e)
            {
                return new PromptCreatorResponseNotOk { Error = "generate prompt error, msg: " + e.Message };
            }
        }
    }
}