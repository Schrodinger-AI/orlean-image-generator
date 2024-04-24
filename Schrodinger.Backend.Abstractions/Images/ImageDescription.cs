namespace Schrodinger.Backend.Abstractions.Images
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the description of an image.
    /// Contains information about the image, its attributes, and any extra data.
    /// </summary>
    [GenerateSerializer]
    public class ImageDescription
    {
        /// <summary>
        /// Gets or sets the image.
        /// </summary>
        [JsonPropertyName("image")]
        [Id(0)]
        public string? Image { get; set; } = null;

        /// <summary>
        /// Gets or sets the list of attributes associated with the image.
        /// </summary>
        [JsonPropertyName("attributes")]
        [Id(1)]
        public List<Attribute> Attributes { get; set; } = [];

        /// <summary>
        /// Gets or sets any extra data associated with the image.
        /// </summary>
        [JsonPropertyName("extraData")]
        [Id(2)]
        public string? ExtraData { get; set; } = null;
    }
}