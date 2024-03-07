namespace Grains;

public class TraitEntry
{
    public string Name { get; set; }
    public List<string> Values { get; set; }
    public string Variation { get; set; }

    public TraitEntry(string name, List<string> values, string variation)
    {
        Name = name;
        Values = values;
        Variation = variation;
    }
}

public class TraitState
{
    public Dictionary<string, TraitEntry> Traits { get; set; } = new Dictionary<string, TraitEntry>();
}