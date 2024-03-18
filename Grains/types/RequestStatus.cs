using Shared;

namespace Grains.types;

public enum RequestStatusEnum
{
    NotStarted,
    Started,
    Failed,
    Completed
}

public class RequestStatus
{
    public string RequestId { get; set; }
    public RequestStatusEnum Status { get; set; }
    public string Message { get; set; }
    public long RequestTimestamp { get; set; }
    public ImageGenerationErrorCode? ErrorCode { get; set; }
}