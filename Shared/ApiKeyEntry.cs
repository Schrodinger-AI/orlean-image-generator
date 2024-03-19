namespace Shared;

[GenerateSerializer]
public class ApiKeyEntry
{
    [Id(0)]
    public string ApiKey { get; set; }
    
    [Id(1)]
    public string ServiceProvider { get; set; } = "";

    [Id(2)]
    public string Email { get; set; }
    [Id(3)]
    public int Tier { get; set; }
    [Id(4)]
    public int MaxQuota { get; set; }

    public ApiKeyEntry(string apiKey, string serviceProvider, string email, int tier, int maxQuota)
    {
        ApiKey = apiKey;
        ServiceProvider = serviceProvider;
        Email = email;
        Tier = tier;
        MaxQuota = maxQuota;
    }
}