namespace Schrodinger.Backend.Abstractions.Types.Images
{
    using System.Text.Json.Serialization;
    using Schrodinger.Backend.Abstractions.Constants;

    /// <summary>
    /// Represents the response from an image query grain.
    /// Contains information about the generated image, its status, and any errors.
    /// </summary>
    [GenerateSerializer]
    public class ImageQueryGrainResponseDto
    {
        /// <summary>
        /// Gets or sets the generated image.
        /// </summary>
        [Id(0)]
        public ImageDescription? Image { get; set; }

        /// <summary>
        /// Gets or sets the status of the image generation.
        /// </summary>
        [Id(1)]
        public ImageGenerationStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the error message, if any, during the image generation.
        /// </summary>
        [Id(2)]
        public string? Error { get; set; }
    }
}