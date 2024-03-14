namespace Shared;

public class ImageGenerationConstants {
    public const string DALLE_BASE_PROMPT = "Rephrase the following to create a logical sentence: A simple pixel art image of a cat ";
};

public enum DalleErrorCodes
{
        api_call_failed,
        invalid_api_key,
        dalle_internal_error,
        rate_limit_reached,
        dalle_engine_unavailable,
        bad_request,
        dalle_billing_quota_exceeded,
}