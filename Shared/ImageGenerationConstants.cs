namespace Shared;

public enum ImageGenerationErrorCode
{
    api_call_failed,
    invalid_api_key,
    internal_error,
    rate_limit_reached,
    engine_unavailable,
    bad_request,
    billing_quota_exceeded,
    content_violation,
}

public enum ImageGenerationServiceProvider
{
    DalleOpenAI,
    AzureOpenAI,
}