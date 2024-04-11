namespace GrainsTest;

using Grains;
using Grains.AzureOpenAI;
using Grains.types;
using Grains.usage_tracker;
using Microsoft.Extensions.Options;
using GrainsTest.utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans.Runtime;
using Shared;
using Xunit;
using TimeProvider = Grains.utilities.TimeProvider;

using Orleans.TestingHost;

[Collection(ClusterCollection.Name)]
public class MultiImageGenerationGrainTest(ClusterFixture fixture)
{
    private readonly TestCluster _cluster = fixture.Cluster;
    readonly Mock<IDalleOpenAIImageGenerator> _mockDalleOpenAiImageGenerator = new();
    readonly Mock<IAzureOpenAIImageGenerator> _mockAzureOpenAiImageGenerator = new ();
    readonly Mock<ISchedulerGrain> _mockSchedulerGrain = new();
    readonly Mock<IMultiImageGeneratorGrain> _mockParentGeneratorGrain = new();
    private const int DEFAULT_MAX_QUOTA = 2;

    private static Mock<IPersistentState<ImageGenerationState>> GetImageGenerationState()
    {
        var state = new ImageGenerationState();
        var mockImageGenerationState = new Mock<IPersistentState<ImageGenerationState>>();
        mockImageGenerationState.SetupGet(o => o.State).Returns(state);
        return mockImageGenerationState;
    }

    [Fact]
    public async Task ShouldGenerateMultipleImagesFromTraitsAsync() {
        // Arrange
        var apiKeyString = "apiKey2apiKey2";
        var apiKey1 = new ApiKey
        {
            ApiKeyString = apiKeyString,
            ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI,
            Url = "some_url"
        };
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);
        var mockSchedulerState = GetMockSchedulerState(apiKey1);
        var mockSchedulerGrain = new Mock<ISchedulerGrain>();

        // Setup the mocks to do nothing when their methods are called
        _mockSchedulerGrain.Setup(x => x.ReportFailedImageGenerationRequestAsync(It.IsAny<RequestStatus>())).Returns(Task.CompletedTask);
        _mockParentGeneratorGrain.Setup(x => x.NotifyImageGenerationStatus(It.IsAny<string>(), It.IsAny<ImageGenerationStatus>(), It.IsAny<string>(), It.IsAny<ImageGenerationErrorCode>())).Returns(Task.CompletedTask);
        
        // Setup the mockGrainFactory to return the mock objects
        var mockGrainFactory = new Mock<IGrainFactory>();
        mockGrainFactory.Setup(x => x.GetGrain<ISchedulerGrain>(It.IsAny<string>(), It.IsAny<string>())).Returns(_mockSchedulerGrain.Object);
        //mockGrainFactory.Setup(x => x.GetGrain<ISchedulerGrain>(It.IsAny<string>(), It.IsAny<string>())).Returns(_mockSchedulerGrain.Object);
        mockGrainFactory.Setup(x => x.GetGrain<IMultiImageGeneratorGrain>(It.IsAny<string>(), It.IsAny<string>())).Returns(_mockParentGeneratorGrain.Object);

