namespace Shared;

public class ApiKey
{
    public string ApiKeyString { get; set; } = "";
    public ImageGenerationServiceProvider ServiceProvider { get; set; } = ImageGenerationServiceProvider.DalleOpenAI;
}