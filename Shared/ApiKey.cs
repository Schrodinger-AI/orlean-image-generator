namespace Shared;


[GenerateSerializer]
public class ApiKey
{
    [Id(0)]
    public string ApiKeyString { get; set; } = "";
    
    [Id(1)]
    public ImageGenerationServiceProvider ServiceProvider { get; set; } = ImageGenerationServiceProvider.DalleOpenAI;
    
    [Id(2)]
    public string Url { get; set; } = "";

    public ApiKey()
    {
    }
    
    public ApiKey(string apiKeyString, string strServiceProvider, string url)
    {
        ApiKeyString = apiKeyString;
        ServiceProvider = GetServiceProvider(strServiceProvider);
        Url = url;
    }
    
    public string GetConcatApiKeyString()
    {
        return $"{ServiceProvider}_{ApiKeyString}";
    }
    
    private static ImageGenerationServiceProvider GetServiceProvider(string serviceProvider)
    {
        return serviceProvider switch
        {
            "DalleOpenAI" => ImageGenerationServiceProvider.DalleOpenAI,
            "AzureOpenAI" => ImageGenerationServiceProvider.AzureOpenAI,
            _ => throw new ArgumentOutOfRangeException(nameof(serviceProvider), serviceProvider, null)
        };
    }
}