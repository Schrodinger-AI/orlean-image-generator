using Shared;

namespace Grains;

public class ImageGenerationState
{
        public Dictionary<string, string> ImageMap { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, string> ImageUrlMap { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, ImageGenerationRequest> ImageGenerationRequestMap { get; set; } = new Dictionary<string, ImageGenerationRequest>();
}
