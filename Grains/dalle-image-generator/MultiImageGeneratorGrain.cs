using Orleans;
using Orleans.Runtime;
using Shared;

namespace Grains;

public class MultiImageGeneratorGrain : Grain, IMultiImageGeneratorGrain
{
    private readonly IPersistentState<MultiImageGenerationState> _multiImageGenerationState;

    public MultiImageGeneratorGrain([PersistentState("multiImageGenerationState", "MySqlSchrodingerImageStore")] IPersistentState<MultiImageGenerationState> multiImageGenerationState)
    {
        _multiImageGenerationState = multiImageGenerationState;
    }

    public async Task<ImageGenerationResponse> GenerateMultipleImagesAsync(ImageGenerationRequest request, string multiImageRequestId)
    {
        _multiImageGenerationState.State.RequestId = multiImageRequestId;
        await _multiImageGenerationState.WriteStateAsync();

        for (int i = 0; i < request.NumberOfImages; i++)
        {
            //generate a new UUID with a prefix of "imageRequest"        
            string imageRequestId = "ImageRequest_" + Guid.NewGuid().ToString();

            var imageGeneratorGrain = GrainFactory.GetGrain<IImageGeneratorGrain>(imageRequestId);

            var imageGenerationResponse = await imageGeneratorGrain.GenerateImageAsync(request, imageRequestId);

            Console.WriteLine("Image generation submitted for request: "+ imageRequestId + " with response: " + imageGenerationResponse);

            if (imageGenerationResponse is ImageGenerationResponseNotOk)
            {
                // Abort the process and return an error response
                return imageGenerationResponse;
            }

            if (imageGenerationResponse is ImageGenerationResponseOk okResponse)
            {
                // Extract the requestId from the response
                var requestId = okResponse.RequestId;

                // Add the requestId to the state
                _multiImageGenerationState.State.ImageGenerationRequestIds.Add(requestId);

                // Write the state to the storage provider
                await _multiImageGenerationState.WriteStateAsync();
            }
        }

        var response = new ImageGenerationResponseOk
        {
            RequestId = multiImageRequestId
        };

        return response;
    }

    public async Task<ImageQueryResponse> QueryMultipleImagesAsync()
    {
        var imageGenerationRequestIds = _multiImageGenerationState.State.ImageGenerationRequestIds;

        var allImages = new List<ImageDescription>();

        foreach (var imageGenerationRequestId in imageGenerationRequestIds)
        {
            var imageGeneratorGrain = GrainFactory.GetGrain<IImageGeneratorGrain>(imageGenerationRequestId);

            var response = await imageGeneratorGrain.QueryImageAsync();

            if (response is ImageQueryResponseNotOk notOkResponse)
            {
                // Abort the process and return an error response
                return notOkResponse;
            }

            if (response is ImageQueryResponseOk okResponse)
            {
                // Extract the images from the response
                var images = okResponse.Images;

                // Add the images to the allImages list
                allImages.AddRange(images);
            }
        }

        return new ImageQueryResponseOk { Images = allImages };
    }
}
