namespace Shared.Abstractions.ApiKeys
{
    /// <summary>
    /// Represents an entry of an API key.
    /// Contains information about the API key, associated email, tier, and maximum quota.
    /// </summary>
    [GenerateSerializer]
    public class ApiKeyEntryDto
    {
        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        [Id(0)]
        public ApiKeyDto ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the email associated with the API key.
        /// </summary>
        [Id(1)]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the tier of the API key.
        /// </summary>
        [Id(2)]
        public int Tier { get; set; }

        /// <summary>
        /// Gets or sets the maximum quota for the API key.
        /// </summary>
        [Id(3)]
        public int MaxQuota { get; set; }
    }
}