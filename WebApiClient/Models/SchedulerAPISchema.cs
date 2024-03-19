using System.Text.Json.Serialization;
namespace WebApi.ApiKey.Models;


public enum ImageGenerationServiceProvider
{
    DalleOpenAI,
    AzureOpenAI,
}

public class ApiKey
{
    public string ApiKeyString { get; set; } = "";
    public ImageGenerationServiceProvider ServiceProvider { get; set; } = ImageGenerationServiceProvider.DalleOpenAI;
}

public class ApiKeyModel
{
    public string ApiKeyString { get; set; } = "";
    public string ServiceProvider { get; set; } = "";
}

public class ApiKeyEntry
{
    public ApiKeyModel ApiKey { get; set; }
    public string Email { get; set; }
    public int Tier { get; set; }
    public int MaxQuota { get; set; }
}

public abstract class AddApiKeyAPIResponse { }

public class AddApiKeyResponseOk(List<ApiKeyModel> apiKeys) : AddApiKeyAPIResponse
{
    [JsonPropertyName("addedApiKey")]
    public List<ApiKeyModel> AddedApiKey { get; set; } = apiKeys;
}

public class AddApiKeyResponseFailed(string error) : AddApiKeyAPIResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = error;
}

public abstract class RemoveApiKeyAPIResponse { }

public class RemoveApiKeyResponseOk(List<ApiKeyModel> apiKeys) : RemoveApiKeyAPIResponse
{
    [JsonPropertyName("removedApiKey")]
    public List<ApiKeyModel> RemovedApiKey { get; set; } = apiKeys;
}

public class RemoveApiKeyResponseFailed(string error) : RemoveApiKeyAPIResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = error;
}

public abstract class ImageGenerationStatesResponse { }

public class ImageGenerationStatesResponseOk<T>(T imageGenerationStates) : ImageGenerationStatesResponse
{
    [JsonPropertyName("imageGenerationStates")]
    public T ImageGenerationStates { get; set; } = imageGenerationStates;
}

public class ImageGenerationStatesResponseFailed(string error) : ImageGenerationStatesResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = error;
}

public abstract class ApiKeysUsageInfoResponse { }

public class ApiKeysUsageInfoResponseOk<T>(T usageInfo) : ApiKeysUsageInfoResponse
{
    [JsonPropertyName("apiKeysUsageInfo")]
    public T ApiKeysUsageInfo { get; set; } = usageInfo;
}

public class ApiKeysUsageInfoResponseFailed(string error) : ApiKeysUsageInfoResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = error;
}

public abstract class IsOverloadedResponse { }

public class IsOverloadedResponseOk(bool isOverloaded) : IsOverloadedResponse
{
    [JsonPropertyName("IsOverloaded")]
    public bool IsOverloaded { get; set; } = isOverloaded;
}

public class IsOverloadedResponseFailed(string error) : IsOverloadedResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = error;
}