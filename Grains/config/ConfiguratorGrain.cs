using System.Collections.Immutable;
using Shared.Abstractions.Interfaces;
using Orleans.Runtime;

namespace Grains.Config;

public class ConfiguratorGrain : Grain, IConfiguratorGrain
{
    private readonly IPersistentState<ConfiguratorGrainState> _configuratorGrainState;

    public ConfiguratorGrain(
        [PersistentState("configuratorGrainState", "MySqlSchrodingerImageStore")]
        IPersistentState<ConfiguratorGrainState> configuratorGrainState)
    {
        _configuratorGrainState = configuratorGrainState;
    }

    public async Task<ImmutableSortedSet<string>> GetAllConfigIdsAsync()
    {
        return await Task.FromResult(_configuratorGrainState.State.AllConfigIds.ToImmutableSortedSet());
    }

    public async Task<string> GetCurrentConfigIdAsync()
    {
        return await Task.FromResult(_configuratorGrainState.State.CurrentConfigId);
    }

    public async Task AddConfigIdAsync(string configId)
    {
        _configuratorGrainState.State.AllConfigIds.Add(configId);
        await _configuratorGrainState.WriteStateAsync();
    }

    public async Task SetCurrentConfigIdAsync(string configId)
    {
        _configuratorGrainState.State.CurrentConfigId = configId;
        await _configuratorGrainState.WriteStateAsync();
    }
}