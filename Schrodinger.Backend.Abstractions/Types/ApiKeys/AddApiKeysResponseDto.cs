namespace Schrodinger.Backend.Abstractions.Types.ApiKeys;

/// <summary>
/// Represents the response from an attempt to add API keys.
/// Contains information about the success of the operation, valid API keys, any error message, and duplicate API keys.
/// </summary>
[GenerateSerializer]
public class AddApiKeysResponseDto
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    [Id(0)]
    public bool IsSuccessful { get; set; }
    
    /// <summary>
    /// Gets or sets the list of valid API keys that were added.
    /// </summary>
    [Id(1)]
    public List<ApiKeyDto>? ValidApiKeys { get; set; }
    
    /// <summary>
    /// Gets or sets the error message, if any, from the operation.
    /// </summary>
    [Id(2)]
    public string? Error { get; set; }
    
    /// <summary>
    /// Gets or sets the list of API keys that were identified as duplicates and not added.
    /// </summary>
    [Id(3)]
    public List<ApiKeyDto>? DuplicateApiKeys { get; set; }
}