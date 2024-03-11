using Orleans;
using Shared;
namespace Grains;

public interface IMultiImageGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
{
    Task<MultiImageGenerationGrainResponse> GenerateMultipleImagesAsync(List<Trait> traits, int NumberOfImages, string multiImageRequestId);

    Task<MultiImageQueryGrainResponse> QueryMultipleImagesAsync();
}
