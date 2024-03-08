using Shared;

namespace Grains;

public class ImageGenerationState
{
    public string? RequestId { get; set; }
    public string? ImageUrl { get; set; }
    public ImageQueryResponse? ImageQueryResponse { get; set; }
    public ImageGenerationRequest? ImageGenerationRequest { get; set; }
}
