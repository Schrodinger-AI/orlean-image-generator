namespace Shared.Abstractions.Images;

using System.Text.Json.Serialization;

[GenerateSerializer]
public class Attribute
{
    [JsonPropertyName("traitType")]
    [Id(0)]
    public string TraitType { get; set; }

    [JsonPropertyName("value")]
    [Id(1)]
    public string Value { get; set; }
}