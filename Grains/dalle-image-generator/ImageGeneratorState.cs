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

}

