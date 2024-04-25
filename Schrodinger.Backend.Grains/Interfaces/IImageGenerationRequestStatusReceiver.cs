
using Schrodinger.Backend.Abstractions.Types.UsageTracker;

namespace Schrodinger.Backend.Grains.Interfaces;

public interface IImageGenerationRequestStatusReceiver : Orleans.IGrainWithStringKey
{
    Task ReportFailedImageGenerationRequestAsync(RequestStatus requestStatus);
    Task ReportCompletedImageGenerationRequestAsync(RequestStatus requestStatus);
    Task ReportBlockedImageGenerationRequestAsync(RequestStatus requestStatus);

}