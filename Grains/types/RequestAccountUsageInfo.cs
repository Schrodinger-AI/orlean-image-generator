namespace Grains.types;

public class RequestAccountUsageInfo
{
    public string RequestId { get; set; }
    public long RequestTimestamp { get; set; }
    public long StartedTimestamp { get; set; }
    public long FailedTimestamp { get; set; }
    public long CompletedTimestamp { get; set; }
    public int Attempts { get; set; }
    public string AccountId { get; set; }
}