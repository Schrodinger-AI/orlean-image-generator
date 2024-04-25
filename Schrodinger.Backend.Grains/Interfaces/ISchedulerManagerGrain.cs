using Schrodinger.Backend.Abstractions.Interfaces.ApiKeys;
using Schrodinger.Backend.Abstractions.Interfaces.Images;
using Schrodinger.Backend.Abstractions.Interfaces.StartupTask;

namespace Schrodinger.Backend.Grains.Interfaces;

public interface ISchedulerManagerGrain : IImageGenerationRequestManager, IAPIKeyGrain, ISchedulerGrainStartupTask, IImageGenerationRequestStatusReceiver
{
    Task AddImageGenerationRequest(string requestId, string childId, long requestTimestamp);

    Task FlushAsync();
    
    Task TickAsync();
}