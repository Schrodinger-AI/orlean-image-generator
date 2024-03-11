using Microsoft.ClearScript.V8;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans;
using Orleans.Runtime;
using Shared;

namespace Grains;

public class PrompterGrain : Grain, IPrompterGrain
{
    private readonly ILogger<PrompterGrain> _logger;
    private readonly IPersistentState<PrompterState> _prompterState;

    public PrompterGrain(
        [PersistentState("prompterState", "MySqlSchrodingerImageStore")]
        IPersistentState<PrompterState> prompterState,
        ILogger<PrompterGrain> logger)
    {
        _prompterState = prompterState;
        _logger = logger;
    }

    public async Task<bool> SetConfigAsync(PrompterConfig config)
    {
        _prompterState.State.ConfigText = config.ConfigText;
        _prompterState.State.ScriptContent = config.ScriptContent;
        _prompterState.State.ValidationTestCase = config.ValidationTestCase;
        _prompterState.State.ValiationOk = await RunValidationTestAsync();
        await _prompterState.WriteStateAsync();
        return _prompterState.State.ValiationOk;
    }

    public async Task<PrompterConfig> GetConfigAsync()
    {
        return await Task.FromResult(new PrompterConfig
        {
            ConfigText = _prompterState.State.ConfigText,
            ScriptContent = _prompterState.State.ScriptContent,
            ValidationTestCase = _prompterState.State.ValidationTestCase
        });
    }

    private async Task<bool> RunValidationTestAsync()
    {
        var validationPayload =
            JsonConvert.DeserializeObject<PromptGenerationRequest>(_prompterState.State.ValidationTestCase);
        if (validationPayload == null) return false;

        var res = await GeneratePromptAsync(validationPayload);
        return !string.IsNullOrEmpty(res);
    }


    public async Task<string> GeneratePromptAsync(PromptGenerationRequest promptGenerationRequest)
    {
        try
        {
            var scriptContent = _prompterState.State.ScriptContent;
            var configText = _prompterState.State.ConfigText;

            var traits = promptGenerationRequest.BaseImage.Traits.Concat(promptGenerationRequest.NewTraits).ToList();

            using var engine = new V8ScriptEngine();
            engine.Execute(scriptContent);

            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var traitArgs = JsonConvert.SerializeObject(traits, serializerSettings);

            engine.Execute($"var config = JSON.parse('{configText.Replace("'", "\\'")}')");
            engine.Execute($"var traitArgs = JSON.parse('{traitArgs.Replace("'", "\\'")}');");
            var result = engine.Script.createPrompt(engine.Script.config, engine.Script.traitArgs);

            return await Task.FromResult(result.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return string.Empty;
        }
    }
}