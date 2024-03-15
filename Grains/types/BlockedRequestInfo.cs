namespace Grains.types;

public class BlockedRequestInfo
{
    public string? BlockedReason { get; set; }
    public RequestAccountUsageInfo RequestAccountUsageInfo { get; set; }
}