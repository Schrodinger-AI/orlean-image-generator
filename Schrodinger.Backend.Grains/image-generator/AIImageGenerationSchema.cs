using System.Text.Json.Serialization;
using Schrodinger.Backend.Abstractions.Constants;

namespace Schrodinger.Backend.Grains.image_generator;

public class AIImageGenerationResponse
{
    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("data")]
    public List<ImageGenerationData> Data { get; set; }

    [JsonPropertyName("error")]
    public AIImageGenerationError Error { get; set; }
}

public class ImageGenerationData
{
    [JsonPropertyName("revised_prompt")]
    public string RevisedPrompt { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}

public class AIImageGenerationError
{
    [JsonPropertyName("httpStatusCode")]
    public int HttpStatusCode { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("imageGenerationErrorCode")]
    public ImageGenerationErrorCode ImageGenerationErrorCode { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }
}
