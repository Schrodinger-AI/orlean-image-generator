using Schrodinger.Backend.Grains.ImageGenerator;
using Schrodinger.Backend.Abstractions.Constants;
using Schrodinger.Backend.Abstractions.Interfaces;

namespace GrainsTest;

using GrainsTest.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans.Runtime;
using Xunit;

[Collection(ClusterCollection.Name)]
public class MultiImageGenerationGrainQueryStatusTest(ClusterFixture fixture)
{
    readonly Mock<ISchedulerGrain> _mockSchedulerGrain = new();

    [Fact]
    public async Task ShouldGetCurrentStatus_As_SuccessfulCompletion() {
        var mockMultiLogger = new Mock<ILogger<MultiImageGeneratorGrain>>();
        var mockMultiImageGeneratorGrain = new Mock<MultiImageGeneratorGrain>(initializeMultiImageGenerationState().Object, mockMultiLogger.Object);
        // Setup the mockGrainFactory to return the mock objects
        mockMultiImageGeneratorGrain.Setup(x => x.GrainFactory.GetGrain<ISchedulerGrain>(It.IsAny<string>(), It.IsAny<string>())).Returns(_mockSchedulerGrain.Object);

        var imageGenerationStatus = await mockMultiImageGeneratorGrain.Object.GetCurrentImageGenerationStatus();

        Assert.Equal(ImageGenerationStatus.SuccessfulCompletion, imageGenerationStatus);
    }
    
    [Fact]
    public async Task ShouldGetCurrentStatus_As_InProgress() {
        var mockMultiLogger = new Mock<ILogger<MultiImageGeneratorGrain>>();
        var multiImageGenerationState = initializeMultiImageGenerationState().Object;
        multiImageGenerationState.State.imageGenerationTrackers["testRequestId2"].Status = ImageGenerationStatus.InProgress;
        var mockMultiImageGeneratorGrain = new Mock<MultiImageGeneratorGrain>(multiImageGenerationState, mockMultiLogger.Object);
        // Setup the mockGrainFactory to return the mock objects
        mockMultiImageGeneratorGrain.Setup(x => x.GrainFactory.GetGrain<ISchedulerGrain>(It.IsAny<string>(), It.IsAny<string>())).Returns(_mockSchedulerGrain.Object);

        var imageGenerationStatus = await mockMultiImageGeneratorGrain.Object.GetCurrentImageGenerationStatus();

        Assert.Equal(ImageGenerationStatus.InProgress, imageGenerationStatus);
    }
    
    [Fact]
    public async Task ShouldGetCurrentStatus_As_InProgress_With_FewNormalFailures() {
        var mockMultiLogger = new Mock<ILogger<MultiImageGeneratorGrain>>();
        var multiImageGenerationState = initializeMultiImageGenerationState().Object;
        multiImageGenerationState.State.imageGenerationTrackers["testRequestId2"].Status = ImageGenerationStatus.FailedCompletion;
        var mockMultiImageGeneratorGrain = new Mock<MultiImageGeneratorGrain>(multiImageGenerationState, mockMultiLogger.Object);
        // Setup the mockGrainFactory to return the mock objects
        mockMultiImageGeneratorGrain.Setup(x => x.GrainFactory.GetGrain<ISchedulerGrain>(It.IsAny<string>(), It.IsAny<string>())).Returns(_mockSchedulerGrain.Object);

        var imageGenerationStatus = await mockMultiImageGeneratorGrain.Object.GetCurrentImageGenerationStatus();

        Assert.Equal(ImageGenerationStatus.InProgress, imageGenerationStatus);
    }
    
    [Fact]
    public async Task ShouldGetCurrentStatus_As_FailedCompletion_With_AContentViolation() {
        var mockMultiLogger = new Mock<ILogger<MultiImageGeneratorGrain>>();
        var multiImageGenerationState = initializeMultiImageGenerationState().Object;
        multiImageGenerationState.State.imageGenerationTrackers["testRequestId2"].Status = ImageGenerationStatus.FailedCompletion;
        multiImageGenerationState.State.imageGenerationTrackers["testRequestId2"].ErrorCode = ImageGenerationErrorCode.content_violation;
        var mockMultiImageGeneratorGrain = new Mock<MultiImageGeneratorGrain>(multiImageGenerationState, mockMultiLogger.Object);
        // Setup the mockGrainFactory to return the mock objects
        mockMultiImageGeneratorGrain.Setup(x => x.GrainFactory.GetGrain<ISchedulerGrain>(It.IsAny<string>(), It.IsAny<string>())).Returns(_mockSchedulerGrain.Object);

        var imageGenerationStatus = await mockMultiImageGeneratorGrain.Object.GetCurrentImageGenerationStatus();

        Assert.Equal(ImageGenerationStatus.FailedCompletion, imageGenerationStatus);
    }
    
    private static Mock<IPersistentState<MultiImageGenerationState>> initializeMultiImageGenerationState()
    {
        var state = new MultiImageGenerationState();
        state.imageGenerationTrackers = new Dictionary<string, ImageGenerationTracker>();
        state.ImageGenerationRequestIds = ["testRequestId1", "testRequestId2"];
        state.imageGenerationTrackers.Add("testRequestId1", new ImageGenerationTracker
        {
            RequestId = "testRequestId1",
            Status = ImageGenerationStatus.SuccessfulCompletion
        });
        state.imageGenerationTrackers.Add("testRequestId2", new ImageGenerationTracker
        {
            RequestId = "testRequestId2",
            Status = ImageGenerationStatus.SuccessfulCompletion
        });
        var mockMultiImageGenerationState = new Mock<IPersistentState<MultiImageGenerationState>>();
        mockMultiImageGenerationState.SetupGet(o => o.State).Returns(state);
        return mockMultiImageGenerationState;
    }
}