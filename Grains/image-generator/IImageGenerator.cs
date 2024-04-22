using Shared.Abstractions.ApiKeys;
using Shared.Abstractions.Images;

namespace Grains.image_generator;

public interface IImageGenerator
{
    Task<AIImageGenerationResponse> RunImageGenerationAsync(string prompt, ApiKey apikey, int numberOfImages, ImageSettings imageSettings, string requestId);
}