        var imageSettings = new ImageSettings();
        imageSettings.Width = 512;
        imageSettings.Height = 512;
        var mockImageSettingsOptions = Options.Create(imageSettings);
        var mockLogger = new Mock<ILogger<ImageGeneratorGrain>>();
        var mockImageGenerationState = GetImageGenerationState();
        var imageGeneratorGrain = new Mock<ImageGeneratorGrain>(
            mockImageGenerationState.Object,
            mockImageSettingsOptions,
            _mockDalleOpenAiImageGenerator.Object,
            _mockAzureOpenAiImageGenerator.Object,
            mockLogger.Object
        );
        mockGrainFactory.Setup(x => x.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(imageGeneratorGrain.Object);

        var mockMultiLogger = new Mock<ILogger<MultiImageGeneratorGrain>>();
        var mockMultiImageGeneratorGrain = new Mock<MultiImageGeneratorGrain>(GetMultiImageGenerationState().Object, mockMultiLogger.Object);
        var mockImageGenerationRequestStatusReceiver = new Mock<IImageGenerationRequestStatusReceiver>();
        mockMultiImageGeneratorGrain.Setup(x => x.NotifyImageGenerationStatus(It.IsAny<string>(), It.IsAny<ImageGenerationStatus>(), It.IsAny<string>(), It.IsAny<ImageGenerationErrorCode>())).Returns(Task.CompletedTask);
        mockImageGenerationRequestStatusReceiver.Setup(x => x.ReportFailedImageGenerationRequestAsync(It.IsAny<RequestStatus>())).Returns(Task.CompletedTask);
        mockImageGenerationRequestStatusReceiver.Setup(x => x.ReportCompletedImageGenerationRequestAsync(It.IsAny<RequestStatus>())).Returns(Task.CompletedTask);
        
        // Set up the mock to return a specific string when GeneratePromptAsync is called
        var prompt = "test";
        mockMultiImageGeneratorGrain
            .Setup(grain => grain.GeneratePromptAsync(It.IsAny<List<Attribute>>()))
            .ReturnsAsync("Your desired string");

        // Set up the mock to do nothing when AddImageGenerationRequest is called
        mockSchedulerGrain
            .Setup(grain => grain.AddImageGenerationRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
            .Returns(Task.CompletedTask); // Do nothing
        
        string localImagePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "images", "file_example_PNG_500kB.png");
        localImagePath = localImagePath.Replace("\\", "/");
        
        // Define mock behavior for DalleOpenAiImageGenerator to return a successful image
        _mockDalleOpenAiImageGenerator
            .Setup(x => x.RunImageGenerationAsync(It.IsAny<string>(), It.IsAny<ApiKey>(), It.IsAny<int>(),
                It.IsAny<ImageSettings>(), It.IsAny<string>()))
            .ReturnsAsync(new ImageGenerationResponse
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

        await imageGeneratorGrain.Object.SetImageGenerationServiceProvider(new ApiKey(apiKeyString: "", strServiceProvider: "DalleOpenAI",  url: ""));
        
        // Act
        var multiImageGenerationGrainResponse = await mockMultiImageGeneratorGrain.Object.GenerateMultipleImagesAsync(generateDummyTraits(), 1, "multi_123");
        
        // assert
        Assert.NotNull(multiImageGenerationGrainResponse);
        Assert.Equal("multi_123", multiImageGenerationGrainResponse.RequestId);
        Assert.True(multiImageGenerationGrainResponse.IsSuccessful);
    }
    
    private static Mock<IPersistentState<SchedulerState>> GetMockSchedulerState(ApiKey? apiKey = null)
    {
        var state = new SchedulerState();
                
        state.ApiAccountInfoList = new List<APIAccountInfo>
        {
            new APIAccountInfo
            {
                ApiKey = apiKey ?? new ApiKey
                {
                    ApiKeyString = "apiKey1",
                    ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI,
                    Url = "some_url"
                },
                Description = "mocked api key 1",
                Email = "mock@mock.com",
                Tier = 0,
                MaxQuota = DEFAULT_MAX_QUOTA
            }
        };
                
        
        var mockSchedulerState = new Mock<IPersistentState<SchedulerState>>();
        mockSchedulerState.SetupGet(o => o.State).Returns(state);
        return mockSchedulerState;
    }
    
    private static Mock<IPersistentState<MultiImageGenerationState>> GetMultiImageGenerationState()
    {
        var state = new MultiImageGenerationState();
        var mockMultiImageGenerationState = new Mock<IPersistentState<MultiImageGenerationState>>();
        mockMultiImageGenerationState.SetupGet(o => o.State).Returns(state);
        return mockMultiImageGenerationState;
    }

    private static List<Attribute> generateDummyTraits()
    {
        Attribute attribute1 = new Attribute
        {
            TraitType = "TestName1",
            Value = "TestValue1"
        };

        Attribute attribute2 = new Attribute
        {
            TraitType = "TestName2",
            Value = "TestValue2"
        };
        
        // Add the attributes to a list
        List<Attribute> traits = new List<Attribute> { attribute1, attribute2 };

        return traits;
    }

}