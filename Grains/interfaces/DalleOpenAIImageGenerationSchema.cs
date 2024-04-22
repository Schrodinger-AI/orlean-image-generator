using System.Net;
using System.Text.Json.Serialization;
using Shared.Abstractions.Constants;

namespace Grains.DalleOpenAI;

public class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}

public class Choice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public Message Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}

public class Result
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("object")]
    public string ObjectType { get; set; }

    [JsonPropertyName("created")]
    public int Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; }
}

public class DalleOpenAIImageGenerationWrappedError
{
    [JsonPropertyName("error")]
    public DalleOpenAIImageGenerationError Error { get; set; }
}

public class DalleOpenAIImageGenerationError
{
    [JsonPropertyName("httpStatusCode")]
    public HttpStatusCode HttpStatusCode { get; set; }
    
    [JsonPropertyName("code")] 
    public string Code { get; set; }
    
    [JsonPropertyName("imageGenerationErrorCode")]
    public ImageGenerationErrorCode ImageGenerationErrorCode { get; set; }

    [JsonPropertyName("message")] 
    public string Message { get; set; }

    [JsonPropertyName("param")] 
    public string Param { get; set; }

    [JsonPropertyName("type")] 
    public string Type { get; set; }
}


