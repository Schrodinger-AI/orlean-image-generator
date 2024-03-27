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

public abstract class AddApiKeysResponse
{
    // This class doesn't need to have any properties or methods,
    // but it's useful to have a common base class for all response types.
}

public class AddApiKeysResponseOk : AddApiKeysResponse
{
    public List<ApiKey> ApiKeys { get; set; }

    public AddApiKeysResponseOk(List<ApiKey> apiKeys)
    {
        ApiKeys = apiKeys;
    }
}

public class AddApiKeysResponseNotOk : AddApiKeysResponse
{
    public List<string> InvalidApiKeys { get; set; }

    public AddApiKeysResponseNotOk(List<string> invalidApiKeys)
    {
        InvalidApiKeys = invalidApiKeys;
    }
}