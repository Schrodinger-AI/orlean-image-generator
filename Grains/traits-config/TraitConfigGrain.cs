
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;

namespace Grains;

public class TraitConfigGrain : Grain, ITraitConfigGrain
{
    private readonly IPersistentState<TraitState> _traitState;

    public TraitConfigGrain([PersistentState("traitState", "MySqlSchrodingerImageStore")] IPersistentState<TraitState> traitState)
    {
        _traitState = traitState;
    }

    public override async Task OnActivateAsync()
    {

        // Check if the data has already been loaded
        if (_traitState.State.Traits == null || _traitState.State.Traits.Count == 0)
        {
            var jsonFilePath = "traits.json";

            if (File.Exists(jsonFilePath))
            {
                var jsonData = await File.ReadAllTextAsync(jsonFilePath);
                var traitEntries = JsonConvert.DeserializeObject<List<TraitEntry>>(jsonData);

                if (_traitState.State.Traits == null)
                {
                    _traitState.State.Traits = [];
                }

                foreach (var entry in traitEntries)
                {
                    _traitState.State.Traits[entry.Name] = entry;
                }

                await _traitState.WriteStateAsync();
            }
        }

        await base.OnActivateAsync();
    }

    public Task<Dictionary<string, TraitEntry>> GetAllTraits()
    {
        return Task.FromResult(_traitState.State.Traits);
    }

    public Task<TraitEntry> GetTraitByName(string traitName)
    {
        if (!_traitState.State.Traits.TryGetValue(traitName, out var traitEntry))
        {
            throw new KeyNotFoundException($"Trait with name {traitName} not found.");
        }

        return Task.FromResult(traitEntry);
    }

    public async Task AddTrait(string traitName, TraitEntry traitEntry)
    {
        if (_traitState.State.Traits.ContainsKey(traitName))
        {
            throw new ArgumentException($"Trait with name {traitName} already exists.");
        }

        _traitState.State.Traits[traitName] = traitEntry;
        await _traitState.WriteStateAsync();
    }

    public async Task DeleteTrait(string traitName)
    {
        if (!_traitState.State.Traits.ContainsKey(traitName))
        {
            throw new ArgumentException($"Trait with name {traitName} does not exist.");
        }

        _traitState.State.Traits.Remove(traitName);
        await _traitState.WriteStateAsync();
    }
}
