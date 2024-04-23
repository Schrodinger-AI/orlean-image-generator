using Orleans;
using Schrodinger.Backend.Abstractions.Trait;

namespace Schrodinger.Backend.Abstractions.Interfaces
{
    public interface ITraitConfigGrain : ISchrodingerGrain, IGrainWithStringKey
    {
        Task<Dictionary<string, TraitEntry>> GetAllTraits();
        Task<TraitEntry> GetTraitByName(string traitName);
        Task<AddTraitAPIResponse> AddTrait(TraitEntry traitEntry);

        Task<AddTraitsAPIResponse> AddTraits(List<TraitEntry> traitEntries);

        Task<ClearAllTraitsAPIResponse> ClearAllTraits();

        Task<DeleteTraitAPIResponse> DeleteTrait(string traitName);

        Task<Dictionary<string, TraitEntry>> GetTraitsMap(List<string> traitNames);
    }
}