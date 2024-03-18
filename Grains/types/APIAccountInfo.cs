using Shared;

namespace Grains.types;

[GenerateSerializer]
public class APIAccountInfo
{
    [Id(0)]
    public string Description { get; set; } = "";
    [Id(1)]
    public string Email { get; set; } = "";
    [Id(2)]
    public ApiKey ApiKey { get; set; } = new ApiKey();
    [Id(3)]
    public int Tier { get; set; } = 0;
    [Id(4)]
    public int MaxQuota { get; set; } = 5;
}