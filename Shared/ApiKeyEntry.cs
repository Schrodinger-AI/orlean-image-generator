namespace Shared;

public class ApiKeyDto
{
    public string ApiKeyString { get; set; } = "";
    public string ServiceProvider { get; set; } = "";
}

public class ApiKeyEntry
{
    public ApiKeyDto ApiKey { get; set; }
    public string Email { get; set; }
    public int Tier { get; set; }
    public int MaxQuota { get; set; }
}