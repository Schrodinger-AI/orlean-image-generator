using Shared;

namespace Grains;

public class ImageGenerationState
{
    public Dictionary<string, Task<string>> ImageMap { get; set; } = new Dictionary<string, Task<string>>();
    public Dictionary<string, ImageGenerationRequest> ImageGenerationRequestMap { get; set; } = new Dictionary<string, ImageGenerationRequest>();
}
