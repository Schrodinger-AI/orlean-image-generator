using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Grains;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public abstract class WatermarkApiSchema
{
    public class WatermarkRequest
    {
        [JsonPropertyName("sourceImage")]
        public string SourceImage { get; set; }

        [JsonPropertyName("watermark")]
        public WatermarkDetails Watermark { get; set; }
    }

    public class WatermarkDetails
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class LogDetails
    {
        [JsonPropertyName("timestamp")] public string Timestamp { get; set; }
        [JsonPropertyName("request_method")] public string RequestMethod { get; set; }
        [JsonPropertyName("request_body")] public WatermarkRequest RequestBody { get; set; }
        [JsonPropertyName("request_url")] public string RequestUrl { get; set; }
        [JsonPropertyName("status_code")] public int StatusCode { get; set; }
        [JsonPropertyName("response")] public string Response { get; set; }
    }
}