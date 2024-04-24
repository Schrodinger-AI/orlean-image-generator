using Schrodinger.Backend.Abstractions.ApiKeys;
using Schrodinger.Backend.Abstractions.Images;

namespace Schrodinger.Backend.Grains.ImageGenerator.Ai;

public interface IAiImageGenerator
{
    Task<AiImageGenerationResponse> RunImageGenerationAsync(
        string prompt,
        ApiKey apikey,
        int numberOfImages,
        ImageSettings imageSettings,
        string requestId
    );
}
