using Grains;
using Orleans;
using Shared;
using Attribute = Shared.Attribute;

namespace Grains.interfaces;

public interface ISchrodingerGrain : IGrainWithGuidKey
{ }

public interface IImageGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
{
    Task SetImageGenerationServiceProvider(string apiKey, ImageGenerationServiceProvider serviceProvider);
    Task<ImageGenerationGrainResponse> GenerateImageFromPromptAsync(string prompt, string imageRequestId, string parentRequestId);

    Task SetImageGenerationRequestData(string prompt, string imageRequestId, string parentRequestId);
    Task<ImageQueryGrainResponse> QueryImageAsync();
    Task<ImageGenerationState> GetStateAsync();
    
    Task TriggerImageGenerationAsync();
}

public interface IMultiImageGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
{
    Task<MultiImageGenerationGrainResponse> GenerateMultipleImagesAsync(List<Attribute> traits, int NumberOfImages, string multiImageRequestId);

    Task NotifyImageGenerationStatus(string imageRequestId, ImageGenerationStatus status, string? error);

    Task<MultiImageQueryGrainResponse> QueryMultipleImagesAsync();
}
