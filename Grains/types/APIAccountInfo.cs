using Shared;

namespace Grains.types;

public class APIAccountInfo
{
    public string Description { get; set; } = "";
    public string Email { get; set; } = "";
    public int Tier { get; set; } = 0;
    public int MaxQuota { get; set; } = 5;
    public ApiKey ApiKey { get; set; } = new ApiKey();
}