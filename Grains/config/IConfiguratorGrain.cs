using System.Collections.Immutable;

namespace UnitTests.Grains;

public interface IConfiguratorGrain
{
    Task<ImmutableSortedSet<string>> GetAllConfigIdsAsync();
    Task<string> GetCurrentConfigIdAsync();
    Task SetCurrentConfigIdAsync(string configId);
}