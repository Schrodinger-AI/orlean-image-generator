using Grains.types;

namespace Grains.usage_tracker;

public interface IImageGenerationRequestStatusReceiver : Orleans.IGrainWithStringKey
{
    Task ReportFailedImageGenerationRequestAsync(RequestStatus requestStatus);
    Task ReportCompletedImageGenerationRequestAsync(RequestStatus requestStatus);
}