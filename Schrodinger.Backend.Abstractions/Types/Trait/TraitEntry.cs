namespace Schrodinger.Backend.Abstractions.Types.Trait
{
    using System.Text.Json.Serialization;

    [GenerateSerializer]
    public class TraitEntry
    {
        [Id(0)] public string Name { get; set; }
        [Id(1)] public List<string> Values { get; set; }
        [Id(2)] public string Variation { get; set; }

        public TraitEntry(string name, List<string> values, string variation)
        {
            Name = name;
            Values = values;
            Variation = variation;
        }
    }
}