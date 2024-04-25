namespace Schrodinger.Backend.Abstractions.Interfaces;

using Orleans;

/// <summary>
/// Represents a Schrodinger grain.
/// </summary>
public interface ISchrodingerGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Activates the grain.
    /// </summary>
    Task Activate()
    {
        return Task.CompletedTask;
    }
}