namespace Shared;

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

[GenerateSerializer]
public class ApiKeyEntryDto
{
    [Id(0)]
    public ApiKeyDto ApiKey { get; set; }
    [Id(1)]
    public string Email { get; set; }
    [Id(2)]
    public int Tier { get; set; }
    [Id(3)]
    public int MaxQuota { get; set; }
}

[GenerateSerializer]
public class AddApiKeysResponseDto
{
    [Id(0)]
    public bool IsSuccessful { get; set; }
    
    [Id(1)]
    public List<ApiKeyDto>? ValidApiKeys { get; set; }
    
    [Id(2)]
    public string? Error { get; set; }
    
    [Id(3)]
    public List<ApiKeyDto>? DuplicateApiKeys { get; set; }
    
}