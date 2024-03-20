namespace Shared;


[GenerateSerializer]
public class ApiKey
{
    [Id(0)]
    public string ApiKeyString { get; set; } = "";
    
    [Id(1)]
    public ImageGenerationServiceProvider ServiceProvider { get; set; } = ImageGenerationServiceProvider.DalleOpenAI;

    public ApiKey(string concatApiKeyString)
    {
        var strings = concatApiKeyString.Split('_');
        ServiceProvider = GetServiceProvider(strings[0]);
        ApiKeyString = strings[1];
    }
    
    public ApiKey(string apiKeyString, ImageGenerationServiceProvider serviceProvider)
    {
        ApiKeyString = apiKeyString;
        ServiceProvider = serviceProvider;
    }
    
    public ApiKey(string apiKeyString, string strServiceProvider)
    {
        ApiKeyString = apiKeyString;
        ServiceProvider = GetServiceProvider(strServiceProvider);
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