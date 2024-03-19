namespace Shared.Prompter;

public class PromptGenerationRequest
{
    public string Identifier { get; set; }
        
    public List<Attribute> NewAttributes { get; set; }

    public ImageDescription? BaseImage { get; set; }
}
