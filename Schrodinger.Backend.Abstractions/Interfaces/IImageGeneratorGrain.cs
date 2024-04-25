namespace Schrodinger.Backend.Abstractions.Interfaces;

using Orleans;
using Schrodinger.Backend.Abstractions.Images;
using Schrodinger.Backend.Abstractions.ApiKeys;

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