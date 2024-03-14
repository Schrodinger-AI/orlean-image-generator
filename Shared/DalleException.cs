namespace Shared;

public class DalleException : Exception
{
    public DalleErrorCodes ErrorCode { get; }

    public override string Message { get; }

    public DalleException(DalleErrorCodes errorCode)
    {
        ErrorCode = errorCode;
    }

    public DalleException(DalleErrorCodes errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public DalleException(DalleErrorCodes errorCode, string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}