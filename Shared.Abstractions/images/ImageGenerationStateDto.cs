namespace Shared.Abstractions.Images;

using System.Text.Json.Serialization;
using Shared.Abstractions.Constants;

/// <summary>
/// Represents the state of an image generation process.
/// Contains information about the request ID, parent request ID, status, prompt, image URL, image description, error, service provider, image generation timestamp, and error code.
/// </summary>
[GenerateSerializer]
public class ImageGenerationStateDto
{
    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    [Id(0)]
    public string RequestId { get; set; }

    /// <summary>
    /// Gets or sets the parent request ID.
    /// </summary>
    [Id(1)]
    public string ParentRequestId { get; set; }

    /// <summary>
    /// Gets or sets the status of the image generation process.
    /// </summary>
    [Id(2)]
    public ImageGenerationStatus Status { get; set; } = ImageGenerationStatus.Dormant;

    /// <summary>
    /// Gets or sets the prompt for the image generation.
    /// </summary>
    [Id(3)]
    public string Prompt { get; set; }
    
    /// <summary>
    /// Gets or sets the URL of the generated image, if available.
    /// </summary>
    [Id(4)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the description of the generated image, if available.
    /// </summary>
    [Id(5)]
    public ImageDescription? Image { get; set; } = null;

    /// <summary>
    /// Gets or sets the error message, if any, during the image generation process.
    /// </summary>
    [Id(6)]
    public string? Error { get; set; } = null;
    
    /// <summary>
    /// Gets or sets the service provider used for the image generation process.
    /// </summary>
    [Id(7)]
    public ImageGenerationServiceProvider? ServiceProvider { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp of the image generation process in epoch milliseconds (GMT).
    /// </summary>
    [Id(8)]
    public long? ImageGenerationTimestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the error code, if any, during the image generation process.
    /// </summary>
    [Id(9)]
    public ImageGenerationErrorCode? ErrorCode { get; set; }
}