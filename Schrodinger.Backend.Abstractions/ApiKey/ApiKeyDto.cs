namespace Schrodinger.Backend.Abstractions.ApiKeys
{
    /// <summary>
    /// Represents a data transfer object for an API key.
    /// Contains information about the API key string, service provider, and URL.
    /// </summary>
    [GenerateSerializer]
    public class ApiKeyDto
    {
        /// <summary>
        /// Gets or sets the API key string.
        /// </summary>
        [Id(0)]
        public string ApiKeyString { get; set; } = "";

        /// <summary>
        /// Gets or sets the service provider.
        /// </summary>
        [Id(1)]
        public string ServiceProvider { get; set; } = "";

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        [Id(2)]
        public string Url { get; set; } = "";

        public ApiKeyDto()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiKeyDto"/> class using an existing <see cref="ApiKey"/> instance.
        /// </summary>
        /// <param name="apiKey">The <see cref="ApiKey"/> instance to use for initialization.</param>
        public ApiKeyDto(ApiKey apiKey)
        {
            ApiKeyString = apiKey.GetObfuscatedApiKeyString();
            ServiceProvider = apiKey.ServiceProvider.ToString();
            Url = apiKey.Url;
        }
    }
}