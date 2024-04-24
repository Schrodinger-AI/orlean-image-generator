using Schrodinger.Backend.Abstractions.ApiKeys;

namespace Schrodinger.Backend.Grains.types;

public class APIAccountInfo
{
    public string Description { get; set; } = "";
    public string Email { get; set; } = "";
    public ApiKey ApiKey { get; set; } = null;
    public int Tier { get; set; } = 0;
    public int MaxQuota { get; set; } = 5;
}