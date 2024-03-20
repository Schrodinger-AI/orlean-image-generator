using Shared;

namespace Grains.types;

[GenerateSerializer]
public class RequestAccountUsageInfo
{
    [Id(0)]
    public string RequestId { get; set; } = "";
    [Id(1)]
    public long RequestTimestamp { get; set; } = 0;
    [Id(2)]
    public long StartedTimestamp { get; set; } = 0;
    [Id(3)]
    public long FailedTimestamp { get; set; } = 0;
    [Id(4)]
    public long CompletedTimestamp { get; set; } = 0;
    [Id(5)]
    public int Attempts { get; set; } = 0;
    [Id(6)]
    public ApiKey? ApiKey { get; set; } = null;
    [Id(7)]
    public string ChildId { get; set; } = "";
}