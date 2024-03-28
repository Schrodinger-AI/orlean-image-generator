using Grains.types;
using Grains.usage_tracker;
using GrainsTest.utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans.Runtime;
using Orleans.Timers;
using Shared;
using Xunit;
using TimeProvider = Grains.utilities.TimeProvider;

namespace GrainsTest;
using Orleans.TestingHost;

[Collection(ClusterCollection.Name)]
public class SchedulerGrainTest(ClusterFixture fixture)
{
    private const int DEFAULT_MAX_QUOTA = 2;
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
    public async Task TickAsync_ShouldUpdateApiUsageStatus_AfterReportedBillingQuotaExceeded_Reactivated()
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
        
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow.AddSeconds(4));

        await schedulerGrain.Object.TickAsync();
        
        // Assert
        // GetApiKeysUsageInfo should return a dictionary with a count of one
        var apiKeys = schedulerGrain.Object.GetApiKeysUsageInfo().Result.ToList();
        Assert.Equal(ApiKeyStatus.Active, apiKeys[0].Value.Status);
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

        var apiKeyToTest = new ApiKey(apiKeyString, ImageGenerationServiceProvider.DalleOpenAI.ToString(), "");
        var apiKeyDtoToTest = new ApiKeyDto(apiKeyToTest);
        
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
        Assert.Equal(apiKeyDtoToTest.ApiKeyString, result[0].ApiKey?.ApiKeyString);
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
    
    [Fact]
    public async Task TickAsync_ShouldCleanUpExpiredCompletedRequests()
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
        
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow.AddSeconds(SchedulerGrain.CLEANUP_INTERVAL + 1));
        
        await schedulerGrain.Object.TickAsync();

        // Assert
        Assert.Empty(schedulerGrain.Object.GetPendingImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetFailedImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetBlockedImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetCompletedImageGenerationRequestsAsync().Result);
    }
    
    [Fact]
    public async Task TickAsync_ShouldNotCleanUpExpiredCompletedRequests()
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
        
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow.AddSeconds(SchedulerGrain.CLEANUP_INTERVAL));
        
        await schedulerGrain.Object.TickAsync();

        // Assert
        Assert.Empty(schedulerGrain.Object.GetPendingImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetFailedImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetBlockedImageGenerationRequestsAsync().Result);
        Assert.Single(schedulerGrain.Object.GetCompletedImageGenerationRequestsAsync().Result);
    }
    
    [Fact]
    public async Task TickAsync_ShouldProcessRequests_WhenApiKeysAvailableAndQuotaAvailable()
    {
        // Arrange
        const string apiKeyString = "apiKey1apiKey1";
        var apiKeyToTest = new ApiKey
        {
            ApiKeyString = apiKeyString,
            ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI,
            Url = "some_url"
        };
        var apiKeyDtoToTest = new ApiKeyDto(apiKeyToTest);
        var mockSchedulerState = GetMockSchedulerState(apiKeyToTest);

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

        // Assert
        var result = schedulerGrain.Object.GetPendingImageGenerationRequestsAsync().Result;
        Assert.Equal(apiKeyDtoToTest.ApiKeyString, result[0].ApiKey?.ApiKeyString);
    }
    
    [Fact]
    public async Task TickAsync_ShouldNotProcessRequests_WhenNoApiKeysAvailable()
    {
        // Arrange
        var state = new SchedulerState
        {
            ApiAccountInfoList = []
        };

        var mockSchedulerState = new Mock<IPersistentState<SchedulerState>>();
        mockSchedulerState.SetupGet(o => o.State).Returns(state);

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

        // Assert
        var result = schedulerGrain.Object.GetStartedImageGenerationRequestsAsync().Result;
        Assert.Null(result[0].ApiKey);
    }
    
    [Fact]
    public async Task TickAsync_ShouldNotProcessRequests_WhenNoQuotaAvailable()
    {
        // Arrange
        const string apiKeyString = "apiKey1apiKey1";
        var mockSchedulerState = GetMockSchedulerState(new ApiKey
        {
            ApiKeyString = apiKeyString,
            ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI,
            Url = "some_url"
        });

        var mockLogger = new Mock<ILogger<SchedulerGrain>>();
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, mockLogger.Object);
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent1", "myRequestId1", now);
        await schedulerGrain.Object.AddImageGenerationRequest("parent2", "myRequestId2", now);
        await schedulerGrain.Object.AddImageGenerationRequest("parent3", "myRequestId3", now);
        
        // Act
        await schedulerGrain.Object.TickAsync();

        // Assert
        // Add your assertions here based on the expected behavior of the TickAsync method when no quota is available
        Assert.Equal(2, schedulerGrain.Object.GetPendingImageGenerationRequestsAsync().Result.Count);
        Assert.Empty(schedulerGrain.Object.GetFailedImageGenerationRequestsAsync().Result);
        Assert.Empty(schedulerGrain.Object.GetBlockedImageGenerationRequestsAsync().Result);
        Assert.Single(schedulerGrain.Object.GetStartedImageGenerationRequestsAsync().Result);
        //verify that log warning is called once
        mockLogger.Verify(logger => logger.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => string.Equals($"[SchedulerGrain] No available API keys, will try again in the next scheduling", v.ToString(), StringComparison.InvariantCultureIgnoreCase)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [Fact]
    public async Task TickAsync_BlockedCall()
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

        await schedulerGrain.Object.ReportBlockedImageGenerationRequestAsync(new RequestStatus
        {
            RequestId = "myRequestId",
            Status = RequestStatusEnum.Failed,
            ErrorCode = ImageGenerationErrorCode.content_violation
        });
        
        await schedulerGrain.Object.TickAsync();
        
        // Assert
        Assert.Single(schedulerGrain.Object.GetBlockedImageGenerationRequestsAsync().Result);
    }
    
    [Fact]
    public async Task FlushAsync()
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
        await schedulerGrain.Object.FlushAsync();
        
        // Assert
        mockSchedulerState.Verify(state => state.WriteStateAsync(), Times.Once);
    }
    
    [Fact]
    public Task GetAllApiKeys()
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
        
        // Assert
        Assert.Single(schedulerGrain.Object.GetAllApiKeys().Result);
        
        return Task.CompletedTask;
    }
    
    [Fact]
    public async Task RemoveApiKeys()
    {
        // Arrange
        var apikey = new ApiKey
        {
            ApiKeyString = "apikey1apikey1",
            ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI,
            Url = "some_url"
        };
        var mockSchedulerState = GetMockSchedulerState(apikey);
        
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);

        var apiKeyList = new List<ApiKey>();
        apiKeyList.Add(apikey);
        await schedulerGrain.Object.RemoveApiKeys(apiKeyList);
        
        // Assert
        Assert.Empty(schedulerGrain.Object.GetAllApiKeys().Result);
    }
    
    [Fact]
    public Task IsOverloaded_NotOverloaded()
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

        // Assert
        Assert.False(schedulerGrain.Object.IsOverloaded().Result);
        
        return Task.CompletedTask;
    }
    
    [Fact]
    public async Task IsOverloaded_Overloaded()
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
        for(var i = 0; i < DEFAULT_MAX_QUOTA; i++)
        {
            await schedulerGrain.Object.AddImageGenerationRequest($"parent{i}", $"myRequestId{i}", now);
        }
        
        // Act
        await schedulerGrain.Object.TickAsync();

        // Assert
        Assert.True(schedulerGrain.Object.IsOverloaded().Result);
    }
    
    [Fact]
    public async Task GetImageGenerationStates()
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

        // Assert
        Assert.Single(schedulerGrain.Object.GetImageGenerationStates().Result["pendingRequests"]);
    }
    
    [Fact]
    public async Task ForceRequestExecution_BlockedRequest()
    {
        // Arrange
        var childId = "myRequestId";
        var mockSchedulerState = GetMockSchedulerState();
        
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent", childId, now);
        
        // Act
        await schedulerGrain.Object.TickAsync();

        await schedulerGrain.Object.ReportBlockedImageGenerationRequestAsync(new RequestStatus
        {
            RequestId = childId,
            Status = RequestStatusEnum.Failed,
            ErrorCode = ImageGenerationErrorCode.content_violation
        });
        
        await schedulerGrain.Object.TickAsync();
        await schedulerGrain.Object.ForceRequestExecution(childId);
        
        // Assert
        Assert.Single(schedulerGrain.Object.GetStartedImageGenerationRequestsAsync().Result);
    }
    
    [Fact]
    public async Task ForceRequestExecution_NoRequest()
    {
        // Arrange
        var childId = "myRequestId";
        var mockSchedulerState = GetMockSchedulerState();
        
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent", childId, now);
        
        // Act
        await schedulerGrain.Object.TickAsync();
        
        await schedulerGrain.Object.ReportCompletedImageGenerationRequestAsync(new RequestStatus
        {
            RequestId = childId,
            Status = RequestStatusEnum.Completed,
            RequestTimestamp = now
        });
        
        await schedulerGrain.Object.ForceRequestExecution(childId);
        
        // Assert
        Assert.Empty(schedulerGrain.Object.GetStartedImageGenerationRequestsAsync().Result);
        Assert.Single(schedulerGrain.Object.GetCompletedImageGenerationRequestsAsync().Result);
    }
    
    [Fact]
    public async Task ForceRequestExecution_PendingRequest()
    {
        // Arrange
        var childId = "myRequestId";
        var mockSchedulerState = GetMockSchedulerState();
        
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await schedulerGrain.Object.AddImageGenerationRequest("parent", childId, now);
        
        // Act
        await schedulerGrain.Object.TickAsync();
        await schedulerGrain.Object.ForceRequestExecution(childId);
        
        // Assert
        Assert.Single(schedulerGrain.Object.GetStartedImageGenerationRequestsAsync().Result);
    }
    
    [Fact]
    public async Task AddApiKeys_NoDuplicates_AddMultipleApiKeys_ShouldAdd()
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

        var apiKeyList = new List<ApiKeyEntryDto>();
        apiKeyList.Add(new ApiKeyEntryDto
        {
            ApiKey = new ApiKeyDto
            {
                ApiKeyString = "apikey2apikey2",
                ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI.ToString(),
                Url = "some_url"
            },
            Email = "temp@temp.co",
            Tier = 1,
            MaxQuota = 5
        });
        apiKeyList.Add(new ApiKeyEntryDto
        {
            ApiKey = new ApiKeyDto
            {
                ApiKeyString = "apikey3apikey3",
                ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI.ToString(),
                Url = "some_url"
            },
            Email = "temp1@temp.co",
            Tier = 1,
            MaxQuota = 5
        });
        var response = await schedulerGrain.Object.AddApiKeys(apiKeyList);
        
        // Assert
        Assert.Equal(3, schedulerGrain.Object.GetAllApiKeys().Result.Count);
        Assert.True(response.IsSuccessful);
    }
    
    [Fact]
    public async Task AddApiKeys_NoDuplicates_AddOneApiKey_ShouldAdd()
    {
        // Arrange
        var apiKey1 = new ApiKey
        {
            ApiKeyString = "apiKey1apiKey1",
            ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI,
            Url = "some_url"
        };
        var apiKey2String = "apiKey2apiKey";
        var mockSchedulerState = GetMockSchedulerState(apiKey1);
        
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var response = await schedulerGrain.Object.AddApiKeys(new List<ApiKeyEntryDto>
        {
            new ApiKeyEntryDto
            {
                Email = "second@api.key",
                Tier = 1,
                MaxQuota = 2,
                ApiKey = new ApiKeyDto
                {
                    ApiKeyString = apiKey2String,
                    ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI.ToString(),
                    Url = ""
                }
            }
        });
        
        // Assert
        Assert.Equal(2, schedulerGrain.Object.GetAllApiKeys().Result.Count);
        Assert.True(response.IsSuccessful);
    }
    
    [Fact]
    public async Task AddApiKeys_OneDuplicate_AddOneApiKey_ShouldFail()
    {
        // Arrange
        var apiKeyString = "apiKey2apiKey2";
        var apiKey1 = new ApiKey
        {
            ApiKeyString = apiKeyString,
            ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI,
            Url = "some_url"
        };
        var mockSchedulerState = GetMockSchedulerState(apiKey1);
        
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var response = await schedulerGrain.Object.AddApiKeys(new List<ApiKeyEntryDto>
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
        
        // Assert
        Assert.Equal(1, schedulerGrain.Object.GetAllApiKeys().Result.Count);
        Assert.Equal("DUPLICATE_API_KEYS", response.Error);
        Assert.Single(response.DuplicateApiKeys);
        Assert.False(response.IsSuccessful);
    }
    
    [Fact]
    public async Task AddApiKeys_OneDuplicate_AddMultipleApiKeys_ShouldFail()
    {
        // Arrange
        var apiKeyString = "apiKey2apiKey2";
        var apiKey3String = "apiKey3apiKey3";
        var apiKey1 = new ApiKey
        {
            ApiKeyString = apiKeyString,
            ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI,
            Url = "some_url"
        };
        var mockSchedulerState = GetMockSchedulerState(apiKey1);
        
        var timeMock = new Mock<TimeProvider>();
        timeMock.SetupGet(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        var imageGenGrain = new Mock<IImageGeneratorGrain>();
        var schedulerGrain = new Mock<SchedulerGrain>(mockSchedulerState.Object, ClusterFixture.FakeReminderRegistry, timeMock.Object, Mock.Of<ILogger<SchedulerGrain>>());
        schedulerGrain
            .Setup(x => x.GrainFactory.GetGrain<IImageGeneratorGrain>(It.IsAny<string>(), null))
            .Returns(imageGenGrain.Object);
        
        var response = await schedulerGrain.Object.AddApiKeys(new List<ApiKeyEntryDto>
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
            },
            new ApiKeyEntryDto
            {
                Email = "second@api.key",
                Tier = 1,
                MaxQuota = 2,
                ApiKey = new ApiKeyDto
                {
                    ApiKeyString = apiKey3String,
                    ServiceProvider = ImageGenerationServiceProvider.DalleOpenAI.ToString(),
                    Url = ""
                }
            }
        });
        
        // Assert
        Assert.Equal(2, schedulerGrain.Object.GetAllApiKeys().Result.Count);
        Assert.Single(response.DuplicateApiKeys);
        Assert.True(response.IsSuccessful);
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
}