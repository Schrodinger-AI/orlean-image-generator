namespace Grains.types;

[GenerateSerializer]
public class BlockedRequestInfo
{
    [Id(0)]
    public string? BlockedReason { get; set; }
    [Id(1)]
    public RequestAccountUsageInfo RequestAccountUsageInfo { get; set; }
}