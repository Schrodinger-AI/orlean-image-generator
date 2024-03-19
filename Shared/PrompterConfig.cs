namespace Shared;

[GenerateSerializer]
public class PrompterConfig
{
    [Id(0)]
    public string ScriptContent { get; set; }
    [Id(1)]
    public string ConfigText { get; set; }
    [Id(2)]
    public string ValidationTestCase { get; set; }
    [Id(3)]
    public bool ValidationOk { get; set; }
}