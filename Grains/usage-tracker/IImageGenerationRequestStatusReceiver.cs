using Grains.types;

namespace Grains.usage_tracker;

public interface IImageGenerationRequestStatusReceiver
{
    Task ReportFailedImageGenerationRequestAsync(RequestStatus requestStatus);
    Task ReportCompletedImageGenerationRequestAsync(RequestStatus requestStatus);
}