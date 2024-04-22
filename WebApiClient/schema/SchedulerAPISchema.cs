using System.Text.Json.Serialization;
using Shared.Abstractions.ApiKeys;

namespace WebApi.Models;

public abstract class AddApiKeyAPIResponse { }

public class AddApiKeyResponseOk(List<ApiKeyDto> apiKeys, List<ApiKeyDto>? duplicateApiKeys) : AddApiKeyAPIResponse
{
    [JsonPropertyName("addedApiKey")]
    public List<ApiKeyDto> AddedApiKey { get; set; } = apiKeys;
    [JsonPropertyName("duplicateAPIKeys")]
    public List<ApiKeyDto>? DuplicateAPIKeys { get; set; } = duplicateApiKeys;
}

public class AddApiKeyResponseFailed(string error, List<ApiKeyDto>? duplicateApiKeys) : AddApiKeyAPIResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = error;
    
    [JsonPropertyName("duplicateAPIKeys")]
    public List<ApiKeyDto>? DuplicateAPIKeys { get; set; } = duplicateApiKeys;
}

public abstract class RemoveApiKeyAPIResponse { }

public class RemoveApiKeyResponseOk(List<ApiKeyDto> apiKeys) : RemoveApiKeyAPIResponse
{
    [JsonPropertyName("removedApiKey")]
    public List<ApiKeyDto> RemovedApiKey { get; set; } = apiKeys;
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

public class ApiKeyUsageInfoDto
{
    public ApiKey? ApiKey { get; set; } = null;
    public string ReactivationTimestamp { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ErrorCode { get; set; }
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

public abstract class ForceRequestExecutionResponse { }

public class ForceRequestExecutionResponseOk(bool successful) : ForceRequestExecutionResponse
{
    [JsonPropertyName("Successful")]
    public bool Successful { get; set; } = successful;
}

public class ForceRequestExecutionResponseFailed(string error) : ForceRequestExecutionResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = error;
}

public abstract class BlockedRequestResponse { }

public class BlockedRequestResponseOk<T>(T request) : BlockedRequestResponse
{
    [JsonPropertyName("blockedRequests")]
    public T BlockedRequests { get; set; } = request;
}

public class BlockedRequestResponseFailed(string error) : BlockedRequestResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = error;
}