namespace Schrodinger.Backend.Abstractions.Interfaces.StartupTask;

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