using Shared;
using Attribute = Shared.Attribute;

namespace Grains;

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
    public List<string>? Errors { get; set; }

    [Id(5)]
    public List<string> ImageGenerationRequestIds = [];
    [Id(6)]
    public Dictionary<string, ImageGenerationTracker> imageGenerationTrackers = [];
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
}
