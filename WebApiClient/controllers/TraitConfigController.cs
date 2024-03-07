using System.Threading.Tasks;
using Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using WebApi.models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TraitConfigController : ControllerBase
    {
        private readonly IClusterClient _client;

        public TraitConfigController(IClusterClient client)
        {
            _client = client;
        }

        [HttpPost("trait-config/add")]
        public async Task<string> addTrait(AddTraitConfigRequest addTraitConfigRequest)
        {
            var traitConfigGrain = _client.GetGrain<ITraitConfigGrain>("traitConfigGrain");
            await traitConfigGrain.AddTrait(addTraitConfigRequest.Name, new TraitEntry(addTraitConfigRequest.Name, addTraitConfigRequest.Values, addTraitConfigRequest.Variation));
            return "Trait added successfully";
        }
       

    }
}