using System.Net;
using Microsoft.Extensions.Logging;

namespace Grains;

public class AzureOpenAIImageGenerator : IImageGenerator
{
    private readonly ILogger<ImageGeneratorGrain> _logger;
    
    public AzureOpenAIImageGenerator(
        ILogger<ImageGeneratorGrain> logger)
    {
        _logger = logger;
    }
    
    public async Task<ImageGenerationResponse> RunImageGenerationAsync(string prompt, string apikey, int numberOfImages, string requestId)
    {
        throw new Exception("Azure ImageGeneration Not Implemented");
    }

    public ImageGenerationError HandleImageGenerationError(HttpStatusCode httpStatusCode, string responseJson)
    {
        throw new Exception("Azure HandleImageGenerationError Not Implemented");
    }
}