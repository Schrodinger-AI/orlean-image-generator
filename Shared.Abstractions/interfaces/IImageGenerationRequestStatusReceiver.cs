
using Shared.Abstractions.UsageTracker;

namespace Shared.Abstractions.Interfaces;

public interface IImageGenerationRequestStatusReceiver : Orleans.IGrainWithStringKey
{
    Task ReportFailedImageGenerationRequestAsync(RequestStatus requestStatus);
    Task ReportCompletedImageGenerationRequestAsync(RequestStatus requestStatus);
    Task ReportBlockedImageGenerationRequestAsync(RequestStatus requestStatus);

}