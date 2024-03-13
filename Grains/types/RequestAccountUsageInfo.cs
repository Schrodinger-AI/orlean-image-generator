namespace Grains.types;

public class RequestAccountUsageInfo
{
    public string RequestId { get; set; } = "";
    public long RequestTimestamp { get; set; } = 0;
    public long StartedTimestamp { get; set; } = 0;
    public long FailedTimestamp { get; set; } = 0;
    public long CompletedTimestamp { get; set; } = 0;
    public int Attempts { get; set; } = 0;
    public string ApiKey { get; set; } = "";
    public string ChildId { get; set; } = "";
}