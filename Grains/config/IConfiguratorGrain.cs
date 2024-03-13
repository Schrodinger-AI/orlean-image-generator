using System.Collections.Immutable;
using Orleans;

public interface IConfiguratorGrain : Grains.ISchrodingerGrain, IGrainWithStringKey
{
    Task<ImmutableSortedSet<string>> GetAllConfigIdsAsync();
    Task<string> GetCurrentConfigIdAsync();
    Task AddConfigIdAsync(string configId);
    Task SetCurrentConfigIdAsync(string configId);
}