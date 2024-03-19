namespace Shared;


[GenerateSerializer]
public class ApiKey
{
    [Id(0)]
    public string ApiKeyString { get; set; } = "";
    
    [Id(1)]
    public ImageGenerationServiceProvider ServiceProvider { get; set; } = ImageGenerationServiceProvider.DalleOpenAI;
}