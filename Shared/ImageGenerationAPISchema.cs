using System.Text.Json.Serialization;

namespace Shared
{
    public class Trait
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class ImageDescription
    {
        [JsonPropertyName("image")]
        public string? Image { get; set; } = null;

        [JsonPropertyName("traits")]
        public List<Trait> Traits { get; set; } = [];

        [JsonPropertyName("extraData")]
        public string? ExtraData { get; set; } = null;
    }

    public class PromptGenerationRequest
    {
        [JsonPropertyName("newTraits")]
        public List<Trait> NewTraits { get; set; }

        [JsonPropertyName("baseImage")]
        public ImageDescription BaseImage { get; set; }
        
        [JsonPropertyName("configText")]
        public ConfigText ConfigText { get; set; }

        [JsonPropertyName("scriptContent")]
        public string ScriptContent { get; set; }
    }
    
    public abstract class PromptGenerationResponse {}

    public class PromptGenerationResponseOk : PromptGenerationResponse
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }
    }

    public class PromptGenerationResponseNotOk : PromptGenerationResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }
    }

    public class ConfigText
    {
        public string prefix { get; set; }
    }

    public class ImageGenerationRequest
    {
        [JsonPropertyName("seed")]
        public string? Seed { get; set; }

        [JsonPropertyName("newTraits")]
        public List<Trait> NewTraits { get; set; }

        [JsonPropertyName("baseImage")]
        public ImageDescription BaseImage { get; set; }
    }

    public abstract class ImageGenerationResponse {}

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

        public abstract class ImageQueryResponse {}

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