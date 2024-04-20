namespace Shared.Abstractions.Images;

using System.Text.Json.Serialization;
using Shared.Abstractions.Constants;

[GenerateSerializer]
public class MultiImageGenerationGrainResponseDto
{
    [Id(0)]
    public string RequestId { get; set; }

    [Id(1)]
    public string Prompt { get; set; }

    [Id(2)]
    public List<Attribute> Traits { get; set; }

    [Id(3)]
    public bool IsSuccessful { get; set; } = false;

    [Id(4)]
    public List<string>? Errors { get; set; }
}