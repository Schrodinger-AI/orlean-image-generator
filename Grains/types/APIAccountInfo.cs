using Shared;

namespace Grains.types;

public class APIAccountInfo
{
    public string Description { get; set; } = "";
    public string Email { get; set; } = "";
    public ApiKey ApiKey { get; set; } = null;
    public int Tier { get; set; } = 0;
    public int MaxQuota { get; set; } = 5;
}

[Serializable]
[GenerateSerializer]
public class AddApiKeysResponse
{
    public bool IsSuccessful { get; set; }
    public List<ApiKey> ValidApiKeys { get; set; }
    public List<string> InvalidApiKeys { get; set; }
}