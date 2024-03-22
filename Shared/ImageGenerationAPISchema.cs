using System.Text.Json.Serialization;

namespace Shared
{
    [GenerateSerializer]
    public class Attribute
    {
        [JsonPropertyName("traitType")]
        [Id(0)]
        public string TraitType { get; set; }

        [JsonPropertyName("value")]
        [Id(1)]
        public string Value { get; set; }
    }

    [GenerateSerializer]
    public class ImageDescription
    {
        [JsonPropertyName("image")]
        [Id(0)]
        public string? Image { get; set; } = null;

        [JsonPropertyName("attributes")]
        [Id(1)]
        public List<Attribute> Attributes { get; set; } = [];

        [JsonPropertyName("extraData")]
        [Id(2)]
        public string? ExtraData { get; set; } = null;
    }

    [GenerateSerializer]
    public class PromptGenerationRequest
    {
        [JsonPropertyName("identifier")]
        [Id(0)]
        public string Identifier { get; set; }
        
        [JsonPropertyName("newAttributes")]
        [Id(1)]
        public List<Attribute> NewAttributes { get; set; }

        [JsonPropertyName("baseImage")]
        [Id(2)]
        public ImageDescription? BaseImage { get; set; }
    }
    
    [GenerateSerializer]
    public class SetPromptConfigRequest
    {
        [JsonPropertyName("identifier")]
        [Id(0)]
        public string Identifier { get; set; }

        [JsonPropertyName("configText")]
        [Id(1)]
        public string ConfigText { get; set; }

        [JsonPropertyName("scriptContent")]
        [Id(2)]
        public string ScriptContent { get; set; }
        
        [JsonPropertyName("validationTestCase")]
        [Id(3)]
        public string ValidationTestCase { get; set; }
    }
    
    [GenerateSerializer]
    public class GetPromptConfigRequest
    {
        [JsonPropertyName("identifier")]
        [Id(0)]
        public string Identifier { get; set; }
    }
    
    [GenerateSerializer]
    public class SwitchIdentifierRequest
    {
        [JsonPropertyName("identifier")]
        [Id(0)]
        public string Identifier { get; set; }
    }
    
    public abstract class PrompterResponse {}

    [GenerateSerializer]
    public class PrompterResponseOk : PrompterResponse
    {
        [JsonPropertyName("result")]
        [Id(0)]
        public object Result { get; set; }
    }

    [GenerateSerializer]
    public class PrompterResponseNotOk : PrompterResponse
    {
        [JsonPropertyName("error")]
        [Id(0)]
        public string Error { get; set; }
    }
    
    [GenerateSerializer]
    public class ImageGenerationRequest
    {
        [JsonPropertyName("seed")]
        [Id(0)]
        public string? Seed { get; set; }

        [JsonPropertyName("newAttributes")]
        [Id(1)]
        public List<Attribute> NewTraits { get; set; }

        [JsonPropertyName("baseImage")]
        [Id(2)]
        public ImageDescription BaseImage { get; set; }

        [JsonPropertyName("numImages")]
        [Id(3)]
        public int NumberOfImages { get; set; } = 1;
    }

    public abstract class ImageGenerationAPIResponse { }

    [GenerateSerializer]
    public class ImageGenerationResponseOk : ImageGenerationAPIResponse
    {
        [JsonPropertyName("requestId")]
        [Id(0)]
        public string RequestId { get; set; }
    }

    [GenerateSerializer]
    public class ImageGenerationResponseNotOk : ImageGenerationAPIResponse
    {
        [JsonPropertyName("error")]
        [Id(0)]
        public string Error { get; set; }
    }

    [GenerateSerializer]
    public class ImageQueryRequest
    {
        [JsonPropertyName("requestId")]
        [Id(0)]
        public string RequestId { get; set; }
    }

    public abstract class ImageQueryResponse { }

    [GenerateSerializer]
    public class ImageQueryResponseOk : ImageQueryResponse
    {
        [JsonPropertyName("images")]
        [Id(0)]
        public List<ImageDescription> Images { get; set; }
    }

    [GenerateSerializer]
    public class ImageQueryResponseNotOk : ImageQueryResponse
    {
        [JsonPropertyName("error")]
        [Id(0)]
        public string Error { get; set; }
        
        [JsonPropertyName("errorCode")]
        [Id(1)]
        public string ErrorCode { get; set; }
    }

    [GenerateSerializer]
    public class InspectGeneratorRequest
    {
        [JsonPropertyName("requestId")]
        [Id(0)]
        public string RequestId { get; set; }
    }
    
    public class PromptUpdateRequest
    {
        [JsonPropertyName("multiImageRequestId")]
        public string MultiImageRequestId { get; set; }
        
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = "";
        
        [JsonPropertyName("prompt")]
        public string? Prompt { get; set; } = null;
        
        [JsonPropertyName("attributes")]
        public List<Attribute> Attributes { get; set; } = [];
    }

    
}