namespace Shared;

[GenerateSerializer]
public class RequestStatesDto
{
    [Id(0)]
    public Dictionary<string, RequestAccountUsageInfoDto> PendingImageGenerationRequests { get; set; } = new();
    [Id(1)]
    public Dictionary<string, RequestAccountUsageInfoDto> StartedImageGenerationRequests { get; set; } = new();
    [Id(2)]
    public Dictionary<string, RequestAccountUsageInfoDto> FailedImageGenerationRequests { get; set; } = new();
    [Id(3)]
    public Dictionary<string, RequestAccountUsageInfoDto> CompletedImageGenerationRequests { get; set; } = new();
}

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
    public ApiKey? ApiKey { get; set; } = null;
    [Id(7)]
    public string ChildId { get; set; } = "";
}

[GenerateSerializer]
public class BlockedRequestInfoDto
{
    [Id(0)]
    public string? BlockedReason { get; set; } = "";
    [Id(1)]
    public RequestAccountUsageInfoDto RequestInfo { get; set; }
}