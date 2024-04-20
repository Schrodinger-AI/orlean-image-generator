namespace Shared.Abstractions.Images;

using System.Text.Json.Serialization;
using Shared.Abstractions.Constants;

[GenerateSerializer]
public class MultiImageQueryGrainResponseDto
{
    [Id(0)]
    public bool Uninitialized { get; set; }
    [Id(1)]
    public List<ImageDescription>? Images { get; set; }

    [Id(2)]
    public ImageGenerationStatus Status { get; set; } = ImageGenerationStatus.Dormant;

    [Id(3)]
    public List<string>? Errors { get; set; }

    [Id(4)] public string? ErrorCode { get; set; } = "";
}