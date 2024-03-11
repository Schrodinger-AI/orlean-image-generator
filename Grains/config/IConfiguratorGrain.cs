using System.Collections.Immutable;
using Orleans;
using Shared;

namespace UnitTests.Grains;

public interface IConfiguratorGrain : ISchrodingerGrain, IGrainWithStringKey
{
    Task<ImmutableSortedSet<string>> GetAllConfigIdsAsync();
    Task<string> GetCurrentConfigIdAsync();
    Task AddConfigIdAsync(string configId);
    Task SetCurrentConfigIdAsync(string configId);
}