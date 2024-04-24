using Orleans.Runtime;
using Schrodinger.Backend.Abstractions.Interfaces;

namespace Schrodinger.Backend.SiloHost.startup;

public class SchedulerGrainStartupTask : IStartupTask
{
    private readonly IGrainFactory _grainFactory;

    public SchedulerGrainStartupTask(IGrainFactory grainFactory) =>
        _grainFactory = grainFactory;

    public async Task Execute(CancellationToken cancellationToken)
    {
        var grain = _grainFactory.GetGrain<ISchedulerGrain>("SchedulerGrain");
        //forcefully activate the grain
        await grain.Activate();
    }
}