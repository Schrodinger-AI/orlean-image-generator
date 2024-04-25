using Shared;

namespace Grains;

[GenerateSerializer]
public class ZkProofState
{
    [Id(0)]
    public bool isPoverLoad { get; set; }

}