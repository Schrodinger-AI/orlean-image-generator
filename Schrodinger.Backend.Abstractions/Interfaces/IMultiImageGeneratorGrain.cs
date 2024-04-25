using Schrodinger.Backend.Abstractions.Constants;
using Schrodinger.Backend.Abstractions.Images;

namespace Schrodinger.Backend.Abstractions.Interfaces;

using Orleans;
using Attribute = Schrodinger.Backend.Abstractions.Images.Attribute;

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