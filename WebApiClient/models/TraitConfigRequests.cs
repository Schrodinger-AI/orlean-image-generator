using System.Text.Json.Serialization;
using Shared;

namespace WebApi.models

{
    public class AddTraitConfigRequest
    {
        [JsonPropertyName("traitEntries")]
        public List<TraitEntry> TraitEntries { get; set; }
    }

}