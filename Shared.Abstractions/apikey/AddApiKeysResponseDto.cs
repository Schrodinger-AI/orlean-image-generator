namespace Shared.Abstractions.ApiKeys;

[GenerateSerializer]
public class AddApiKeysResponseDto
{
    [Id(0)]
    public bool IsSuccessful { get; set; }
    
    [Id(1)]
    public List<ApiKeyDto>? ValidApiKeys { get; set; }
    
    [Id(2)]
    public string? Error { get; set; }
    
    [Id(3)]
    public List<ApiKeyDto>? DuplicateApiKeys { get; set; }
    
}