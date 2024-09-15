using Schrodinger.Backend.Abstractions.Types.ApiKeys;
using Schrodinger.Backend.Abstractions.Types.Images;

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
