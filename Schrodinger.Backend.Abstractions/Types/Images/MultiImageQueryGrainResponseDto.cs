namespace Schrodinger.Backend.Abstractions.Types.Images
{
    using System.Text.Json.Serialization;
    using Schrodinger.Backend.Abstractions.Constants;

    /// <summary>
    /// Represents the response from a multi-image query grain.
    /// Contains information about the image generation status, errors, and the generated images.
    /// </summary>
    [GenerateSerializer]
    public class MultiImageQueryGrainResponseDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether the image generation is uninitialized.
        /// </summary>
        [Id(0)]
        public bool Uninitialized { get; set; }

        /// <summary>
        /// Gets or sets the list of generated images.
        /// </summary>
        [Id(1)]
        public List<ImageDescription>? Images { get; set; }

        /// <summary>
        /// Gets or sets the status of the image generation.
        /// </summary>
        [Id(2)]
        public ImageGenerationStatus Status { get; set; } = ImageGenerationStatus.Dormant;

        /// <summary>
        /// Gets or sets the list of errors, if any, during the image generation.
        /// </summary>
        [Id(3)]
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Gets or sets the error code, if any, during the image generation.
        /// </summary>
        [Id(4)]
        public string? ErrorCode { get; set; } = "";
    }
}