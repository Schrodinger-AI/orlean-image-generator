using Orleans;
namespace Grains;

    public interface ITraitConfigGrain : ISchrodingerGrain, IGrainWithStringKey
    {
        Task<Dictionary<string, TraitEntry>> GetAllTraits();
        Task<TraitEntry> GetTraitByName(string traitName);
        Task AddTrait(string traitName, TraitEntry traitEntry);

        Task DeleteTrait(string traitName);

        Task<Dictionary<string, TraitEntry>> GetTraitsMap(List<string> traitNames);
    }
