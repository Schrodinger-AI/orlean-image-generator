using Grains;
using Orleans;

namespace Shared;

public interface ISchrodingerGrain : IGrainWithGuidKey
{ }

public interface IImageGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
{

    Task SetImageGenerationServiceProvider(string apiKey, ImageGenerationServiceProvider serviceProvider);
    Task<ImageGenerationGrainResponse> GenerateImageFromPromptAsync(string prompt, string imageRequestId, string parentRequestId);

    Task SetImageGenerationRequestData(string prompt, string imageRequestId, string parentRequestId);
    
    Task UpdatePromptAsync(string prompt);

    Task<ImageQueryGrainResponse> QueryImageAsync();
    Task<ImageGenerationState> GetStateAsync();
    
    Task TriggerImageGenerationAsync();
}

public interface IMultiImageGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
{
    Task<MultiImageGenerationGrainResponse> GenerateMultipleImagesAsync(List<Attribute> traits, int NumberOfImages, string multiImageRequestId);

    Task UpdatePromptAndAttributes(string prompt, List<Attribute> attributes);

    Task NotifyImageGenerationStatus(string imageRequestId, ImageGenerationStatus status, string? error, ImageGenerationErrorCode? errorCode);

    Task<MultiImageQueryGrainResponse> QueryMultipleImagesAsync();
    
}
