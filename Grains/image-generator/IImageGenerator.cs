using Schrodinger.Backend.Abstractions.ApiKeys;
using Schrodinger.Backend.Abstractions.Images;

namespace Grains.image_generator;

public interface IImageGenerator
{
    Task<AIImageGenerationResponse> RunImageGenerationAsync(
        string prompt,
        ApiKey apikey,
        int numberOfImages,
        ImageSettings imageSettings,
        string requestId
    );
}
