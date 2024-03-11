using Orleans;
using Shared;
namespace Grains;

    public interface IImageGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
    {
        Task<ImageGenerationGrainResponse> GenerateImageAsync(List<Trait> traits, string imageRequestId);

         Task<ImageQueryGrainResponse> QueryImageAsync();
    }
