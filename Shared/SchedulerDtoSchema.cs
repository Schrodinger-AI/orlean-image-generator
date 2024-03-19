namespace Shared;

public class RequestAccountUsageInfoDto
{
    public string RequestId { get; set; } = "";
    public string RequestTimestamp { get; set; } = "";
    public string StartedTimestamp { get; set; } = "";
    public string FailedTimestamp { get; set; } = "";
    public string CompletedTimestamp { get; set; } = "";
    public int Attempts { get; set; } = 0;
    public ApiKey? ApiKey { get; set; } = null;
    public string ChildId { get; set; } = "";
}

public class ApiKeyUsageInfoDto
{
    public ApiKey? ApiKey { get; set; } = null;
    public string ReactivationTimestamp { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ErrorCode { get; set; }
}
