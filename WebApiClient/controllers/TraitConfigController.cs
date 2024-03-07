using Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using WebApi.models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TraitsController : ControllerBase
    {
        private readonly IClusterClient _client;

        public TraitsController(IClusterClient client)
        {
            _client = client;
        }

        [HttpPost]
        public async Task<ActionResult> AddTrait(AddTraitConfigRequest addTraitConfigRequest)
        {
            var traitConfigGrain = _client.GetGrain<ITraitConfigGrain>("traitConfigGrain");
            await traitConfigGrain.AddTrait(addTraitConfigRequest.Name, new TraitEntry(addTraitConfigRequest.Name, addTraitConfigRequest.Values, addTraitConfigRequest.Variation));
            return Ok("Trait added successfully");
        }

        [HttpDelete("{name}")]
        public async Task<ActionResult> DeleteTrait(string name)
        {
            var traitConfigGrain = _client.GetGrain<ITraitConfigGrain>("traitConfigGrain");
            try
            {
                await traitConfigGrain.DeleteTrait(name);
                return Ok("Trait deleted successfully");
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
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