using Grains.types;
using Grains.usage_tracker;
using GrainsTest.utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans.Runtime;
using Shared;
using Xunit;
using TimeProvider = Grains.utilities.TimeProvider;

namespace GrainsTest;
using Orleans.TestingHost;

[Collection(ClusterCollection.Name)]
public class SchedulerGrainTest(ClusterFixture fixture)
{
    private readonly TestCluster _cluster = fixture.Cluster;
    
    /*[Fact]
    public async Task Scheduler_AddImageGenerationRequest_Test()
    {
        var scheduler = _cluster.GrainFactory.GetGrain<ISchedulerGrain>("SchedulerGrain");
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await scheduler.AddImageGenerationRequest("parent", "myRequestId", now);
        
        await ClusterFixture.Tick();

        Assert.Single(scheduler.GetPendingImageGenerationRequestsAsync().Result);
    }*/

    [Fact]
    public async Task TickAsync_ShouldUpdateApiUsageStatus_WhenReportedBillingQuotaExceeded()
    {
        // Arrange
        var mockSchedulerState = GetMockSchedulerState();
        
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent", "myRequestId", now);
        
        // Act
        await schedulerGrain.Object.TickAsync();

        await schedulerGrain.Object.ReportFailedImageGenerationRequestAsync(new RequestStatus
        {
            RequestId = "myRequestId",
            Status = RequestStatusEnum.Failed,
            ErrorCode = ImageGenerationErrorCode.billing_quota_exceeded
        });
        
        await schedulerGrain.Object.TickAsync();

        // Assert
        // GetApiKeysUsageInfo should return a dictionary with a count of one
        Assert.Single(schedulerGrain.Object.GetApiKeysUsageInfo().Result);
    }
    
    [Fact]
    public async Task TickAsync_ShouldUpdateApiUsageStatus_WhenReportedRateLimitReached()
    {
        // Arrange
        var apiKey = new ApiKey
        {
            ApiKeyString = "apiKey1",
            ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI,
            Url = "some_url"
        };
        var mockSchedulerState = GetMockSchedulerState(apiKey);
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent", "myRequestId", now);
        
        // Act
        await schedulerGrain.Object.TickAsync();

        await schedulerGrain.Object.ReportFailedImageGenerationRequestAsync(new RequestStatus
        {
            RequestId = "myRequestId",
            Status = RequestStatusEnum.Failed,
            ErrorCode = ImageGenerationErrorCode.rate_limit_reached,
            RequestTimestamp = now - ApiKeyUsageInfo.RATE_LIMIT_WAIT + 10
        });
        
        await schedulerGrain.Object.TickAsync();

        // Assert
        var result = schedulerGrain.Object.GetApiKeysUsageInfo().Result;
        Assert.Equal(ApiKeyStatus.OnHold, result[apiKey.GetConcatApiKeyString()].Status);
    }
    
    [Fact]
    public async Task TickAsync_ShouldUpdateApiUsageStatus_WhenReportedRateLimitReached_ApiKeyReactivated()
    {
        // Arrange
        var apiKey = new ApiKey
        {
            ApiKeyString = "apiKey1",
            ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI,
            Url = "some_url"
        };
        var mockSchedulerState = GetMockSchedulerState(apiKey);
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent", "myRequestId", now);
        
        // Act
        await schedulerGrain.Object.TickAsync();

        await schedulerGrain.Object.ReportFailedImageGenerationRequestAsync(new RequestStatus
        {
            RequestId = "myRequestId",
            Status = RequestStatusEnum.Failed,
            ErrorCode = ImageGenerationErrorCode.rate_limit_reached,
            RequestTimestamp = now - ApiKeyUsageInfo.RATE_LIMIT_WAIT - 1
        });
        
        await schedulerGrain.Object.TickAsync();

        // Assert
        var result = schedulerGrain.Object.GetApiKeysUsageInfo().Result;
        Assert.Equal(ApiKeyStatus.Active, result[apiKey.GetConcatApiKeyString()].Status);
    }
    
