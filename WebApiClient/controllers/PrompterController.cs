using Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Shared;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("prompt")]
    public class PrompterController : ControllerBase
    {
        private static string _identifier = "main";
        private readonly IClusterClient _client;

        public PrompterController(IClusterClient client)
        {
            _client = client;
        }

        [HttpPost("set-config")]
        public async Task<PrompterResponse> SetConfig(SetPromptConfigRequest setPromptConfigRequest)
        {
            try
            {
                var prompterGrain = _client.GetGrain<IPrompterGrain>(_identifier);
                var res = await prompterGrain.SetConfigAsync(new PrompterConfig
                {
                    ConfigText = setPromptConfigRequest.ConfigText,
                    ScriptContent = setPromptConfigRequest.ScriptContent,
                    ValidationTestCase = setPromptConfigRequest.ValidationTestCase
                });
                if (res)
                {
                    return new PrompterResponseOk {Result = "success"};
                }
                return new PrompterResponseNotOk {Error = "set prompt config fail error, invalid input"};
            }
            catch (Exception e)
            {
                return new PrompterResponseNotOk {Error = "set prompt config error, msg: " + e.Message};
            }
        }

        [HttpPost("get-config")]
        public async Task<PrompterResponse> GetConfig(GetPromptConfigRequest getPromptConfigRequest)
        {
            try
            {
                var prompterGrain = _client.GetGrain<IPrompterGrain>(getPromptConfigRequest.Identifier);
                var result = await prompterGrain.GetConfigAsync();
                return new PrompterResponseOk {Result = result};
            }
            catch (Exception e)
            {
                return new PrompterResponseNotOk {Error = "get prompt config error, msg: " + e.Message};
            }
        }

        [HttpPost("generate")]
        public async Task<PrompterResponse> Generate(PromptGenerationRequest promptGenerationRequest)
        {
            try
            {
                var promptCreatorGrain = _client.GetGrain<IPrompterGrain>(_identifier);
                var result = await promptCreatorGrain.GeneratePromptAsync(promptGenerationRequest);
                if (!string.IsNullOrEmpty(result))
                {
                    return new PrompterResponseOk {Result = result};
                }

                return new PrompterResponseNotOk {Error = "generate prompt fail, invalid input"};
            }
            catch (Exception e)
            {
                return new PrompterResponseNotOk {Error = "generate prompt error, msg: " + e.Message};
            }
        }

        [HttpPost("switch-identifier")]
        public async Task<PrompterResponse> SwitchIdentifier(SwitchIdentifierRequest switchIdentifierRequest)
        {
            _identifier = switchIdentifierRequest.Identifier;
            return new PrompterResponseOk { Result = await Task.FromResult("success") };
        }
        
        [HttpPost("get-current-config")]
        public async Task<PrompterResponse> GetCurrenConfig()
        {
            try
            {
                var prompterGrain = _client.GetGrain<IPrompterGrain>(_identifier);
                var result = await prompterGrain.GetConfigAsync();
                return new PrompterResponseOk {Result = result};
            }
            catch (Exception e)
            {
                return new PrompterResponseNotOk {Error = "get current prompt config error, msg: " + e.Message};
            }
        }
        
    }
    
}