using System.Text.Json.Serialization;

namespace WebApi.Models
{
    public class Attribute
    {
        [JsonPropertyName("traitType")] public string TraitType { get; set; }

        [JsonPropertyName("value")] public string Value { get; set; }
    }

    public class ImageDescription
    {
        [JsonPropertyName("image")] public string? Image { get; set; } = null;

        [JsonPropertyName("attributes")] public List<Attribute> Attributes { get; set; } = [];

        [JsonPropertyName("extraData")] public string? ExtraData { get; set; } = null;
    }
}