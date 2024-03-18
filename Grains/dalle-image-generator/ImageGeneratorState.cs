using Shared;

namespace Grains;

public class ImageGenerationState
{
    public string RequestId { get; set; }

    public string ParentRequestId { get; set; }

    public ImageGenerationStatus Status { get; set; } = ImageGenerationStatus.Dormant;

    public string Prompt { get; set; }
    
    public string? ImageUrl { get; set; }

    public ImageDescription? Image { get; set; } = null;

    public string? Error { get; set; } = null;
    
    public ImageGenerationServiceProvider? ServiceProvider { get; set; }
    
    // New property for the image generation timestamp in epoch milliseconds (GMT)
    // Now it's nullable and not set by default
    public long? ImageGenerationTimestamp { get; set; }
}

