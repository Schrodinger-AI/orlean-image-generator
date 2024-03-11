using System.Text.Json.Serialization;

namespace Shared
{
    public class Trait
    {
        [JsonPropertyName("traitType")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class ImageDescription
    {
        [JsonPropertyName("image")]
        public string? Image { get; set; } = null;

        [JsonPropertyName("attributes")]
        public List<Trait> Traits { get; set; } = [];

        [JsonPropertyName("extraData")]
        public string? ExtraData { get; set; } = null;
    }

    public class PromptGenerationRequest
    {
        [JsonPropertyName("newAttributes")]
        public List<Trait> NewTraits { get; set; }

        [JsonPropertyName("baseImage")]
        public ImageDescription BaseImage { get; set; }
    }
    
    public class SetPromptStateRequest
    {
        [JsonPropertyName("configText")]
        public ConfigText ConfigText { get; set; }

        [JsonPropertyName("scriptContent")]
        public string ScriptContent { get; set; }
    }
    
    public abstract class PromptCreatorResponse {}

    public class PromptCreatorResponseOk : PromptCreatorResponse
    {
        [JsonPropertyName("result")]
        public object Result { get; set; }
    }

    public class PromptCreatorResponseNotOk : PromptCreatorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }
    }
    
    public class ImageGenerationRequest
    {
        [JsonPropertyName("seed")]
        public string? Seed { get; set; }

        [JsonPropertyName("newAttributes")]
        public List<Trait> NewTraits { get; set; }

        [JsonPropertyName("baseImage")]
        public ImageDescription BaseImage { get; set; }

        [JsonPropertyName("numImages")]
        public int NumberOfImages { get; set; } = 1;
    }

    public abstract class ImageGenerationResponse { }

    public class ImageGenerationResponseOk : ImageGenerationResponse
    {
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }
    }

    public class ImageGenerationResponseNotOk : ImageGenerationResponse
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

}