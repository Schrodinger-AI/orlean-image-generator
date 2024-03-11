using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Shared;

namespace Grains;

public class PromptCreatorGrain : Grain, IPromptCreatorGrain
{
    private readonly IPersistentState<PromptCreatorState> _promptCreatorState;

    public PromptCreatorGrain(
        [PersistentState("promptCreatorState", "MySqlSchrodingerImageStore")]
        IPersistentState<PromptCreatorState> promptCreatorState)
    {
        _promptCreatorState = promptCreatorState;
    }

    public async void SetPromptState(SetPromptStateRequest setPromptStateRequest)
    {
        var scriptContent = setPromptStateRequest.ScriptContent;
        var configText = setPromptStateRequest.ConfigText;

        _promptCreatorState.State.ConfigText = configText;
        _promptCreatorState.State.ScriptContent = scriptContent;

        await _promptCreatorState.WriteStateAsync();
    }

    public Task<PromptConfigOptions> ReadPromptState()
    {
        return Task.FromResult(new PromptConfigOptions
        {
            ConfigText = _promptCreatorState.State.ConfigText,
            ScriptContent = _promptCreatorState.State.ScriptContent
        });
    }

    public Task<string> Generate(PromptGenerationRequest promptGenerationRequest)
    {
        var scriptContent = _promptCreatorState.State.ScriptContent;
        var configText = _promptCreatorState.State.ConfigText;
        
        if (string.IsNullOrEmpty(scriptContent) || configText == null)
        {
            return Task.FromResult("");
        }

        var traits = promptGenerationRequest.BaseImage.Traits.Concat(promptGenerationRequest.NewTraits).ToList();

        using var engine = new V8ScriptEngine();
        engine.Execute(scriptContent);

        var config = JsonConvert.SerializeObject(configText).ToLower();
        var traitArgs = JsonConvert.SerializeObject(traits).ToLower();

        engine.Execute($"var config = JSON.parse('{config.Replace("'", "\\'")}')");
        engine.Execute($"var traitArgs = JSON.parse('{traitArgs.Replace("'", "\\'")}');");
        var result = engine.Script.createPrompt(engine.Script.config, engine.Script.traitArgs);

        return Task.FromResult(result.ToString());
    }
}