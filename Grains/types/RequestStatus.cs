using Shared;

namespace Grains.types;

public enum RequestStatusEnum
{
    NotStarted,
    Started,
    Failed,
    Completed
}

[GenerateSerializer]
public class RequestStatus
{
    [Id(0)]
    public string RequestId { get; set; }
    [Id(1)]
    public RequestStatusEnum Status { get; set; }
    [Id(2)]
    public string Message { get; set; }
    [Id(3)]
    public long RequestTimestamp { get; set; }
    [Id(4)]
    public DalleErrorCode? ErrorCode { get; set; }
}