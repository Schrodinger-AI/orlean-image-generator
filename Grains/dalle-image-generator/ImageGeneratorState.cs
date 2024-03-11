using Shared;

namespace Grains;

public class ImageGenerationState
{
    public string RequestId { get; set; }
    public string? ImageUrl { get; set; }

    public ImageDescription? Image { get; set; } = null;

    public string? Error { get; set; } = null;

    public List<Trait> Traits { get; set; }
}
