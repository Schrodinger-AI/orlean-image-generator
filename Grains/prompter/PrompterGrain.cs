using Microsoft.ClearScript.V8;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans.Runtime;
using Shared.Abstractions.Interfaces;
using Shared.Abstractions.Prompter;

namespace Grains.Prompter;

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

    public async Task<bool> SetConfigAsync(PrompterConfigDto config)
    {
        _prompterState.State.ConfigText = config.ConfigText;
        _prompterState.State.ScriptContent = config.ScriptContent;
        // It's used to run GeneratePromptAsync method to valid whether configText and ScriptContent are correct
        _prompterState.State.ValidationTestCase = config.ValidationTestCase;
        // Run GeneratePromptAsync method to valid configText and ScriptContent, assign value to ValidationOk
        _prompterState.State.ValidationOk = await RunValidationTestAsync();
        await _prompterState.WriteStateAsync();
        return _prompterState.State.ValidationOk;
    }

    public async Task<PrompterConfigDto> GetConfigAsync()
    {
        return await Task.FromResult(new PrompterConfigDto
        {
            ConfigText = _prompterState.State.ConfigText,
            ScriptContent = _prompterState.State.ScriptContent,
            ValidationTestCase = _prompterState.State.ValidationTestCase,
            ValidationOk = _prompterState.State.ValidationOk
        });
    }

    private async Task<bool> RunValidationTestAsync()
    {
        var validationPayload =
            JsonConvert.DeserializeObject<PromptGenerationRequestDto>(_prompterState.State.ValidationTestCase);
        if (validationPayload == null) return false;

        var res = await GeneratePromptAsync(validationPayload);
        return !string.IsNullOrEmpty(res);
    }


    public async Task<string> GeneratePromptAsync(PromptGenerationRequestDto promptGenerationRequest)
    {
        try
        {
            var scriptContent = _prompterState.State.ScriptContent;
            var configText = _prompterState.State.ConfigText;
            var oldTraits = promptGenerationRequest.BaseImage?.Attributes ?? [];

            var traits = oldTraits.Concat(promptGenerationRequest.NewAttributes).ToList();

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