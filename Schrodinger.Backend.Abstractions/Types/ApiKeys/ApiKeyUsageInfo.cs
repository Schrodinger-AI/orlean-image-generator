namespace Schrodinger.Backend.Abstractions.Types.ApiKeys
{
    using Schrodinger.Backend.Abstractions.Constants;

    /// <summary>
    /// Represents the usage information of an API key.
    /// Contains information about the API key, last used timestamp, attempts, status, and any error codes.
    /// </summary>
    [GenerateSerializer]
    public class ApiKeyUsageInfo
    {
        public const long RATE_LIMIT_WAIT = 120; // 2 minutes
        public const long INVALID_API_KEY_WAIT = 86400; //1 day

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        [Id(0)]
        public ApiKey ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the API key was last used.
        /// </summary>
        [Id(1)]
        public long LastUsedTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the number of attempts made using the API key.
        /// </summary>
        [Id(2)]
        public long Attempts { get; set; }

        /// <summary>
        /// Gets or sets the status of the API key.
        /// </summary>
        [Id(3)]
        public ApiKeyStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the error code, if any, associated with the API key.
        /// </summary>
        [Id(4)]
        public ImageGenerationErrorCode? ErrorCode { get; set; }

        /// <summary>
        /// Calculates the reactivation timestamp based on the error code and attempts.
        /// </summary>
        public long GetReactivationTimestamp()
        {
            return ErrorCode switch
            {
                ImageGenerationErrorCode.rate_limit_reached => LastUsedTimestamp + RATE_LIMIT_WAIT,
                ImageGenerationErrorCode.invalid_api_key => LastUsedTimestamp + INVALID_API_KEY_WAIT,
                _ => LastUsedTimestamp + (long)Math.Min(Math.Pow(3, Attempts), 27.0)
            };
        }
    }
}