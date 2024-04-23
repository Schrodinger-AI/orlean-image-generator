
using Schrodinger.Backend.Abstractions.UsageTracker;

namespace Schrodinger.Backend.Abstractions.Interfaces;

public interface IImageGenerationRequestStatusReceiver : Orleans.IGrainWithStringKey
{
    Task ReportFailedImageGenerationRequestAsync(RequestStatus requestStatus);
    Task ReportCompletedImageGenerationRequestAsync(RequestStatus requestStatus);
    Task ReportBlockedImageGenerationRequestAsync(RequestStatus requestStatus);

}