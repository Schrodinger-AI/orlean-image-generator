using System.Net;
using System.Text.Json.Serialization;
using Shared;

namespace Grains;


public class ImageGenerationResponse
{
    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("data")]
    public List<ImageGenerationData> Data { get; set; }
    
    [JsonPropertyName("error")]
    public ImageGenerationError Error { get; set; }
}

public class ImageGenerationData
{
    [JsonPropertyName("revised_prompt")]
    public string RevisedPrompt { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}

public class ImageGenerationError
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