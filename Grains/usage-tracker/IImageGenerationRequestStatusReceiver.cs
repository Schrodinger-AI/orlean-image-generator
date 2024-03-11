namespace Grains.usage_tracker;

public interface IImageGenerationRequestStatusReceiver
{
    Task ReportFailedImageGenerationRequestAsync(string requestId);
    Task ReportCompletedImageGenerationRequestAsync(string requestId);
}