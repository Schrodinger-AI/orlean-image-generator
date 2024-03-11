using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Shared;

namespace Grains;

public class PrompterGrain : Grain, IPrompterGrain
{
    private readonly IPersistentState<PrompterState> _promptCreatorState;

    public PrompterGrain(
        [PersistentState("prompterState", "MySqlSchrodingerImageStore")]
        IPersistentState<PrompterState> promptCreatorState)
    {
        _promptCreatorState = promptCreatorState;
    }

    public async Task<bool> SetConfigAsync(PrompterConfig config)
    {
        _promptCreatorState.State.ConfigText = config.ConfigText;
        _promptCreatorState.State.ScriptContent = config.ScriptContent;
        _promptCreatorState.State.ValidationTestCase = config.ValidationTestCase;
        _promptCreatorState.State.ValiationOk = await RunValidationTestAsync();
        await _promptCreatorState.WriteStateAsync();
        return _promptCreatorState.State.ValiationOk;
    }

    public async Task<PrompterConfig> GetConfigAsync()
    {
        return await Task.FromResult(new PrompterConfig
        {
            ConfigText = _promptCreatorState.State.ConfigText,
            ScriptContent = _promptCreatorState.State.ScriptContent
        });
    }

    private async Task<bool> RunValidationTestAsync()
    {
        var validationPayload =
            JsonConvert.DeserializeObject<PromptGenerationRequest>(_promptCreatorState.State.ValidationTestCase);
        if (validationPayload == null) return false;

        var res = await GeneratePromptAsync(validationPayload);
        return !string.IsNullOrEmpty(res);
    }


    public async Task<string> GeneratePromptAsync(PromptGenerationRequest promptGenerationRequest)
    {
        try
        {
            var scriptContent = _promptCreatorState.State.ScriptContent;
            var configText = _promptCreatorState.State.ConfigText;

            var traits = promptGenerationRequest.BaseImage.Traits.Concat(promptGenerationRequest.NewTraits).ToList();

            using var engine = new V8ScriptEngine();
            engine.Execute(scriptContent);

            var traitArgs = JsonConvert.SerializeObject(traits);

            engine.Execute($"var config = JSON.parse('{configText.Replace("'", "\\'")}')");
            engine.Execute($"var traitArgs = JSON.parse('{traitArgs.Replace("'", "\\'")}');");
            var result = engine.Script.createPrompt(engine.Script.config, engine.Script.traitArgs);

            return await Task.FromResult(result.ToString());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            // TODO: Add logs
            return string.Empty;
        }
    }
}