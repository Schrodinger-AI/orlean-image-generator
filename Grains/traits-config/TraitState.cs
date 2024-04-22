using Shared.Abstractions.Trait;

namespace Grains
{
    public class TraitState
    {
        public Dictionary<string, TraitEntry> Traits { get; set; } = new Dictionary<string, TraitEntry>();
    }
}