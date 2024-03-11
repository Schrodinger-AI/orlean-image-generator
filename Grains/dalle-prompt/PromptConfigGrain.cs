using Orleans;
using Orleans.Runtime;
using Shared;

namespace Grains;

public class PromptConfigGrain : Grain, IPromptConfigGrain
{
    private readonly IPersistentState<PromptConfigState> _promptConfigState;

    public PromptConfigGrain([PersistentState("promptConfigState", "MySqlSchrodingerImageStore")] IPersistentState<PromptConfigState> promptConfigState)
    {
        _promptConfigState = promptConfigState;
    }
    
    public async Task<PromptConfigResponse> ConfigPrompt(PromptConfigRequest promptConfigRequest)
    {
        try
        {
            // Load and execute the JavaScript file
            var scriptContent = promptConfigRequest.ScriptContent;
            var configText = promptConfigRequest.ConfigText;

            _promptConfigState.State.ConfigText = configText;
            _promptConfigState.State.ScriptContent = scriptContent;
            
            await _promptConfigState.WriteStateAsync();

            return new PromptConfigResponseOk()
            {
                Result = "success"
            };
        }
        catch (Exception e)
        {
            return new PromptConfigResponseNotOk {Error = "prompt config error, e: " + e.Message};
        }
    }
    
    public async Task<PromptConfigResponse> QueryPromptConfig()
    {
        try
        {
            if (string.IsNullOrEmpty(_promptConfigState.State.ScriptContent) || _promptConfigState.State.ConfigText == null)
            {
                return new PromptConfigResponseNotOk() {Error = "prompt generation error, e: "};
            }
            
            return new PromptConfigQueryResponseOk
            {
                ConfigText = _promptConfigState.State.ConfigText,
                ScriptContent = _promptConfigState.State.ScriptContent
            };
        }
        catch (Exception e)
        {
            return new PromptConfigResponseNotOk {Error = "prompt config error, e: " + e.Message};
        }
    }
    
}