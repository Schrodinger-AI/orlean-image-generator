using Orleans;
using Shared;
namespace Grains;

    public interface IImageGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
    {
        Task<ImageGenerationResponse> GenerateImageAsync(ImageGenerationRequest imageGenerationRequest, string imageRequestId);

        Task<ImageQueryResponse> QueryImageAsync();
    }
