using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Shared;

namespace Grains;

public class PromptCreatorGrain : Grain, IPromptCreatorGrain
{
    private readonly IPersistentState<PromptConfigState> _promptConfigState;

    public PromptCreatorGrain(
        [PersistentState("promptConfigState", "MySqlSchrodingerImageStore")]
        IPersistentState<PromptConfigState> promptConfigState)
    {
        _promptConfigState = promptConfigState;
    }

    public async void SetPromptState(SetPromptStateRequest setPromptStateRequest)
    {
        var scriptContent = setPromptStateRequest.ScriptContent;
        var configText = setPromptStateRequest.ConfigText;

        _promptConfigState.State.ConfigText = configText;
        _promptConfigState.State.ScriptContent = scriptContent;

        await _promptConfigState.WriteStateAsync();
    }

    public Task<PromptConfigOptions> ReadPromptState()
    {
        return Task.FromResult(new PromptConfigOptions
        {
            ConfigText = _promptConfigState.State.ConfigText,
            ScriptContent = _promptConfigState.State.ScriptContent
        });
    }

    public Task<string> Generate(PromptGenerationRequest promptGenerationRequest)
    {
        if (string.IsNullOrEmpty(_promptConfigState.State.ScriptContent) || _promptConfigState.State.ConfigText == null)
        {
            return Task.FromResult("");
        }

        // Load and execute the JavaScript file
        var scriptContent = _promptConfigState.State.ScriptContent;
        var configText = _promptConfigState.State.ConfigText;

        // Serialize C# objects to JSON
        var traits = promptGenerationRequest.BaseImage.Traits.Concat(promptGenerationRequest.NewTraits).ToList();

        using var engine = new V8ScriptEngine();
        engine.Execute(scriptContent);

        // Pass the JSON string to JavaScript, parse it, and call 'myFunction'
        var config = JsonConvert.SerializeObject(configText).ToLower();
        var traitArgs = JsonConvert.SerializeObject(traits).ToLower();

        engine.Execute($"var config = JSON.parse('{config.Replace("'", "\\'")}')");
        engine.Execute($"var traitArgs = JSON.parse('{traitArgs.Replace("'", "\\'")}');");
        var result = engine.Script.createPrompt(engine.Script.config, engine.Script.traitArgs);

        return Task.FromResult(result.ToString());
    }
}