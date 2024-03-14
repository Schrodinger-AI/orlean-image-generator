using System.Net;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Shared;

namespace Grains;
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

public class DalleResponse
{
    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("data")]
    public List<DalleData> Data { get; set; }
    
    [JsonPropertyName("error")]
    public DalleError Error { get; set; }
}

public class DalleError
{
    [JsonPropertyName("httpStatusCode")]
    public HttpStatusCode HttpStatusCode { get; set; }
    
    [JsonPropertyName("code")] 
    public string Code { get; set; }
    
    [JsonPropertyName("dalleErrorCode")]
    public DalleErrorCode DalleErrorCode { get; set; }

    [JsonPropertyName("message")] 
    public string Message { get; set; }

    [JsonPropertyName("param")] 
    public string Param { get; set; }

    [JsonPropertyName("type")] 
    public string Type { get; set; }
    
    [JsonPropertyName("error")]
    public DalleError Error { get; set; }
}

public class DalleData
{
    [JsonPropertyName("revised_prompt")]
    public string RevisedPrompt { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}