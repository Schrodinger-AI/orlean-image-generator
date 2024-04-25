using Schrodinger.Backend.Abstractions.Types.Trait;

namespace Schrodinger.Backend.Grains.TraitConfig
{
    public class TraitState
    {
        public Dictionary<string, TraitEntry> Traits { get; set; } =
            new Dictionary<string, TraitEntry>();
    }
}
