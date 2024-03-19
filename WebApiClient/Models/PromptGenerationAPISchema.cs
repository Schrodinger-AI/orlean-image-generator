using System.Text.Json.Serialization;
using WebApi.Models;
using Attribute = WebApi.Models.Attribute;

namespace WebApi.Prompter.Models 
{
    public class PromptGenerationAPIRequest
    {
        [JsonPropertyName("identifier")]
        public string Identifier { get; set; }
        
        [JsonPropertyName("newAttributes")]
        public List<Attribute> NewAttributes { get; set; }

        [JsonPropertyName("baseImage")]
        public ImageDescription? BaseImage { get; set; }
    }
    
    public class SetPromptConfigRequest
    {
        [JsonPropertyName("identifier")]
        public string Identifier { get; set; }

        [JsonPropertyName("configText")]
        public string ConfigText { get; set; }

        [JsonPropertyName("scriptContent")]
        public string ScriptContent { get; set; }
        
        [JsonPropertyName("validationTestCase")]
        public string ValidationTestCase { get; set; }
    }
    
    public class GetPromptConfigRequest
    {
        [JsonPropertyName("identifier")]
        public string Identifier { get; set; }
    }
    
    public class SwitchIdentifierRequest
    {
        [JsonPropertyName("identifier")]
        public string Identifier { get; set; }
    }
    
    public abstract class PrompterAPIResponse {}

    public class PrompterResponseOk : PrompterAPIResponse
    {
        [JsonPropertyName("result")]
        public object Result { get; set; }
    }

    public class PrompterResponseNotOk : PrompterAPIResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }
    }

    
}