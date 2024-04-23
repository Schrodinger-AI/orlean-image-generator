namespace Schrodinger.Backend.Abstractions.Interfaces;

using Orleans;
using Schrodinger.Backend.Abstractions.Constants;
using Schrodinger.Backend.Abstractions.Images;
using Schrodinger.Backend.Abstractions.ApiKeys;

/// <summary>
/// Represents a Schrodinger grain.
/// </summary>
public interface ISchrodingerGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Activates the grain.
    /// </summary>
    Task Activate()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Interface for an image generator grain.
/// Functions are used to generate images from traits, prompts
/// Functions to update the prompt, query the image, get the state of the image generation
/// </summary>
public interface IImageGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
{
    /// <summary>
    /// Sets the image generation service provider.
    /// </summary>
    /// <param name="apiKey">The API key for the service provider.</param>
    Task SetImageGenerationServiceProvider(ApiKey apiKey);
    
    /// <summary>
    /// Generates an image from a given prompt asynchronously.
    /// </summary>
    /// <param name="prompt">The prompt for the image generation.</param>
    /// <param name="imageRequestId">The unique identifier for the image request.</param>
    /// <param name="parentRequestId">The unique identifier for the parent request.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="ImageGenerationGrainResponseDto"/>.</returns>
    Task<ImageGenerationGrainResponseDto> GenerateImageFromPromptAsync(string prompt, string imageRequestId, string parentRequestId);
    
    
    /// <summary>
    /// Sets the data for the image generation request.
    /// </summary>
    /// <param name="prompt">The prompt for the image generation.</param>
    /// <param name="imageRequestId">The unique identifier for the image request.</param>
    /// <param name="parentRequestId">The unique identifier for the parent request.</param>
    Task SetImageGenerationRequestData(string prompt, string imageRequestId, string parentRequestId);    
    
    /// <summary>
    /// Updates the prompt for the image generation asynchronously.
    /// </summary>
    /// <param name="prompt">The new prompt for the image generation.</param>
    Task UpdatePromptAsync(string prompt);

    
    /// <summary>
    /// Queries the current state of the image generation asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="ImageQueryGrainResponseDto"/>.</returns>
    Task<ImageQueryGrainResponseDto> QueryImageAsync();
    
    /// <summary>
    /// Gets the current state of the image generation asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="ImageGenerationStateDto"/>.</returns>
    Task<ImageGenerationStateDto> GetStateAsync();
    
    /// <summary>
    /// Triggers the image generation process asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task TriggerImageGenerationAsync();
}

/// <summary>
/// Interface for a multi-image generator grain.
/// Functions are used to generate multiple images from traits
/// Functions to update the prompt and attributes, notify the image generation status, query multiple images, get the current image generation status, generate prompt from attributes, and check if already submitted.
/// </summary>
public interface IMultiImageGeneratorGrain : ISchrodingerGrain, IGrainWithStringKey
{
    /// <summary>
    /// Generates multiple images from given traits asynchronously.
    /// </summary>
    /// <param name="traits">The traits for the image generation.</param>
    /// <param name="NumberOfImages">The number of images to generate.</param>
    /// <param name="multiImageRequestId">The unique identifier for the multi-image request.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="MultiImageGenerationGrainResponseDto"/>.</returns>
    Task<MultiImageGenerationGrainResponseDto> GenerateMultipleImagesAsync(List<Attribute> traits, int NumberOfImages, string multiImageRequestId);

    /// <summary>
    /// Updates the prompt and attributes for the image generation.
    /// </summary>
    /// <param name="prompt">The new prompt for the image generation.</param>
    /// <param name="attributes">The new attributes for the image generation.</param>
    Task UpdatePromptAndAttributes(string prompt, List<Attribute> attributes);

    /// <summary>
    /// Notifies the status of the image generation.
    /// </summary>
    /// <param name="imageRequestId">The unique identifier for the image request.</param>
    /// <param name="status">The status of the image generation.</param>
    /// <param name="error">The error message, if any.</param>
    /// <param name="errorCode">The error code, if any.</param>
    Task NotifyImageGenerationStatus(string imageRequestId, ImageGenerationStatus status, string? error, ImageGenerationErrorCode? errorCode);

    /// <summary>
    /// Queries the current state of the multiple image generation asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="MultiImageQueryGrainResponseDto"/>.</returns>
    Task<MultiImageQueryGrainResponseDto> QueryMultipleImagesAsync();

    /// <summary>
    /// Gets the current status of the image generation.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="ImageGenerationStatus"/>.</returns>
    Task<ImageGenerationStatus> GetCurrentImageGenerationStatus();

    /// <summary>
    /// Generates a prompt from given attributes asynchronously.
    /// </summary>
    /// <param name="attributes">The attributes for the prompt generation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the generated prompt.</returns>
    Task<string> GeneratePromptAsync(List<Attribute> attributes);

    /// <summary>
    /// Checks if the image generation request is already submitted.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the request is already submitted.</returns>
    Task<bool> IsAlreadySubmitted();
}