    [Fact]
    public async Task TickAsync_ShouldUpdateApiUsageStatus_WhenReportedCompletion()
    {
        // Arrange
        var mockSchedulerState = GetMockSchedulerState();
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent", "myRequestId", now);
        
        // Act
        await schedulerGrain.Object.TickAsync();

        await schedulerGrain.Object.ReportCompletedImageGenerationRequestAsync(new RequestStatus
        {
            RequestId = "myRequestId",
            Status = RequestStatusEnum.Completed,
            RequestTimestamp = now
        });
        
        await schedulerGrain.Object.TickAsync();

        // Assert
        Assert.Empty(schedulerGrain.Object.GetApiKeysUsageInfo().Result);
    }
    
    [Fact]
    public async Task TickAsync_AddImageGenerationRequest_WhenReportedCompletion()
    {
        // Arrange
        var mockSchedulerState = GetMockSchedulerState();
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent", "myRequestId", now);
        
        // Act
        await schedulerGrain.Object.TickAsync();

        await schedulerGrain.Object.ReportCompletedImageGenerationRequestAsync(new RequestStatus
        {
            RequestId = "myRequestId",
            Status = RequestStatusEnum.Completed,
            RequestTimestamp = now
        });
        
        await schedulerGrain.Object.TickAsync();

        // Assert
        Assert.Empty(schedulerGrain.Object.GetPendingImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetFailedImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetBlockedImageGenerationRequestsAsync().Result);
        Assert.Single(schedulerGrain.Object.GetCompletedImageGenerationRequestsAsync().Result);
    }
    
    [Fact]
    public async Task TickAsync_AddImageGenerationRequest_WhenReportedFailureMultipleTimes()
    {
        // Arrange
        const string requestId = "myRequestId";
        
        var mockSchedulerState = GetMockSchedulerState();
        var mockLogger = new Mock<ILogger<SchedulerGrain>>();
        
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, mockLogger.Object);
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent", requestId, now);
        
        // Act
        await schedulerGrain.Object.TickAsync();

        await schedulerGrain.Object.ReportFailedImageGenerationRequestAsync(new RequestStatus
        {
            RequestId = requestId,
            Status = RequestStatusEnum.Failed,
            RequestTimestamp = now
        });
        await schedulerGrain.Object.ReportFailedImageGenerationRequestAsync(new RequestStatus
        {
            RequestId = requestId,
            Status = RequestStatusEnum.Failed,
            RequestTimestamp = now
        });
        
        await schedulerGrain.Object.TickAsync();

        // Assert
        Assert.Empty(schedulerGrain.Object.GetPendingImageGenerationRequestsAsync().Result);
        Assert.Single(schedulerGrain.Object.GetFailedImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetBlockedImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetCompletedImageGenerationRequestsAsync().Result);
        //verify that log warning is called once
        mockLogger.Verify(logger => logger.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => string.Equals($"[SchedulerGrain] Request {requestId} not found in pending list", v.ToString(), StringComparison.InvariantCultureIgnoreCase)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
    
    [Fact]
    public async Task TickAsync_ShouldUpdateExpiredPendingRequestsToBlocked()
    {
        // Arrange
        var mockSchedulerState = GetMockSchedulerState();

        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow.AddSeconds(-SchedulerGrain.PENDING_EXPIRY_THRESHOLD - 1));

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent", "myRequestId", now);
        
        // Act
        await schedulerGrain.Object.TickAsync();
        
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);
        
        await schedulerGrain.Object.TickAsync();

