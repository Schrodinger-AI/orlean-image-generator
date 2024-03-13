using Grains.Contracts;

namespace Grains;

public class PrompterState
{
    public string ConfigText { get; set; }
    public string ScriptContent { get; set; }
    public string ValidationTestCase { get; set; }
    public bool ValiationOk { get; set; }
}