namespace Shared;

public enum ApiKeyStatus
{
    Active,
    OnHold
}
    
public class ApiKeyUsageInfo
{
    public string ApiKey { get; set; }
    public long LastUsedTimestamp { get; set; }
    public long Attempts { get; set; }
    public ApiKeyStatus Status { get; set; }
}