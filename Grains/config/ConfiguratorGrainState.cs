namespace UnitTests.Grains;

public class ConfiguratorGrainState
{
    public SortedSet<string> AllConfigIds { get; set; } = new SortedSet<string>();
    public string CurrentConfigId { get; set; }
}