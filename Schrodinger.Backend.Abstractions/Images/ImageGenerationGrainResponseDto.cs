namespace Schrodinger.Backend.Abstractions.Images
{
    using System.Text.Json.Serialization;
    using Schrodinger.Backend.Abstractions.Constants;

    /// <summary>
    /// Represents the response from an image generation grain.
    /// Contains information about the request ID, timestamp, success status, and any errors.
    /// </summary>
    [GenerateSerializer]
    public class ImageGenerationGrainResponseDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the request.
        /// </summary>
        [Id(0)]
        public string RequestId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the image generation request.
        /// </summary>
        [Id(1)]
        public long ImageGenerationRequestTimestamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the image generation was successful.
        /// </summary>
        [Id(2)]
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the error message, if any, during the image generation.
        /// </summary>
        [Id(3)]
        public string? Error { get; set; }

        /// <summary>
        /// Gets or sets the error code, if any, during the image generation.
        /// </summary>
        [Id(4)]
        public ImageGenerationErrorCode? ErrorCode { get; set; }
    }
}