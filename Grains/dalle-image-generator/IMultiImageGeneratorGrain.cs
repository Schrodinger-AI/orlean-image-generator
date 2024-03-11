using Orleans;
using Shared;
namespace Grains;

public interface IMultiImageGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
{
    Task<ImageGenerationResponse> GenerateMultipleImagesAsync(ImageGenerationRequest request, string multiImageRequestId);

    Task<ImageQueryResponse> QueryMultipleImagesAsync();
}
