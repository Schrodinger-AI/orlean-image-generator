namespace Shared;

public enum ApiKeyStatus
{
    Active,
    OnHold
}
    
[GenerateSerializer]
public class ApiKeyUsageInfo
{
    public const long RATE_LIMIT_WAIT = 120; // 2 minutes
    public const long INVALID_API_KEY_WAIT = 86400; //1 day

    [Id(0)]
    public string ApiKey { get; set; }
    [Id(1)]
    public long LastUsedTimestamp { get; set; }
    [Id(2)]
    public long Attempts { get; set; }
    [Id(3)]
    public ApiKeyStatus Status { get; set; }
    [Id(4)]
    public ImageGenerationErrorCode? ErrorCode { get; set; }
    
    public long GetReactivationTimestamp()
    {
        return ErrorCode switch
        {
            ImageGenerationErrorCode.rate_limit_reached => LastUsedTimestamp + RATE_LIMIT_WAIT,
            ImageGenerationErrorCode.invalid_api_key => LastUsedTimestamp + INVALID_API_KEY_WAIT,
            _ => LastUsedTimestamp + (long)Math.Min(Math.Pow(3, Attempts), 27.0)
        };
    }
}