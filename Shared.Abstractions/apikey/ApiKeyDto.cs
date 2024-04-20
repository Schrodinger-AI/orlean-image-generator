namespace Shared.Abstractions.ApiKeys;

[GenerateSerializer]
public class ApiKeyDto
{
    [Id(0)]
    public string ApiKeyString { get; set; } = "";
    [Id(1)]
    public string ServiceProvider { get; set; } = "";
    [Id(2)]
    public string Url { get; set; } = "";

    public ApiKeyDto()
    {
    }

    public ApiKeyDto(ApiKey apiKey)
    {
        ApiKeyString = apiKey.GetObfuscatedApiKeyString();
        ServiceProvider = apiKey.ServiceProvider.ToString();
        Url = apiKey.Url;
    }
}