using Schrodinger.Backend.Abstractions.Types.Trait;

namespace Schrodinger.Backend.Grains.Trait
{
    public class TraitState
    {
        public Dictionary<string, TraitEntry> Traits { get; set; } =
            new Dictionary<string, TraitEntry>();
    }
}
