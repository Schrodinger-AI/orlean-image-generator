namespace Shared;

public enum ApiKeyStatus
{
    Active,
    OnHold
}
    
public class ApiKeyUsageInfo
{
    public const long RATE_LIMIT_WAIT = 120; // 2 minutes
    public const long INVALID_API_KEY_WAIT = 86400; //1 day
    
    public ApiKey ApiKey { get; set; }
    public long LastUsedTimestamp { get; set; }
    public long Attempts { get; set; }
    public ApiKeyStatus Status { get; set; }
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