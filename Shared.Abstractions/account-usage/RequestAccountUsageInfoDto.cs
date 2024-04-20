namespace Shared.Abstractions.AccountUsage;

using Shared.Abstractions.ApiKeys;

[GenerateSerializer]
public class RequestAccountUsageInfoDto
{
    [Id(0)]
    public string RequestId { get; set; } = "";
    [Id(1)]
    public string RequestTimestamp { get; set; } = "";
    [Id(2)]
    public string StartedTimestamp { get; set; } = "";
    [Id(3)]
    public string FailedTimestamp { get; set; } = "";
    [Id(4)]
    public string CompletedTimestamp { get; set; } = "";
    [Id(5)]
    public int Attempts { get; set; } = 0;
    [Id(6)]
    public ApiKeyDto? ApiKey { get; set; } = null;
    [Id(7)]
    public string ChildId { get; set; } = "";
}
