namespace Schrodinger.Backend.Grains.UsageTracker.Types;

public class BlockedRequestInfo
{
    public string? BlockedReason { get; set; }
    public RequestAccountUsageInfo RequestAccountUsageInfo { get; set; }
}