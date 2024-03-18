using System.Net;

namespace Grains;

public interface IImageGenerator
{
    Task<ImageGenerationResponse> RunImageGenerationAsync(string prompt, string apikey, int numberOfImages, string requestId);
    ImageGenerationError HandleImageGenerationError(HttpStatusCode httpStatusCode, string responseJson);
}