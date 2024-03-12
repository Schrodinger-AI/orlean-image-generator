using System.Collections.Immutable;
using Orleans;
using Shared;

namespace Grains;

public interface IConfiguratorGrain : ISchrodingerGrain, IGrainWithStringKey
{
    Task<ImmutableSortedSet<string>> GetAllConfigIdsAsync();
    Task<string> GetCurrentConfigIdAsync();
    Task AddConfigIdAsync(string configId);
    Task SetCurrentConfigIdAsync(string configId);
}