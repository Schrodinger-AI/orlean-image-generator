using System.Text.Json.Serialization;
using WebApi.Models;
using Attribute = WebApi.Models.Attribute;

namespace WebApi.ImageGeneration.Models 
{
    public class ImageGenerationAPIRequest
    {
        [JsonPropertyName("seed")]
        public string? Seed { get; set; }

        [JsonPropertyName("newAttributes")]
        public List<Attribute> NewTraits { get; set; }

        [JsonPropertyName("baseImage")]
        public ImageDescription BaseImage { get; set; }

        [JsonPropertyName("numImages")]
        public int NumberOfImages { get; set; } = 1;
    }

    public abstract class ImageGenerationAPIResponse { }

    public class ImageGenerationResponseOk : ImageGenerationAPIResponse
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }
    }

    public class ImageGenerationResponseNotOk : ImageGenerationAPIResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }
    }

    public class ImageQueryRequest
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }
    }

    public abstract class ImageQueryResponse { }

    public class ImageQueryResponseOk : ImageQueryResponse
    {
        [JsonPropertyName("images")]
        public List<ImageDescription> Images { get; set; }
    }

    public class ImageQueryResponseNotOk : ImageQueryResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }
    }

    public class InspectGeneratorRequest
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }
    }

    
}