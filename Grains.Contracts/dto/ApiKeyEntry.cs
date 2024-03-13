namespace Grains.Contracts;

public class ApiKeyEntry
{
    public string ApiKey { get; set; }
    public string Email { get; set; }
    public int Tier { get; set; }
    public int MaxQuota { get; set; }

    public ApiKeyEntry(string apiKey, string email, int tier, int maxQuota)
    {
        ApiKey = apiKey;
        Email = email;
        Tier = tier;
        MaxQuota = maxQuota;
    }
}