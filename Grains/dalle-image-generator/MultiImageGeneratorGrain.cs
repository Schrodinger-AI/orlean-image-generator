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
        bool IsSuccessful = true;

        for (int i = 0; i < request.NumberOfImages; i++)
        {
            //generate a new UUID with a prefix of "imageRequest"        
            string imageRequestId = "ImageRequest_" + Guid.NewGuid().ToString();

            var imageGeneratorGrain = GrainFactory.GetGrain<IImageGeneratorGrain>(imageRequestId);

            List<Trait> newTraits = request.NewTraits;
            List<Trait> baseTraits = request.BaseImage.Traits;

            //collect the newTraits from the request and combine it with trats from the base image
            IEnumerable<Trait> traits = newTraits.Concat(baseTraits);

            var imageGenerationGrainResponse = await imageGeneratorGrain.GenerateImageAsync(traits.ToList(), imageRequestId);

            Console.WriteLine("Image generation submitted for request: " + imageRequestId + " with response: " + imageGenerationGrainResponse);

            IsSuccessful = IsSuccessful && imageGenerationGrainResponse.IsSuccessful;

            if (!imageGenerationGrainResponse.IsSuccessful)
            {
                // Add the error to the state
                if (_multiImageGenerationState.State.Errors == null)
                {
                    _multiImageGenerationState.State.Errors = new List<string>();
                }

                _multiImageGenerationState.State.Errors.Add(imageGenerationGrainResponse.Error);
            }

            else
            {
                // Extract the requestId from the response
                var requestId = imageGenerationGrainResponse.RequestId;

                // Add the requestId to the state
                _multiImageGenerationState.State.ImageGenerationRequestIds.Add(requestId);
            }
        }

        _multiImageGenerationState.State.IsSuccessful = IsSuccessful;
        await _multiImageGenerationState.WriteStateAsync();

        if (!IsSuccessful)
        {
            return new ImageGenerationResponseNotOk
            {
                Error = "Image generation failed"
            };
        }
        else
        {
            return new ImageGenerationResponseOk
            {
                RequestId = multiImageRequestId
            };
        }
    }

    public async Task<ImageQueryResponse> QueryMultipleImagesAsync()
    {
        var allImages = new List<ImageDescription>();
        var isSuccessful = true;
        var errorMessages = new List<string>();

        foreach (var imageGenerationRequestId in _multiImageGenerationState.State.ImageGenerationRequestIds)
        {
            var imageGeneratorGrain = GrainFactory.GetGrain<IImageGeneratorGrain>(imageGenerationRequestId);

            var response = await imageGeneratorGrain.QueryImageAsync();

            if (response is ImageQueryGrainResponse grainResponse)
            {
                if (grainResponse.IsSuccessful && grainResponse.Image != null)
                {
                    allImages.Add(grainResponse.Image);
                }
            }
            else
            {
                isSuccessful = false;
                errorMessages.Add("Error querying image");
            }
        }

        if (isSuccessful)
        {
            return new ImageQueryResponseOk { Images = allImages };
        }
        else
        {
            return new ImageQueryResponseNotOk { Error = string.Join("\n ", errorMessages) };
        }
    }
}
