namespace Grains.Contracts
{

    public class PrompterConfig
    {
        public string ScriptContent { get; set; }
        public string ConfigText { get; set; }
        public string ValidationTestCase { get; set; }
        public bool ValidationOk { get; set; }
    }

    public class PromptGenerationRequest
    {
        public string Identifier { get; set; }

        public List<Attribute> NewAttributes { get; set; }

        public ImageDescription? BaseImage { get; set; }
    }

    public class Attribute
    {
        public string TraitType { get; set; }

        public string Value { get; set; }
    }
}