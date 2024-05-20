namespace Schrodinger.Backend.Abstractions.Types.AccountUsage;

/// <summary>
/// Represents information about a blocked request.
/// Contains information about the reason for blocking and the request information.
/// </summary>
[GenerateSerializer]
public class BlockedRequestInfoDto
{
    /// <summary>
    /// Gets or sets the reason for blocking the request.
    /// </summary>
    [Id(0)]
    public string? BlockedReason { get; set; } = "";

    /// <summary>
    /// Gets or sets the information about the request that was blocked.
    /// </summary>
    [Id(1)]
    public RequestAccountUsageInfoDto RequestInfo { get; set; }
}