using Shared;

namespace Grains;

[GenerateSerializer]
public class PrompterState
{
    [Id(0)]
    public string ConfigText { get; set; }
    [Id(1)]
    public string ScriptContent { get; set; }
    [Id(2)]
    public string ValidationTestCase { get; set; }
    [Id(3)]
    public bool ValiationOk { get; set; }

}