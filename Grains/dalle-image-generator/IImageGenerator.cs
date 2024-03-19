using Shared;

namespace Grains.ImageGenerator;

public interface IImageGenerator
{
    Task<ImageGenerationResponse> RunImageGenerationAsync(string prompt, string apikey, int numberOfImages, ImageSettings imageSettings, string requestId);
}