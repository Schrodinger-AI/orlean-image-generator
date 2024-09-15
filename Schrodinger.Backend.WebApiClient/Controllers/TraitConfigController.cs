using Microsoft.AspNetCore.Mvc;
using Schrodinger.Backend.Abstractions.Interfaces.Trait;
using Schrodinger.Backend.Abstractions.Types.Trait;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("traits")]
    public class TraitsController : ControllerBase
    {
        private readonly IClusterClient _client;

        public TraitsController(IClusterClient client)
        {
            _client = client;
        }

        [HttpPost("add")]
        public async Task<AddTraitsAPIResponse> AddTraits(List<TraitEntry> traitEntries)
        {
            var traitConfigGrain = _client.GetGrain<ITraitConfigGrain>("traitConfigGrain");
            var traitAPIResponse = await traitConfigGrain.AddTraits(traitEntries);
            return traitAPIResponse;
        }

        [HttpDelete("{name}")]
        public async Task<DeleteTraitAPIResponse> DeleteTrait(string name)
        {
            var traitConfigGrain = _client.GetGrain<ITraitConfigGrain>("traitConfigGrain");
            var deleteTraitAPIResponse = await traitConfigGrain.DeleteTrait(name);
            return deleteTraitAPIResponse;
        }

        [HttpPost("clear")]
        public async Task<ClearAllTraitsAPIResponse> clearAllTraits()
        {
            var traitConfigGrain = _client.GetGrain<ITraitConfigGrain>("traitConfigGrain");
            var clearAllTraitsAPIResponse = await traitConfigGrain.ClearAllTraits();
            return clearAllTraitsAPIResponse;
        }

        [HttpGet("{name}")]
        public async Task<ActionResult<TraitEntry>> GetTraitByName(string name)
        {
            var traitConfigGrain = _client.GetGrain<ITraitConfigGrain>("traitConfigGrain");
            var trait = await traitConfigGrain.GetTraitByName(name);
            if (trait == null)
            {
                return NotFound();
            }
            return Ok(trait);
        }

        [HttpGet]
        public async Task<ActionResult<TraitEntry[]>> GetAllTraits()
        {
            var traitConfigGrain = _client.GetGrain<ITraitConfigGrain>("traitConfigGrain");
            Dictionary<string, TraitEntry> traits = await traitConfigGrain.GetAllTraits();
            return Ok(traits.Values.ToArray());
        }
    }
}