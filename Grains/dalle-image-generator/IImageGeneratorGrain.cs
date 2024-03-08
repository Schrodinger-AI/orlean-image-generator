using Orleans;
using Shared;
namespace Grains;

    public interface IImageGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
    {
        Task<ImageGenerationResponse> generateImageAsync(ImageGenerationRequest imageGenerationRequest, string imageRequestId);

        Task<ImageQueryResponse> queryImageAsync(string imageRequestId);

    }
