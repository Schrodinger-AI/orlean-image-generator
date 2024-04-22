
using Orleans.Runtime;
using Shared.Abstractions.Interfaces;
using Shared.Abstractions.Trait;

namespace Grains.trait_config;

public class TraitConfigGrain : Grain, ITraitConfigGrain
{
    private readonly IPersistentState<TraitState> _traitState;

    public TraitConfigGrain([PersistentState("traitState", "MySqlSchrodingerImageStore")] IPersistentState<TraitState> traitState)
    {
        _traitState = traitState;
    }

    public Task<Dictionary<string, TraitEntry>> GetAllTraits()
    {
        return Task.FromResult(_traitState.State.Traits);
    }

    public Task<TraitEntry> GetTraitByName(string traitName)
    {
        if (!_traitState.State.Traits.TryGetValue(traitName, out var traitEntry))
        {
            throw new KeyNotFoundException($"Attribute with name {traitName} not found.");
        }

        return Task.FromResult(traitEntry);
    }

    public async Task<AddTraitAPIResponse> AddTrait(TraitEntry traitEntry)
    {
        try
        {
            if (_traitState.State.Traits.ContainsKey(traitEntry.Name))
            {
                throw new ArgumentException($"Attribute with name {traitEntry.Name} already exists.");
            }

            _traitState.State.Traits[traitEntry.Name] = traitEntry;
            await _traitState.WriteStateAsync();

            return new AddTraitResponseOk(traitEntry)
            {
                TotalTraitsCount = GetTraitsCount()
            };
        }
        catch (Exception ex)
        {
            return new AddTraitResponseNotOk(ex.Message);
        }
    }

    public async Task<AddTraitsAPIResponse> AddTraits(List<TraitEntry> traitEntries)
    {
        try
        {
            foreach (var entry in traitEntries)
            {
                if (!_traitState.State.Traits.ContainsKey(entry.Name))
                {
                    _traitState.State.Traits[entry.Name] = entry;
                }
            }

            await _traitState.WriteStateAsync();

            List<string> traitNames = traitEntries.Select(trait => trait.Name).ToList();

            Dictionary<string, TraitEntry> traitMap = await GetTraitsMap(traitNames);

            // return the list of traits entered to the memory and persisted to db
            return new AddTraitsResponseOk(traitMap)
            {
                TotalTraitsCount = GetTraitsCount()
            };
        }
        catch (Exception ex)
        {
            return new AddTraitsResponseNotOk(ex.Message);
        }
    }

    public async Task<DeleteTraitAPIResponse> DeleteTrait(string traitName)
    {
        try
        {
            if (!_traitState.State.Traits.ContainsKey(traitName))
            {
                throw new ArgumentException($"Attribute with name {traitName} does not exist.");
            }

            TraitEntry deletedTrait = _traitState.State.Traits[traitName];

            _traitState.State.Traits.Remove(traitName);
            await _traitState.WriteStateAsync();

            //publish the DeletedTraitResponseOk event
            return new DeleteTraitResponseOk(deletedTrait)
            {
                DeletedTrait = deletedTrait
            };
        }
        catch (Exception ex)
        {
            return new DeleteTraitResponseNotOk(ex.Message);
        }
    }

    public async Task<UpdateTraitAPIResponse> UpdateTrait(string traitName, TraitEntry traitEntry)
    {
        try
        {
            if (!_traitState.State.Traits.ContainsKey(traitName))
            {
                throw new ArgumentException($"Attribute with name {traitName} does not exist.");
            }

            _traitState.State.Traits[traitName] = traitEntry;
            await _traitState.WriteStateAsync();

            return new UpdateTraitResponseOk(traitEntry)
            {
                UpdatedTrait = traitEntry
            };
        }
        catch (Exception ex)
        {
            return new UpdateTraitResponseNotOk(ex.Message);
        }
    }

    public async Task<ClearAllTraitsAPIResponse> ClearAllTraits()
    {
        try
        {
            long totalTraitsCount = GetTraitsCount();
            _traitState.State.Traits.Clear();
            await _traitState.WriteStateAsync();
            return new ClearAllTraitsResponseOk()
            {
                ClearedTraitsCount = totalTraitsCount
            };
        }
        catch (Exception ex)
        {
            return new ClearAllTraitsResponseNotOk(ex.Message);
        }
    }

    public Task<Dictionary<string, TraitEntry>> GetTraitsMap(List<string> traitNames)
    {
        var traits = new Dictionary<string, TraitEntry>();

        foreach (var traitName in traitNames)
        {
            //get trait by name
            if (_traitState.State.Traits.TryGetValue(traitName, out var traitEntry))
            {
                traits[traitName] = traitEntry;
            }
            else
            {
                throw new KeyNotFoundException($"Attribute with name {traitName} not found.");
            }
        }

        return Task.FromResult(traits);
    }

    public Dictionary<string, TraitEntry> GetTraitsMap()
    {
        return _traitState.State.Traits;
    }

    public int GetTraitsCount()
    {
        return _traitState.State.Traits.Count;
    }
}
