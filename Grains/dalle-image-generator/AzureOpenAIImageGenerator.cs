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
    
    // Your existing code goes here, but with the method names changed to match the interface
    public async Task<ImageGenerationResponse> RunImageGenerationAsync(string prompt, string apikey, int numberOfImages, string requestId)
    {
        return null;
    }

    public ImageGenerationError HandleImageGenerationError(HttpStatusCode httpStatusCode, string responseJson)
    {
        return null;
    }
}