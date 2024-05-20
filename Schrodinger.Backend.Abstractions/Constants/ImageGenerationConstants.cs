namespace Schrodinger.Backend.Abstractions.Constants
{
    /// <summary>
    /// Represents the error codes that can occur during image generation.
    /// </summary>
    public enum ImageGenerationErrorCode
    {
        None,
        api_call_failed,
        invalid_api_key,
        internal_error,
        rate_limit_reached,
        engine_unavailable,
        bad_request,
        billing_quota_exceeded,
        content_violation,
    }

    /// <summary>
    /// Represents the service providers available for image generation.
    /// </summary>
    public enum ImageGenerationServiceProvider
    {
        DalleOpenAI,
        AzureOpenAI,
    }

    /// <summary>
    /// Represents the possible statuses of an image generation process.
    /// </summary>
    public enum ImageGenerationStatus
    {
        Dormant,
        InProgress,
        FailedCompletion,
        SuccessfulCompletion,
        Blocked,
    }
}