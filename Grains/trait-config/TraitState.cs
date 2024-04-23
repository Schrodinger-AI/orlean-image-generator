using Schrodinger.Backend.Abstractions.Trait;

namespace Grains.trait_config
{
    public class TraitState
    {
        public Dictionary<string, TraitEntry> Traits { get; set; } =
            new Dictionary<string, TraitEntry>();
    }
}
