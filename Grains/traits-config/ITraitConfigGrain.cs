using Orleans;
using Shared;
namespace Grains;

    public interface ITraitConfigGrain : ISchrodingerGrain, IGrainWithStringKey
    {
        Task<Dictionary<string, TraitEntry>> GetAllTraits();
        Task<TraitEntry> GetTraitByName(string traitName);
        Task<AddTraitAPIResponse> AddTrait(TraitEntry traitEntry);

        Task<AddTraitsAPIResponse> AddTraits(List<TraitEntry> traitEntries);

        Task<DeleteTraitAPIResponse> DeleteTrait(string traitName);

        Dictionary<string, TraitEntry> GetTraitsMap(List<string> traitNames);
    }
