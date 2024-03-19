namespace Shared;

[GenerateSerializer]
public class TraitEntry
{
    [Id(0)]
    public string Name { get; set; }
    [Id(1)]
    public List<string> Values { get; set; }
    [Id(2)]
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