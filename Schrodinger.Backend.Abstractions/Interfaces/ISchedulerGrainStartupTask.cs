namespace Schrodinger.Backend.Abstractions.Interfaces;

using Orleans;

/// <summary>
/// Represents a ISchedulerGrainStartupTask grain.
/// </summary>
public interface ISchedulerGrainStartupTask : ISchrodingerGrain, Orleans.IGrainWithStringKey
{
    /// <summary>
    /// Activates the grain.
    /// </summary>
    Task Activate()
    {
        return Task.CompletedTask;
    }
}