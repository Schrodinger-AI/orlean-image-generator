using Grains.image_generator;
using Grains.image_generator.AzureOpenAI;
using Grains.image_generator.DalleOpenAI;
using Shared.Abstractions.ApiKeys;
using Shared.Abstractions.Constants;
using Shared.Abstractions.Images;
using Shared.Abstractions.Interfaces;
using Shared.Abstractions.UsageTracker;

namespace GrainsTest;

using Grains;
using Microsoft.Extensions.Options;
using GrainsTest.utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans.Runtime;
using Xunit;

using Orleans.TestingHost;

[Collection(ClusterCollection.Name)]
public class ImageGenerationGrainTest(ClusterFixture fixture)
{
    private readonly TestCluster _cluster = fixture.Cluster;
    readonly Mock<IDalleOpenAIImageGenerator> _mockDalleOpenAiImageGenerator = new();
    readonly Mock<IAzureOpenAIImageGenerator> _mockAzureOpenAiImageGenerator = new ();
    readonly Mock<ISchedulerGrain> _mockSchedulerGrain = new();
    readonly Mock<IMultiImageGeneratorGrain> _mockParentGeneratorGrain = new();

    private static Mock<IPersistentState<ImageGenerationState>> GetImageGenerationState()
    {
        var state = new ImageGenerationState();
        var mockImageGenerationState = new Mock<IPersistentState<ImageGenerationState>>();
        mockImageGenerationState.SetupGet(o => o.State).Returns(state);
        return mockImageGenerationState;
    }

    [Fact]
    public async Task ShouldGenerateImageFromPromptAsync() {
        // Arrange
        // Setup the mocks to do nothing when their methods are called
        _mockSchedulerGrain.Setup(x => x.ReportFailedImageGenerationRequestAsync(It.IsAny<RequestStatus>())).Returns(Task.CompletedTask);
        _mockParentGeneratorGrain.Setup(x => x.NotifyImageGenerationStatus(It.IsAny<string>(), It.IsAny<ImageGenerationStatus>(), It.IsAny<string>(), It.IsAny<ImageGenerationErrorCode>())).Returns(Task.CompletedTask);
        
        var imageSettings = new ImageSettings();
        imageSettings.Width = 512;
        imageSettings.Height = 512;
        var mockImageSettingsOptions = Options.Create(imageSettings);
        var mockLogger = new Mock<ILogger<ImageGeneratorGrain>>();
        var mockImageGenerationState = GetImageGenerationState();
        var grain = new Mock<ImageGeneratorGrain>(
            mockImageGenerationState.Object,
            mockImageSettingsOptions,
            _mockDalleOpenAiImageGenerator.Object,
            _mockAzureOpenAiImageGenerator.Object,
            mockLogger.Object
        );
        
        // Setup the mockGrainFactory to return the mock objects
        var mockGrainFactory = new Mock<IGrainFactory>();
        // grain.Setup(x => x.GrainFactory.GetGrain<ISchedulerGrain>(It.IsAny<string>(), It.IsAny<string>())).Returns(_mockSchedulerGrain.Object);
        // grain.Setup(x => x.GrainFactory.GetGrain<IMultiImageGeneratorGrain>(It.IsAny<string>(), It.IsAny<string>())).Returns(_mockParentGeneratorGrain.Object);
        //
        var mockMultiImageGeneratorGrain = new Mock<IMultiImageGeneratorGrain>();
        var mockImageGenerationRequestStatusReceiver = new Mock<IImageGenerationRequestStatusReceiver>();
        mockMultiImageGeneratorGrain.Setup(x => x.NotifyImageGenerationStatus(It.IsAny<string>(), It.IsAny<ImageGenerationStatus>(), It.IsAny<string>(), It.IsAny<ImageGenerationErrorCode>())).Returns(Task.CompletedTask);
        mockImageGenerationRequestStatusReceiver.Setup(x => x.ReportFailedImageGenerationRequestAsync(It.IsAny<RequestStatus>())).Returns(Task.CompletedTask);
        mockImageGenerationRequestStatusReceiver.Setup(x => x.ReportCompletedImageGenerationRequestAsync(It.IsAny<RequestStatus>())).Returns(Task.CompletedTask);
        
        string localImagePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "images", "file_example_PNG_500kB.png");
        localImagePath = localImagePath.Replace("\\", "/");
        
        // Define mock behavior for DalleOpenAiImageGenerator to return a successful image
        _mockDalleOpenAiImageGenerator
            .Setup(x => x.RunImageGenerationAsync(It.IsAny<string>(), It.IsAny<ApiKey>(), It.IsAny<int>(),
                It.IsAny<ImageSettings>(), It.IsAny<string>()))
            .ReturnsAsync(new AIImageGenerationResponse
            {
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Data = new List<ImageGenerationData>
                {
                    new ImageGenerationData
                    {
                        RevisedPrompt = "test",
                        Url = localImagePath
                    }
                },
                Error = null
            });

        // Act
        var prompt = "test";
        var imageRequestId = "Img_123";
        var parentImageRequestId = "Parent_123";
        await grain.Object.SetImageGenerationServiceProvider(new ApiKey(apiKeyString: "", strServiceProvider: "DalleOpenAI",  url: ""));
        var image = await grain.Object.GenerateImageFromPromptAsync(prompt, imageRequestId, parentImageRequestId);
        
        // Assert
        Assert.NotNull(image);
        Assert.Equal(image.RequestId, imageRequestId);
        Assert.True(image.IsSuccessful);
        Assert.Null(image.ErrorCode);
    }
}