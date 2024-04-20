namespace Shared.Abstractions.AccountUsage;

using Shared.Abstractions.ApiKeys;

/// <summary>
/// Represents a request for account usage information.
/// Contains information about the request ID, timestamps, attempts, API key, and child ID.
/// </summary>
[GenerateSerializer]
public class RequestAccountUsageInfoDto
{
    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    [Id(0)]
    public string RequestId { get; set; } = "";

    /// <summary>
    /// Gets or sets the timestamp when the request was made.
    /// </summary>
    [Id(1)]
    public string RequestTimestamp { get; set; } = "";

    /// <summary>
    /// Gets or sets the timestamp when the request started processing.
    /// </summary>
    [Id(2)]
    public string StartedTimestamp { get; set; } = "";

    /// <summary>
    /// Gets or sets the timestamp when the request failed, if applicable.
    /// </summary>
    [Id(3)]
    public string FailedTimestamp { get; set; } = "";

    /// <summary>
    /// Gets or sets the timestamp when the request completed, if applicable.
    /// </summary>
    [Id(4)]
    public string CompletedTimestamp { get; set; } = "";

    /// <summary>
    /// Gets or sets the number of attempts made for the request.
    /// </summary>
    [Id(5)]
    public int Attempts { get; set; } = 0;

    /// <summary>
    /// Gets or sets the API key associated with the request.
    /// </summary>
    [Id(6)]
    public ApiKeyDto? ApiKey { get; set; } = null;

    /// <summary>
    /// Gets or sets the child ID associated with the request.
    /// </summary>
    [Id(7)]
    public string ChildId { get; set; } = "";
}