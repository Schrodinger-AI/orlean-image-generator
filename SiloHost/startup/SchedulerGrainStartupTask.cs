using Grains.usage_tracker;
using Orleans;
using Orleans.Runtime;

namespace SiloHost.startup;

public class SchedulerGrainStartupTask : IStartupTask
{
    private readonly IGrainFactory _grainFactory;

    public SchedulerGrainStartupTask(IGrainFactory grainFactory) =>
        _grainFactory = grainFactory;

    public async Task Execute(CancellationToken cancellationToken)
    {
        var grain = _grainFactory.GetGrain<ISchedulerGrain>("SchedulerGrain");
        //call a random method to force the grain to activate
        await grain.IsOverloaded();
    }
}