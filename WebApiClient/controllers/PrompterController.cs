using Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using UnitTests.Grains;
using WebApi.Prompter.Models;
using SharedAttribute = Shared.Attribute;
using SharedPrompter = Shared.Prompter;
using SharedPrompterConfig = Shared.PrompterConfig;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("prompt")]
    public class PrompterController : ControllerBase
    {
        private string _configuratorIdentifier = "configurator";
        private readonly IClusterClient _client;

        public PrompterController(IClusterClient client)
        {
            _client = client;
        }

        [HttpPost("set-config")]
        public async Task<PrompterAPIResponse> SetConfig(SetPromptConfigRequest setPromptConfigRequest)
        {
            try
            {
                var configuratorGrain = _client.GetGrain<IConfiguratorGrain>(_configuratorIdentifier);
                var allConfigIds = await configuratorGrain.GetAllConfigIdsAsync();
                if (allConfigIds.Contains(setPromptConfigRequest.Identifier))
                {
                    return new PrompterResponseNotOk
                        { Error = $"a configuration with identifier \"{setPromptConfigRequest.Identifier}\" already exists" };
                }

                var prompterGrain = _client.GetGrain<IPrompterGrain>(setPromptConfigRequest.Identifier);
                var res = await prompterGrain.SetConfigAsync(new SharedPrompterConfig
                {
                    ConfigText = setPromptConfigRequest.ConfigText,
                    ScriptContent = setPromptConfigRequest.ScriptContent,
                    ValidationTestCase = setPromptConfigRequest.ValidationTestCase
                });
                if (res)
                {
                    await configuratorGrain.AddConfigIdAsync(setPromptConfigRequest.Identifier);
                    await configuratorGrain.SetCurrentConfigIdAsync(setPromptConfigRequest.Identifier);
                    return new PrompterResponseOk { Result = "success" };
                }

                return new PrompterResponseNotOk { Error = "set prompt config fail error, invalid input" };
            }
            catch (Exception e)
            {
                return new PrompterResponseNotOk { Error = "set prompt config error, msg: " + e.Message };
            }
        }

        [HttpPost("get-config")]
        public async Task<PrompterAPIResponse> GetConfig(GetPromptConfigRequest getPromptConfigRequest)
        {
            try
            {
                var prompterGrain = _client.GetGrain<IPrompterGrain>(getPromptConfigRequest.Identifier);
                var result = await prompterGrain.GetConfigAsync();
                return new PrompterResponseOk { Result = result };
            }
            catch (Exception e)
            {
                return new PrompterResponseNotOk { Error = "get prompt config error, msg: " + e.Message };
            }
        }

        [HttpPost("generate")]
        public async Task<PrompterAPIResponse> Generate(PromptGenerationAPIRequest promptGenerationAPIRequest)
        {
            try
            {
                var configuratorGrain = _client.GetGrain<IConfiguratorGrain>(_configuratorIdentifier);
                var currentConfigId = await configuratorGrain.GetCurrentConfigIdAsync();
                var promptCreatorGrain = _client.GetGrain<IPrompterGrain>(currentConfigId);
                var result = await promptCreatorGrain.GeneratePromptAsync(TransformToPromptGenerationRequest(promptGenerationAPIRequest));
                if (!string.IsNullOrEmpty(result))
                {
                    return new PrompterResponseOk { Result = result };
                }

                return new PrompterResponseNotOk { Error = "generate prompt fail, invalid input" };
            }
            catch (Exception e)
            {
                return new PrompterResponseNotOk { Error = "generate prompt error, msg: " + e.Message };
            }
        }
        
        public static SharedPrompter.PromptGenerationRequest TransformToPromptGenerationRequest(PromptGenerationAPIRequest apiRequest)
        {
            return new SharedPrompter.PromptGenerationRequest
            {
                Identifier = apiRequest.Identifier,
                NewAttributes = apiRequest.NewAttributes.Select(a => new SharedAttribute
                {
                    TraitType = a.TraitType,
                    Value = a.Value
                }).ToList(),
                BaseImage = apiRequest.BaseImage == null ? null : new Shared.ImageDescription
                {
                    Image = apiRequest.BaseImage.Image,
                    Attributes = apiRequest.BaseImage.Attributes.Select(a => new SharedAttribute
                    {
                        TraitType = a.TraitType,
                        Value = a.Value
                    }).ToList(),
                    ExtraData = apiRequest.BaseImage.ExtraData
                }
            };
        }
        
        [HttpPost("switch-identifier")]
        public async Task<PrompterAPIResponse> SwitchIdentifier(SwitchIdentifierRequest switchIdentifierRequest)
        {
            var configuratorGrain = _client.GetGrain<IConfiguratorGrain>(_configuratorIdentifier);
            var allConfigIds = await configuratorGrain.GetAllConfigIdsAsync();
            if (!allConfigIds.Contains(switchIdentifierRequest.Identifier))
            {
                return new PrompterResponseNotOk { Error = "switch identifier fail, invalid identifier" };
            }

            await configuratorGrain.SetCurrentConfigIdAsync(switchIdentifierRequest.Identifier);
            return new PrompterResponseOk { Result = await Task.FromResult("success") };
        }

        [HttpPost("get-current-config")]
        public async Task<PrompterAPIResponse> GetCurrenConfig()
        {
            try
            {
                var configuratorGrain = _client.GetGrain<IConfiguratorGrain>(_configuratorIdentifier);
                var currentConfigId = await configuratorGrain.GetCurrentConfigIdAsync();
                var prompterGrain = _client.GetGrain<IPrompterGrain>(currentConfigId);
                var result = await prompterGrain.GetConfigAsync();
                return new PrompterResponseOk { Result = result };
            }
            catch (Exception e)
            {
                return new PrompterResponseNotOk { Error = "get current prompt config error, msg: " + e.Message };
            }
        }

        [HttpPost("get-all-configs")]
        public async Task<PrompterAPIResponse> GetAllConfigs()
        {
            try
            {
                List<SharedPrompterConfig> result = new List<SharedPrompterConfig>();
                var configuratorGrain = _client.GetGrain<IConfiguratorGrain>(_configuratorIdentifier);
                var allConfigIds = await configuratorGrain.GetAllConfigIdsAsync();
                foreach (var prompterGrain in allConfigIds.Select(
                             configId => _client.GetGrain<IPrompterGrain>(configId)))
                {
                    var config = await prompterGrain.GetConfigAsync();
                    result.Add(config);
                }

                return new PrompterResponseOk { Result = result };
            }
            catch (Exception e)
            {
                return new PrompterResponseNotOk { Error = "get current prompt config error, msg: " + e.Message };
            }
        }
    }
}