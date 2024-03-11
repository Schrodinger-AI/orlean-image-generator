using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Shared;

namespace Grains;

public class PromptGeneratorGrain : Grain, IPromptGeneratorGrain
{
    private readonly PromptBuilder _promptBuilder;
    private readonly IPersistentState<PromptConfigState> _promptConfigState;
    
    public PromptGeneratorGrain(PromptBuilder promptBuilder, [PersistentState("promptConfigState", "MySqlSchrodingerImageStore")] IPersistentState<PromptConfigState> promptConfigState)
    {
        _promptBuilder = promptBuilder;
        _promptConfigState = promptConfigState;
    }

    public async Task<string> generatePrompt(PromptGenerationRequest promptGenerationRequest) {

        List<Trait> newTraits = promptGenerationRequest.NewTraits;
        List<Trait> baseTraits = promptGenerationRequest.BaseImage.Traits;

        //collect the newTraits from the request and combine it with trats from the base image
        IEnumerable<Trait> traits = newTraits.Concat(baseTraits);

        // Extract trait names from the request
        Dictionary<string, TraitEntry> traitDefinitions = await lookupTraitDefinitions(traits.ToList());

        string prompt = await generatePrompt(traits.ToList(), traitDefinitions);
            
        return prompt;
    }

    public async Task<Dictionary<string, TraitEntry>> lookupTraitDefinitions(List<Trait> requestTraits)
    {
        // Extract trait names from the request
        var traitNames = requestTraits.Select(t => t.Name).ToList();

        // Get a reference to the TraitConfigGrain
        var traitConfigGrain = GrainFactory.GetGrain<ITraitConfigGrain>("traitConfigGrain");

        // Retrieve the trait definitions from the TraitConfigGrain
        var response = await traitConfigGrain.GetTraitsMap(traitNames);

        return response;
    }

    public async Task<String> generatePrompt(List<Trait> requestTraits, Dictionary<string, TraitEntry> traitDefinitions)
    {
        var sentences = await _promptBuilder.GenerateSentences(requestTraits, traitDefinitions);
        var prompt = await _promptBuilder.GenerateFinalPromptFromSentences(ImageGenerationConstants.DALLE_BASE_PROMPT, sentences);
        return prompt;
    }

    public async Task<PromptGenerationResponse> GeneratePrompt(PromptGenerationRequest promptGenerationRequest)
    {
        try
        {
            var promptConfigGrain = GrainFactory.GetGrain<IPromptConfigGrain>("promptConfigGrain");
            var promptConfig = await promptConfigGrain.QueryPromptConfig();
            
            if (string.IsNullOrEmpty(_promptConfigState.State.ScriptContent) || _promptConfigState.State.ConfigText == null)
            {
                return new PromptGenerationResponseNotOk {Error = "prompt generation error, e: "};
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