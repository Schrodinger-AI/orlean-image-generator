using Orleans;
using Schrodinger.Backend.Abstractions.Trait;

namespace Schrodinger.Backend.Abstractions.Interfaces
{
    public interface ITraitConfigGrain : ISchrodingerGrain, IGrainWithStringKey
    {
        /// Mutation functions - ADD/DELETE/CLEAR
        
        /// <summary>
        ///  Add trait
        /// </summary>
        /// <param name="traitEntries"></param>
        /// <returns>APIResponse with Added Trait(s) and totalTraitsCount after Adding traits or Error based on successful or failed completion</returns>
        Task<AddTraitsAPIResponse> AddTraits(List<TraitEntry> traitEntries);
        
        /// <summary>
        /// Delete trait
        /// </summary>
        /// <param name="traitName"></param>
        /// <returns>APIResponse with Deleted Trait and totalTraitsCount after Deletion or Error based on successful or failed completion</returns>
        Task<DeleteTraitAPIResponse> DeleteTrait(string traitName);
        
        /// <summary>
        ///  Clear all traits
        ///  </summary>
        /// <returns>APIResponse with ClearedTraitsCount or Error based on successful or failed completion</returns>
        Task<ClearAllTraitsAPIResponse> ClearAllTraits();
        
        /// Query functions
        
        /// <summary>
        ///   Get trait by name
        /// </summary>
        /// <param name="traitName"></param>
        /// <returns>TraitEntry Struct</returns>
        Task<TraitEntry> GetTraitByName(string traitName);

        /// <summary>
        /// Get all traits
        /// </summary>
        /// <returns>Mapping of traitEntries</returns>
        Task<Dictionary<string, TraitEntry>> GetAllTraits();
    }
}