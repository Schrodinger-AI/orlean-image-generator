using System.Drawing;
using System.Drawing.Text;
using Grains;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Attribute = Shared.Attribute;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;
using PointF = SixLabors.ImageSharp.PointF;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using FontCollection = SixLabors.Fonts.FontCollection;
using FontStyle = SixLabors.Fonts.FontStyle;

namespace WebApi.Controllers;

[ApiController]
[Route("image")]
public class MultiImageGeneratorController : ControllerBase
{
    private readonly IClusterClient _client;

    private readonly ILogger<MultiImageGeneratorController> _logger;


    public MultiImageGeneratorController(IClusterClient client, ILogger<MultiImageGeneratorController> logger)
    {
        _client = client;
        _logger = logger;
    }


    [HttpPost("inspect")]
    public async Task<ImageGenerationState> Inspect(InspectGeneratorRequest request)
    {
        var grain = _client.GetGrain<IImageGeneratorGrain>(request.RequestId);
        var state = await grain.GetStateAsync();
        return state;
    }

    [HttpPost("generate")]
    public async Task<ImageGenerationResponse> GenerateImage(ImageGenerationRequest imageGenerationRequest)
    {
        List<Attribute> newTraits = imageGenerationRequest.NewTraits;
        List<Attribute> baseTraits = imageGenerationRequest.BaseImage.Attributes;

        //collect the newTraits from the request and combine it with trats from the base image
        IEnumerable<Attribute> traits = newTraits.Concat(baseTraits);

        //generate a new UUID with a prefix of "imageRequest"        
        string imageRequestId = "MultiImageRequest_" + Guid.NewGuid().ToString();

        var multiImageGeneratorGrain = _client.GetGrain<IMultiImageGeneratorGrain>(imageRequestId);

        var response = await multiImageGeneratorGrain.GenerateMultipleImagesAsync(traits.ToList(),
            imageGenerationRequest.NumberOfImages, imageRequestId);

        if (response.IsSuccessful)
        {
            return new ImageGenerationResponseOk { RequestId = imageRequestId };
        }
        else
        {
            List<string> errorMessages = response.Errors ?? new List<string>();
            string errorMessage = string.Join(", ", errorMessages);
            return new ImageGenerationResponseNotOk { Error = errorMessage };
        }
    }

    [HttpPost("query")]
    public async Task<ObjectResult> QueryImage(ImageQueryRequest imageQueryRequest)
    {
        var multiImageGeneratorGrain = _client.GetGrain<IMultiImageGeneratorGrain>(imageQueryRequest.RequestId);

        var imageQueryResponse = await multiImageGeneratorGrain.QueryMultipleImagesAsync();
        if (imageQueryResponse.Uninitialized)
            return StatusCode(404, new ImageQueryResponseNotOk { Error = "Request not found" });

        if (imageQueryResponse.Status != ImageGenerationStatus.SuccessfulCompletion)
            return StatusCode(202, new ImageQueryResponseNotOk { Error = "The result is not ready." });
        var images = imageQueryResponse.Images ?? [];
        return StatusCode(200, new ImageQueryResponseOk { Images = images });

    }
    [HttpPost("process")]
    public IActionResult AddWatermark([FromBody] WatermarkApiSchema.WatermarkRequest request)
    {
        try
        {
            // Convert input Base64 string to byte array
            var inputBytes = Convert.FromBase64String(request.SourceImage.Split(",")[1]);

            // Load the input image from byte array
            using var image = Image.Load(inputBytes);

            // Define the font and text options for your watermark
            var fonts = new FontCollection();
            var fontFamily = fonts.Add("font/PressStart2P-Regular.ttf");
            var font = fontFamily.CreateFont(10, FontStyle.Regular);

            var text = request.Watermark.Text;

            var textSize = TextMeasurer.MeasureSize(text, new TextOptions(font));
            var textLocation = new PointF(image.Width - textSize.Width - 10, image.Height - textSize.Height - 10);

            var backgroundRectangle = new RectangularPolygon(textLocation.X - 5, textLocation.Y - 5, textSize.Width + 10, textSize.Height + 10);
            image.Mutate(x => x.Fill(Color.White.WithAlpha(0.6f), backgroundRectangle));

            // Apply the watermark
            image.Mutate(x =>
                x.DrawText(
                    text,
                    font,
                    Color.Black.WithAlpha(0.6f),
                    textLocation
                )
            );

            // Convert the watermarked image to Base64 string
            var outputBase64 = ConvertToBase64(image);

            var logDetails = new WatermarkApiSchema.LogDetails
            {
                Timestamp = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}",
                RequestMethod = "POST",
                RequestBody = request,
                RequestUrl = "/image/process",
                StatusCode = 200,
                Response = outputBase64
            };

            // _logger.LogInformation("Image processed successfully. {@LogDetails}", logDetails);

            return Ok(new { ProcessedImage = $"data:image/webp;base64,{outputBase64}" });
        }
        catch (Exception ex)
        {
            var logDetails = new WatermarkApiSchema.LogDetails
            {
                Timestamp = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}",
                RequestMethod = "POST",
                RequestBody = request,
                RequestUrl = "/image/process",
                StatusCode = 500,
                Response = ex.Message
            };
            // _logger.LogError("An error occurred while processing the image. {@LogDetails}", logDetails);
            var errorResponse = new
            {
                error = ex.Message
            };
            return StatusCode(500, errorResponse);
        }
    }

    // Convert the image to Base64 string
    private static string ConvertToBase64(Image image)
    {
        using var memoryStream = new MemoryStream();
        image.Save(memoryStream, SixLabors.ImageSharp.Formats.Webp.WebpFormat.Instance);
        return Convert.ToBase64String(memoryStream.ToArray());
    }
}