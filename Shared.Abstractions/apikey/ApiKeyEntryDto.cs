namespace Shared.Abstractions.ApiKeys;

[GenerateSerializer]
public class ApiKeyEntryDto
{
    [Id(0)]
    public ApiKeyDto ApiKey { get; set; }
    [Id(1)]
    public string Email { get; set; }
    [Id(2)]
    public int Tier { get; set; }
    [Id(3)]
    public int MaxQuota { get; set; }
}
