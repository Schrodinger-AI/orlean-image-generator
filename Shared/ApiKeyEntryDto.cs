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

[Serializable]
[GenerateSerializer]
public class AddApiKeysResponse
{
    [Id(0)]
    public bool IsSuccessful { get; set; }
    
    [Id(1)]
    public List<ApiKey>? ValidApiKeys { get; set; }
    
    [Id(2)]
    public string? Error { get; set; }
    
    [Id(3)]
    public List<string>? DuplicateApiKeys { get; set; }
    
}