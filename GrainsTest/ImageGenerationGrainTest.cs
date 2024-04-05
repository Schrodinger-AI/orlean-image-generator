namespace GrainsTest;

using Grains;
using Grains.ImageGenerator;
using Microsoft.Extensions.Options;
using GrainsTest.utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans.Runtime;
using Shared;
using Xunit;

using Orleans.TestingHost;

[Collection(ClusterCollection.Name)]
public class ImageGenerationGrainTest(ClusterFixture fixture)
{
    private readonly TestCluster _cluster = fixture.Cluster;

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
        var mockDalleOpenAiImageGenerator = new Mock<IImageGenerator>();
        var mockAzureOpenAiImageGenerator = new Mock<IImageGenerator>();
        var imageGenerators = new List<IImageGenerator> {
            mockDalleOpenAiImageGenerator.Object,
            mockAzureOpenAiImageGenerator.Object
        };
        var imageSettings = new ImageSettings();
        var mockImageSettingsOptions = Options.Create(imageSettings);
        var mockLogger = new Mock<ILogger<ImageGeneratorGrain>>();
        var mockImageGenerationState = GetImageGenerationState();
        var grain = new ImageGeneratorGrain(
            mockImageGenerationState.Object,
            mockImageSettingsOptions,
            imageGenerators,
            mockLogger.Object
        );
        
        // Define mock behavior for DalleOpenAiImageGenerator to return a successful image
        mockDalleOpenAiImageGenerator.Setup(o => o.GenerateImageFromPromptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ImageGenerationGrainResponse {
                RequestId = "Img_123",
                IsSuccessful = true,
                ErrorCode = null
            });

        // Act
        var prompt = "test";
        var imageRequestId = "Img_123";
        var parentImageRequestId = "Parent_123";
        var image = await grain.GenerateImageFromPromptAsync(prompt, imageRequestId, parentImageRequestId);
        
        // Assert
        Assert.NotNull(image);
        Assert.Equal(image.RequestId, imageRequestId);
        Assert.True(image.IsSuccessful);
        Assert.Null(image.ErrorCode);
    }


}