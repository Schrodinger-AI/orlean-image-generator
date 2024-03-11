using Orleans;

namespace Shared;

public interface ISchrodingerGrain : IGrainWithGuidKey
{}

public interface IImageGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
{
    Task<ImageGenerationGrainResponse> GenerateImageFromPromptAsync(string prompt, string imageRequestId);

    Task<ImageQueryGrainResponse> QueryImageAsync();
}

public interface IMultiImageGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
{
    Task<MultiImageGenerationGrainResponse> GenerateMultipleImagesAsync(List<Trait> traits, int NumberOfImages, string multiImageRequestId);

    Task<MultiImageQueryGrainResponse> QueryMultipleImagesAsync();
}
