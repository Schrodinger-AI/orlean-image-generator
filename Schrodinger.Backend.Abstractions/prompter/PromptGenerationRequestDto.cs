namespace Schrodinger.Backend.Abstractions.Prompter
{
    using System.Text.Json.Serialization;
    using Attribute = Schrodinger.Backend.Abstractions.Images.Attribute;
    using Schrodinger.Backend.Abstractions.Images;

    [GenerateSerializer]
    public class PromptGenerationRequestDto
    {
        [JsonPropertyName("identifier")]
        [Id(0)]
        public string Identifier { get; set; }

        [JsonPropertyName("newAttributes")]
        [Id(1)]
        public List<Attribute> NewAttributes { get; set; }

        [JsonPropertyName("baseImage")]
        [Id(2)]
        public ImageDescription? BaseImage { get; set; }
    }

}