using Shared.Abstractions.Constants;
using Attribute = Shared.Abstractions.Images.Attribute;

namespace Grains.image_generator;

[GenerateSerializer]
public class MultiImageGenerationState
{
    [Id(0)]
    public string RequestId { get; set; }

    [Id(1)]
    public List<Attribute> Traits { get; set; }

    [Id(2)]
    public string Prompt { get; set; }

    [Id(3)]
    public bool IsSuccessful { get; set; }

    [Id(4)]
    public List<string> ImageGenerationRequestIds = [];
    
    [Id(5)]
    public Dictionary<string, ImageGenerationTracker> imageGenerationTrackers = [];
    
    [Id(6)]
    public ImageGenerationStatus ImageGenerationStatus { get; set; }
    
    [Id(7)]
    public ImageGenerationErrorCode? ErrorCode { get; set; }
}

[GenerateSerializer]
public class ImageGenerationTracker
{
    [Id(0)]
    public string RequestId { get; set; }

    [Id(1)]
    public ImageGenerationStatus Status { get; set; }

    [Id(2)]
    public string? Error { get; set; }
    
    [Id(3)]
    public ImageGenerationErrorCode? ErrorCode { get; set; }
}
