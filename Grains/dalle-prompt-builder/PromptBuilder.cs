using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Shared;

namespace Grains;

public class PromptBuilder
{
    public async Task<List<string>> GenerateSentences(List<Trait> requestTraits, Dictionary<string, TraitEntry> traitDefinitions)
    {
        var sentences = new List<string>();

        foreach (var trait in requestTraits)
        {
            var traitName = trait.Name;

            if (traitDefinitions.TryGetValue(traitName, out var traitDef))
            {
                // Assuming Validate is an existing method that validates a traitDef object
                // var errors = await Validate(traitDef);
                // if (errors.Count > 0)
                // {
                //     throw new Exception($"Validation failed during GeneratePromptAsync Sentences for trait: {JsonSerializer.Serialize(traitValue)}");
                // }

                if (traitDef.Values.Contains(trait.Value))
                {
                    sentences.Add(traitDef.Variation.Replace("%s", trait.Value));
                }
                else
                {
                    throw new Exception($"Trait value `{trait.Value}` is not found under TraitName: `{traitName}` in trait definitions -> valid TraitValues are: {string.Join(", ", traitDef.Values)}");
                }
            }
            else
            {
                throw new Exception($"Trait {traitName} not found in trait definitions");
            }
        }

        Console.WriteLine($"Sentences derived from traits are: {string.Join(", ", sentences)}");

        if (sentences.Count == 0)
        {
            throw new Exception("No sentences were generated from traits");
        }

        return sentences;
    }

    public async Task<string> GenerateFinalPromptFromSentences(string basePrompt, List<string> sentences)
    {
        var sentenceText = basePrompt + "\n" + string.Join('\n', sentences);
        Console.WriteLine("sentenceText: " + sentenceText);

        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new
            {
                model = "gpt-4-0125-preview",
                messages = new[]
                {
                new
                {
                    role = "user",
                    content = new[]
                    {
                        new { type = "text", text = sentenceText }
                    }
                }
            },
                max_tokens = 300
            };

            var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", content);
            Console.WriteLine("sentenceText: " + response);

            response.EnsureSuccessStatusCode();

            Console.WriteLine("Prompt generated successfully");

            var jsonString = await response.Content.ReadAsStringAsync();

            Console.WriteLine("jsonString is: " + jsonString);

            Result result = JsonSerializer.Deserialize<Result>(jsonString);
            Console.WriteLine("result is: " + result);
            string promptContent = result.Choices[0].Message.Content;
            return promptContent;
        }
    }

}