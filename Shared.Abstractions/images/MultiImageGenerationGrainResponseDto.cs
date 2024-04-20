namespace Shared.Abstractions.Images
{
    using System.Text.Json.Serialization;
    using Shared.Abstractions.Constants;

    /// <summary>
    /// Represents the response from a multi-image generation grain.
    /// Contains information about the request ID, prompt, traits, success status, and any errors.
    /// </summary>
    [GenerateSerializer]
    public class MultiImageGenerationGrainResponseDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the request.
        /// </summary>
        [Id(0)]
        public string RequestId { get; set; }

        /// <summary>
        /// Gets or sets the prompt used for the image generation.
        /// </summary>
        [Id(1)]
        public string Prompt { get; set; }

        /// <summary>
        /// Gets or sets the list of traits used for the image generation.
        /// </summary>
        [Id(2)]
        public List<Attribute> Traits { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the image generation was successful.
        /// </summary>
        [Id(3)]
        public bool IsSuccessful { get; set; } = false;

        /// <summary>
        /// Gets or sets the list of errors, if any, during the image generation.
        /// </summary>
        [Id(4)]
        public List<string>? Errors { get; set; }
    }
}