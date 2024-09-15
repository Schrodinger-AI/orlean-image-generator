namespace Schrodinger.Backend.Grains.Errors;

using Schrodinger.Backend.Abstractions.Constants;

public class ImageGenerationException : Exception
{
    public ImageGenerationErrorCode ErrorCode { get; }

    public override string Message { get; }

    public ImageGenerationException(ImageGenerationErrorCode errorCode)
    {
        ErrorCode = errorCode;
    }

    public ImageGenerationException(
        ImageGenerationErrorCode errorCode,
        string message
    )
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public ImageGenerationException(
        ImageGenerationErrorCode errorCode,
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
