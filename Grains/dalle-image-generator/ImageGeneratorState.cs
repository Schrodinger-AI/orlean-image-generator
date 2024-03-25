using Shared;

namespace Grains;

[GenerateSerializer]
public class ImageGenerationState
{
    [Id(0)]
    public string RequestId { get; set; }

    [Id(1)]
    public string ParentRequestId { get; set; }

    [Id(2)]
    public ImageGenerationStatus Status { get; set; } = ImageGenerationStatus.Dormant;

    [Id(3)]
    public string Prompt { get; set; }
    
    [Id(4)]
    public string? ImageUrl { get; set; }

    [Id(5)]
    public ImageDescription? Image { get; set; } = null;

    [Id(6)]
    public string? Error { get; set; } = null;
    
    [Id(7)]
    public ImageGenerationServiceProvider? ServiceProvider { get; set; }
    
    // New property for the image generation timestamp in epoch milliseconds (GMT)
    // Now it's nullable and not set by default
    [Id(8)]
    public long? ImageGenerationTimestamp { get; set; }
    
    [Id(9)]
    public ImageGenerationErrorCode? ErrorCode { get; set; }
}
