namespace Shared;

public class DalleException : Exception
{
    public DalleErrorCode ErrorCode { get; }

    public override string Message { get; }

    public DalleException(DalleErrorCode errorCode)
    {
        ErrorCode = errorCode;
    }

    public DalleException(DalleErrorCode errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public DalleException(DalleErrorCode errorCode, string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}