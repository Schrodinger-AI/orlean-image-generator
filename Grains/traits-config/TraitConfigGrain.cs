
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Microsoft.Extensions.Options;
using Shared;

namespace Grains;

public class TraitConfigGrain : Grain, ITraitConfigGrain
{
    private readonly IPersistentState<TraitState> _traitState;

    private readonly string _filePath;

    public TraitConfigGrain(IOptions<TraitConfigOptions> options, [PersistentState("traitState", "MySqlSchrodingerImageStore")] IPersistentState<TraitState> traitState)
    {
        _traitState = traitState;
        _filePath = options.Value.FilePath;
    }

    public override async Task OnActivateAsync()
    {
        Console.WriteLine("traits from: " + _filePath);

        // Check if the data has already been loaded
        if (_traitState.State.Traits == null || _traitState.State.Traits.Count == 0)
        {

            Console.WriteLine("traits file exists result: " + File.Exists(_filePath));

            if (File.Exists(_filePath))
            {
                Console.WriteLine("Loading traits from " + _filePath);
                var jsonData = await File.ReadAllTextAsync(_filePath);
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

    public async Task UpdateTrait(string traitName, TraitEntry traitEntry)
    {
        if (!_traitState.State.Traits.ContainsKey(traitName))
        {
            throw new ArgumentException($"Trait with name {traitName} does not exist.");
        }

        _traitState.State.Traits[traitName] = traitEntry;
        await _traitState.WriteStateAsync();
    }

    public async Task ClearTraits()
    {
        _traitState.State.Traits.Clear();
        await _traitState.WriteStateAsync();
    }

    public async Task<Dictionary<string, TraitEntry>> GetTraitsMap(List<string> traitNames)
    {
        var traits = new Dictionary<string, TraitEntry>();

        foreach (var traitName in traitNames)
        {
            //get trait by name
            if (_traitState.State.Traits.TryGetValue(traitName, out var traitEntry))
            {
                traits[traitName] = traitEntry;
            } else {
                throw new KeyNotFoundException($"Trait with name {traitName} not found.");
            }
        }

        return traits;
    }
}