        // Assert
        Assert.Empty(schedulerGrain.Object.GetPendingImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetFailedImageGenerationRequestsAsync().Result);
        Assert.Single(schedulerGrain.Object.GetBlockedImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetCompletedImageGenerationRequestsAsync().Result);
    }
    
    [Fact]
    public async Task TickAsync_ShouldNotUpdateExpiredPendingRequestsToBlocked()
    {
        // Arrange
        var mockSchedulerState = GetMockSchedulerState();

        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow.AddSeconds(-SchedulerGrain.PENDING_EXPIRY_THRESHOLD + 1));

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent", "myRequestId", now);
        
        // Act
        await schedulerGrain.Object.TickAsync();
        
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);
        
        await schedulerGrain.Object.TickAsync();

        // Assert
        Assert.Single(schedulerGrain.Object.GetPendingImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetFailedImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetBlockedImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetCompletedImageGenerationRequestsAsync().Result);
    }

    [Fact]
    public async Task OnActivateAsync_ShouldUpdatePendingList()
    {
        // Arrange
        const string child1Id = "myRequestId1";
        const string child2Id = "myRequestId2";
        
        var mockSchedulerState = GetMockSchedulerState();
        
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);
        
        // create a new image generator grain
        var imageGenGrain1 = new Mock<IImageGeneratorGrain>();
        var imageGenGrain2 = new Mock<IImageGeneratorGrain>();
        
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(child1Id, null))
            .Returns(imageGenGrain1.Object);
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(child2Id, null))
            .Returns(imageGenGrain2.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent1", child1Id, now);
        await schedulerGrain.Object.AddImageGenerationRequest("parent2", child2Id, now);

        await schedulerGrain.Object.TickAsync();
        
        // Act
        await schedulerGrain.Object.OnActivateAsync(new CancellationToken());
        await schedulerGrain.Object.ReportFailedImageGenerationRequestAsync(new RequestStatus
        {
            RequestId = child1Id,
            Status = RequestStatusEnum.Failed
        });
        await schedulerGrain.Object.ReportCompletedImageGenerationRequestAsync(new RequestStatus
        {
            RequestId = child2Id,
            Status = RequestStatusEnum.Completed
        });

        // Assert
        // Add your assertions here based on the expected behavior of the UpdateExpiredPendingRequestsToBlocked method
        Assert.Empty(schedulerGrain.Object.GetPendingImageGenerationRequestsAsync().Result);
        Assert.Single(schedulerGrain.Object.GetFailedImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetBlockedImageGenerationRequestsAsync().Result);
        Assert.Single(schedulerGrain.Object.GetCompletedImageGenerationRequestsAsync().Result);
    }
    
    // 1. get a request blocked
    // 2. remove the api key
    // 3. add a new api key
    // 4. requests should proceed with new api key
    [Fact]
    public async Task TickAsync_BlockedApiKey_RemoveBlockedApiKey_InsertNewApiKey_ShouldCompleteRequest()
    {
        // Arrange
        const string requestId = "myRequestId";
        const string apiKeyString = "apiKey2apiKey2";
        
        var mockSchedulerState = GetMockSchedulerState();
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent", requestId, now);
        
        // Act
        await schedulerGrain.Object.TickAsync();

        await schedulerGrain.Object.ReportFailedImageGenerationRequestAsync(new RequestStatus
        {
            RequestId = requestId,
            Status = RequestStatusEnum.Failed,
            RequestTimestamp = now,
            ErrorCode = ImageGenerationErrorCode.billing_quota_exceeded
        });
        
        await schedulerGrain.Object.TickAsync();
        
        // remove the api key
        var apiKey = mockSchedulerState.Object.State.ApiAccountInfoList[0].ApiKey;
        var removeKeys = new List<ApiKey>();
        removeKeys.Add(apiKey);
        await schedulerGrain.Object.RemoveApiKeys(removeKeys);
        
        // add a new api key
        await schedulerGrain.Object.AddApiKeys(new List<ApiKeyEntryDto>
        {
            new ApiKeyEntryDto
            {
                Email = "second@api.key",
                Tier = 1,
                MaxQuota = 2,
                ApiKey = new ApiKeyDto
                {
                    ApiKeyString = apiKeyString,
                    ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI.ToString(),
                    Url = ""
                }
            }
        });
        
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow.AddSeconds(3));
        
        await schedulerGrain.Object.TickAsync();

        // Assert
        var result = schedulerGrain.Object.GetPendingImageGenerationRequestsAsync().Result;
        Assert.Equal(apiKeyString.Substring(0, apiKeyString.Length/2), result[0].ApiKey?.ApiKeyString);
    }
    
    //1. use an invalid azure api key
    //2. generate image
    //3. report invalid api key
    //4. remove the invalid api key
    //5. there are no more api key, request should remain in failed list
    [Fact]
    public async Task TickAsync_InvalidApiKey_Generate_ReportInvalidApiKey_RemoveInvalidApiKey_Tick()
    {
        // Arrange
        const string requestId = "myRequestId";
        const string apiKeyString = "apiKey2apiKey2";
        
        var mockSchedulerState = GetMockSchedulerState();
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent", requestId, now);
        
        // Act
        await schedulerGrain.Object.TickAsync();

        await schedulerGrain.Object.ReportFailedImageGenerationRequestAsync(new RequestStatus
        {
            RequestId = requestId,
            Status = RequestStatusEnum.Failed,
            RequestTimestamp = now,
            ErrorCode = ImageGenerationErrorCode.invalid_api_key
        });
        
        await schedulerGrain.Object.TickAsync();
        
        // remove the api key
        var apiKey = mockSchedulerState.Object.State.ApiAccountInfoList[0].ApiKey;
        var removeKeys = new List<ApiKey>();
        removeKeys.Add(apiKey);
        await schedulerGrain.Object.RemoveApiKeys(removeKeys);
        
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow.AddSeconds(3));
        await schedulerGrain.Object.TickAsync();
        
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow.AddSeconds(6));
        await schedulerGrain.Object.TickAsync();

        // Assert
        Assert.Single(schedulerGrain.Object.GetFailedImageGenerationRequestsAsync().Result);
    }
    
    /*
    [Fact]
    public async Task TickAsync_ShouldCleanUpExpiredCompletedRequests()
    {
        // Arrange
        var mockMasterTrackerState = new Mock<IPersistentState<SchedulerState>>();
        var mockLogger = new Mock<ILogger<SchedulerGrain>>();
        var mockReminderRegistry = new Mock<IReminderRegistry>();
        var schedulerGrain = new SchedulerGrain(mockMasterTrackerState.Object, mockReminderRegistry.Object, mockLogger.Object);

        // Act
        await schedulerGrain.TickAsync();

        // Assert
        // Add your assertions here based on the expected behavior of the CleanUpExpiredCompletedRequests method
    }

    [Fact]
    public async Task TickAsync_ShouldProcessRequests_WhenApiKeysAvailableAndQuotaAvailable()
    {
        // Arrange
        var mockMasterTrackerState = new Mock<IPersistentState<SchedulerState>>();
        var mockLogger = new Mock<ILogger<SchedulerGrain>>();
        var mockReminderRegistry = new Mock<IReminderRegistry>();
        var schedulerGrain = new SchedulerGrain(mockMasterTrackerState.Object, mockReminderRegistry.Object, mockLogger.Object);

        // Act
        await schedulerGrain.TickAsync();

        // Assert
        // Add your assertions here based on the expected behavior of the ProcessRequest method
    }

    [Fact]
    public async Task TickAsync_ShouldNotProcessRequests_WhenNoApiKeysAvailable()
    {
        // Arrange
        var mockMasterTrackerState = new Mock<IPersistentState<SchedulerState>>();
        var mockLogger = new Mock<ILogger<SchedulerGrain>>();
        var mockReminderRegistry = new Mock<IReminderRegistry>();
        var schedulerGrain = new SchedulerGrain(mockMasterTrackerState.Object, mockReminderRegistry.Object, mockLogger.Object);

        // Act
        await schedulerGrain.TickAsync();

        // Assert
        // Add your assertions here based on the expected behavior of the TickAsync method when no API keys are available
    }

    [Fact]
    public async Task TickAsync_ShouldNotProcessRequests_WhenNoQuotaAvailable()
    {
        // Arrange
        var mockMasterTrackerState = new Mock<IPersistentState<SchedulerState>>();
        var mockLogger = new Mock<ILogger<SchedulerGrain>>();
        var mockReminderRegistry = new Mock<IReminderRegistry>();
        var schedulerGrain = new SchedulerGrain(mockMasterTrackerState.Object, mockReminderRegistry.Object, mockLogger.Object);

        // Act
        await schedulerGrain.TickAsync();

        // Assert
        // Add your assertions here based on the expected behavior of the TickAsync method when no quota is available
    }

    [Fact]
    public async Task TickAsync_ShouldRemoveProcessedRequests_WhenCalled()
    {
        // Arrange
        var mockMasterTrackerState = new Mock<IPersistentState<SchedulerState>>();
        var mockLogger = new Mock<ILogger<SchedulerGrain>>();
        var mockReminderRegistry = new Mock<IReminderRegistry>();
        var schedulerGrain = new SchedulerGrain(mockMasterTrackerState.Object, mockReminderRegistry.Object, mockLogger.Object);

        // Act
        await schedulerGrain.TickAsync();

        // Assert
        // Add your assertions here based on the expected behavior of the RemoveProcessedRequests method
    }

    [Fact]
    public async Task TickAsync_ShouldNotRemoveUnprocessedRequests_WhenCalled()
    {
        // Arrange
        var mockMasterTrackerState = new Mock<IPersistentState<SchedulerState>>();
        var mockLogger = new Mock<ILogger<SchedulerGrain>>();
        var mockReminderRegistry = new Mock<IReminderRegistry>();
        var schedulerGrain = new SchedulerGrain(mockMasterTrackerState.Object, mockReminderRegistry.Object, mockLogger.Object);

        // Act
        await schedulerGrain.TickAsync();

        // Assert
        // Add your assertions here based on the expected behavior of the TickAsync method when there are unprocessed requests
    }

    [Fact]
    public async Task TickAsync_ShouldLogError_WhenUnremovedRequestsExist()
    {
        // Arrange
        var mockMasterTrackerState = new Mock<IPersistentState<SchedulerState>>();
        var mockLogger = new Mock<ILogger<SchedulerGrain>>();
        var mockReminderRegistry = new Mock<IReminderRegistry>();
        var schedulerGrain = new SchedulerGrain(mockMasterTrackerState.Object, mockReminderRegistry.Object, mockLogger.Object);

        // Act
        await schedulerGrain.TickAsync();

        // Assert
        mockLogger.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => string.Equals("Requests not found in failed or started requests", v.ToString(), StringComparison.InvariantCultureIgnoreCase)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [Fact]
    public async Task TickAsync_ShouldNotLogError_WhenNoUnremovedRequestsExist()
    {
        // Arrange
        var mockMasterTrackerState = new Mock<IPersistentState<SchedulerState>>();
        var mockLogger = new Mock<ILogger<SchedulerGrain>>();
        var mockReminderRegistry = new Mock<IReminderRegistry>();
        var schedulerGrain = new SchedulerGrain(mockMasterTrackerState.Object, mockReminderRegistry.Object, mockLogger.Object);

        // Act
        await schedulerGrain.TickAsync();

        // Assert
        mockLogger.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => string.Equals("Requests not found in failed or started requests", v.ToString(), StringComparison.InvariantCultureIgnoreCase)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Never);
    }

    [Fact]
    public async Task TickAsync_ShouldPruneRequestsWithMaxAttempts()
    {
        // Arrange
        var mockMasterTrackerState = new Mock<IPersistentState<SchedulerState>>();
        var mockLogger = new Mock<ILogger<SchedulerGrain>>();
        var mockReminderRegistry = new Mock<IReminderRegistry>();
        var schedulerGrain = new SchedulerGrain(mockMasterTrackerState.Object, mockReminderRegistry.Object, mockLogger.Object);

        // Act
        await schedulerGrain.TickAsync();

        // Assert
        // Add your assertions here based on the expected behavior of the PruneRequestsWithMaxAttempts method
    }

    [Fact]
    public async Task TickAsync_ShouldNotPruneRequestsWithoutMaxAttempts()
    {
        // Arrange
        var mockMasterTrackerState = new Mock<IPersistentState<SchedulerState>>();
        var mockLogger = new Mock<ILogger<SchedulerGrain>>();
        var mockReminderRegistry = new Mock<IReminderRegistry>();
        var schedulerGrain = new SchedulerGrain(mockMasterTrackerState.Object, mockReminderRegistry.Object, mockLogger.Object);

        // Act
        await schedulerGrain.TickAsync();

        // Assert
        // Add your assertions here based on the expected behavior of the PruneRequestsWithMaxAttempts method
    }*/
    
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
                MaxQuota = 2
            }
        };
                
        
        var mockSchedulerState = new Mock<IPersistentState<SchedulerState>>();
        mockSchedulerState.SetupGet(o => o.State).Returns(state);
        return mockSchedulerState;
    }
}