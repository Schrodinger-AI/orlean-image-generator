namespace Shared.Abstractions.Images
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an attribute of an image.
    /// Contains information about the trait type and its value.
    /// </summary>
    [GenerateSerializer]
    public class Attribute
    {
        /// <summary>
        /// Gets or sets the type of the trait.
        /// </summary>
        [JsonPropertyName("traitType")]
        [Id(0)]
        public string TraitType { get; set; }

        /// <summary>
        /// Gets or sets the value of the trait.
        /// </summary>
        [JsonPropertyName("value")]
        [Id(1)]
        public string Value { get; set; }
    }
}