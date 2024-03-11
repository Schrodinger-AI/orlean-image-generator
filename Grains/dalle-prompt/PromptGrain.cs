using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using Orleans;
using Shared;

namespace Grains;

public class PromptGrain : Grain, IPromptGrain
{
    public async Task<PromptGenerationResponse> generatePrompt(PromptGenerationRequest promptGenerationRequest)
    {
        try
        {
            // Serialize C# objects to JSON
            var traits = promptGenerationRequest.BaseImage.Traits.Concat(promptGenerationRequest.NewTraits).ToList();

            // Load and execute the JavaScript file
            var scriptContent = promptGenerationRequest.ScriptContent;

            using var engine = new V8ScriptEngine();
            engine.Execute(scriptContent);

            // Pass the JSON string to JavaScript, parse it, and call 'myFunction'
            var configText = JsonConvert.SerializeObject(promptGenerationRequest.ConfigText).ToLower();
            var traitArgs = JsonConvert.SerializeObject(traits).ToLower();

            engine.Execute($"var config = JSON.parse('{configText.Replace("'", "\\'")}')");
            engine.Execute($"var traitArgs = JSON.parse('{traitArgs.Replace("'", "\\'")}');");
            var result = engine.Script.createPrompt(engine.Script.config, engine.Script.traitArgs);

            return new PromptGenerationResponseOk
            {
                Prompt = result.ToString()
            };
        }
        catch (Exception e)
        {
            return new PromptGenerationResponseNotOk {Error = "prompt generation error, e: " + e.Message};
        }
    }
}