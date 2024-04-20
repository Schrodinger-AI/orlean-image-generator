namespace Shared.Abstractions.AccountUsage;

[GenerateSerializer]
public class BlockedRequestInfoDto
{
    [Id(0)]
    public string? BlockedReason { get; set; } = "";
    [Id(1)]
    public RequestAccountUsageInfoDto RequestInfo { get; set; }
}