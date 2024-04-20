namespace Shared.Abstractions.Images;

using System.Text.Json.Serialization;
using Shared.Abstractions.Constants;

[GenerateSerializer]
public class ImageGenerationGrainResponseDto
{
    [Id(0)]
    public string RequestId { get; set; }

    [Id(1)]
    public long ImageGenerationRequestTimestamp { get; set; }

    [Id(2)]
    public bool IsSuccessful { get; set; }
    [Id(3)]
    public string? Error { get; set; }
    [Id(4)]
    public ImageGenerationErrorCode? ErrorCode { get; set; }
}