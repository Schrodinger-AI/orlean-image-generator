namespace Shared.Abstractions.Images;

using System.Text.Json.Serialization;

[GenerateSerializer]
public class ImageDescription
{
    [JsonPropertyName("image")]
    [Id(0)]
    public string? Image { get; set; } = null;

    [JsonPropertyName("attributes")]
    [Id(1)]
    public List<Attribute> Attributes { get; set; } = [];

    [JsonPropertyName("extraData")]
    [Id(2)]
    public string? ExtraData { get; set; } = null;
}