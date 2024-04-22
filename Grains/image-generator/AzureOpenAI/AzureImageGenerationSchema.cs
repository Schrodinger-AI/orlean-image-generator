using System.Text.Json.Serialization;
namespace Grains.image_generator.AzureOpenAI;

public class ContentFilterResultDetail
{
    [JsonPropertyName("filtered")]
    public bool Filtered { get; set; }

    [JsonPropertyName("severity")]
    public string Severity { get; set; }
}

public class ContentFilterResult
{
    [JsonPropertyName("hate")]
    public ContentFilterResultDetail Hate { get; set; }

    [JsonPropertyName("self-harm")]
    public ContentFilterResultDetail SelfHarm { get; set; }

    [JsonPropertyName("sexual")]
    public ContentFilterResultDetail Sexual { get; set; }

    [JsonPropertyName("violence")]
    public ContentFilterResultDetail Violence { get; set; }
}

public class InnerError
{
    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("content_filter_result")]
    public ContentFilterResult ContentFilterResult { get; set; }
}

public class AzureError
{
    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("param")]
    public string Param { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("innererror")]
    public InnerError InnerError { get; set; }
}

public class AzureErrorWrapper
{
    [JsonPropertyName("error")]
    public AzureError Error { get; set; }
}