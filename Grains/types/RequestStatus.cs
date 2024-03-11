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

    /// <summary>
    /// Email address will be used as the account identifier
    /// </summary>
    public string AccountId { get; set; }

    public RequestStatusEnum Status { get; set; }
    public string Message { get; set; }
}