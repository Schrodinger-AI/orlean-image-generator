using System.Collections.Immutable;
using Orleans;

namespace Schrodinger.Backend.Abstractions.Interfaces.Config;

public interface IConfiguratorGrain : ISchrodingerGrain, IGrainWithStringKey
{
    Task<ImmutableSortedSet<string>> GetAllConfigIdsAsync();
    Task<string> GetCurrentConfigIdAsync();
    Task AddConfigIdAsync(string configId);
    Task SetCurrentConfigIdAsync(string configId);
}