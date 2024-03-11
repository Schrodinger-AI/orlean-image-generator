namespace UnitTests.Grains;

public class ConfiguratorGrainState
{
    public SortedSet<string> AllConfigIds { get; set; }
    public string CurrentConfigId { get; set; }
}