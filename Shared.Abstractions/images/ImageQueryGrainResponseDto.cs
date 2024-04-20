namespace Shared.Abstractions.Images;

using Shared.Abstractions.Constants;

[GenerateSerializer]
public class ImageQueryGrainResponseDto
{
    [Id(0)]
    public ImageDescription? Image { get; set; }

    [Id(1)]
    public ImageGenerationStatus Status { get; set; }
    [Id(2)]
    public string? Error { get; set; }
}