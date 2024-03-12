using System.Reflection;
using Grains;
using Grains.types;
using Grains.usage_tracker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Timers;

namespace GrainsTest;
using Orleans.TestingHost;

public class ScheduleGrainTest
{
    [SetUp]
    public void Setup()
    {
    }
    
    [Test]
    public async Task AddImageGenerationRequest_State_Test()
    {
        // mock a persistent state item
        var state = Mock.Of<IPersistentState<SchedulerState>>(_ => _.State == Mock.Of<SchedulerState>());
        state.State.StartedImageGenerationRequests = new Dictionary<string, RequestAccountUsageInfo>();

        // create a new grain - we dont mock the grain here because we do not need to override any base methods
        var grain = new SchedulerGrain(state, Mock.Of<ILogger<SchedulerGrain>>());

        // set a new value
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await grain.AddImageGenerationRequest("parent", "myRequestId", now);

        // assert the state was saved
        Mock.Get(state).Verify(_ => _.WriteStateAsync());
        Assert.That(state.State.StartedImageGenerationRequests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Scheduler_Test()
    {
        var builder = new TestClusterBuilder();
        var cluster = builder.AddSiloBuilderConfigurator<SiloConfigurator>().Build();
        cluster.Deploy();

        var scheduler = cluster.GrainFactory.GetGrain<ISchedulerGrain>("SchedulerGrain");
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await scheduler.AddImageGenerationRequest("parent", "myRequestId", now);
        
        await Tick();

        Assert.That(scheduler.GetPendingImageGenerationRequestsAsync().Result, Has.Count.EqualTo(1));
        
        cluster.StopAllSilos();
    }
    
    [Test]
    public async Task Scheduler_FailRequest_Test()
    {
        var builder = new TestClusterBuilder();
        var cluster = builder.AddSiloBuilderConfigurator<SiloConfigurator>().Build();
        cluster.Deploy();

        var scheduler = cluster.GrainFactory.GetGrain<ISchedulerGrain>("SchedulerGrain");
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await scheduler.AddImageGenerationRequest("parent", "myRequestId", now);
        
        await Tick();

        await scheduler.ReportFailedImageGenerationRequestAsync(new RequestStatus
        {
            Message = "failed",
            RequestId = "myRequestId",
            Status = RequestStatusEnum.Failed
        });
        
        Assert.That(scheduler.GetFailedImageGenerationRequestsAsync().Result, Has.Count.EqualTo(1));
        
        cluster.StopAllSilos();
    }
    
    [Test]
    public async Task Scheduler_FailRequest_Reprocessed_Test()
    {
        var builder = new TestClusterBuilder();
        var cluster = builder.AddSiloBuilderConfigurator<SiloConfigurator>().Build();
        cluster.Deploy();

        var scheduler = cluster.GrainFactory.GetGrain<ISchedulerGrain>("SchedulerGrain");
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await scheduler.AddImageGenerationRequest("parent", "myRequestId", now);
        
        await Tick();

        await scheduler.ReportFailedImageGenerationRequestAsync(new RequestStatus
        {
            Message = "failed",
            RequestId = "myRequestId",
            Status = RequestStatusEnum.Failed
        });
        
        await Tick();
        
        Assert.That(scheduler.GetPendingImageGenerationRequestsAsync().Result, Has.Count.EqualTo(1));
        
        cluster.StopAllSilos();
    }
    
    [Test]
    public async Task Scheduler_CompletedRequest_Test()
    {
        var builder = new TestClusterBuilder();
        var cluster = builder.AddSiloBuilderConfigurator<SiloConfigurator>().Build();
        cluster.Deploy();

        var scheduler = cluster.GrainFactory.GetGrain<ISchedulerGrain>("SchedulerGrain");
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await scheduler.AddImageGenerationRequest("parent", "myRequestId", now);
        
        await Tick();

        await scheduler.ReportCompletedImageGenerationRequestAsync(new RequestStatus
        {
            Message = "Completed",
            RequestId = "myRequestId",
            Status = RequestStatusEnum.Completed
        });
        
        Assert.That(scheduler.GetPendingImageGenerationRequestsAsync().Result, Has.Count.EqualTo(0));
        Assert.That(scheduler.GetFailedImageGenerationRequestsAsync().Result, Has.Count.EqualTo(0));
        Assert.That(scheduler.GetStartedImageGenerationRequestsAsync().Result, Has.Count.EqualTo(0));
        
        cluster.StopAllSilos();
    }
    
    [Test]
    public async Task Scheduler_ProcessedMaxRequest_Test()
    {
        const string REQUEST_ID_1 = "myRequest1";
        const string REQUEST_ID_2 = "myRequest2";
        
        var builder = new TestClusterBuilder();
        var cluster = builder.AddSiloBuilderConfigurator<SiloConfigurator>().Build();
        cluster.Deploy();

        var scheduler = cluster.GrainFactory.GetGrain<ISchedulerGrain>("SchedulerGrain");
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await scheduler.AddImageGenerationRequest("parent1", REQUEST_ID_1, now);
        await scheduler.AddImageGenerationRequest("parent2", REQUEST_ID_2, now);
        
        await Tick();
        
        Assert.That(scheduler.GetPendingImageGenerationRequestsAsync().Result, Has.Count.EqualTo(2));
        
        cluster.StopAllSilos();
    }
    
    [Test]
    public async Task Scheduler_ExceedMaxRequest_Test()
    {
        const string REQUEST_ID_1 = "myRequest1";
        const string REQUEST_ID_2 = "myRequest2";
        const string REQUEST_ID_3 = "myRequest3";
        
        var builder = new TestClusterBuilder();
        var cluster = builder.AddSiloBuilderConfigurator<SiloConfigurator>().Build();
        cluster.Deploy();

        var scheduler = cluster.GrainFactory.GetGrain<ISchedulerGrain>("SchedulerGrain");
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await scheduler.AddImageGenerationRequest("parent1", REQUEST_ID_1, now);
        await scheduler.AddImageGenerationRequest("parent2", REQUEST_ID_2, now);
        await scheduler.AddImageGenerationRequest("parent3", REQUEST_ID_3, now);

        await Tick();
        
        Assert.That(scheduler.GetPendingImageGenerationRequestsAsync().Result, Has.Count.EqualTo(2));
        Assert.That(scheduler.GetStartedImageGenerationRequestsAsync().Result, Has.Count.EqualTo(1));
        
        cluster.StopAllSilos();
    }
    
    [Test]
    public async Task Scheduler_ExceedMaxRequest_ReprocessAfterOneMinute_Test()
    {
        const string REQUEST_ID_1 = "myRequest1";
        const string REQUEST_ID_2 = "myRequest2";
        const string REQUEST_ID_3 = "myRequest3";
        
        var builder = new TestClusterBuilder();
        var cluster = builder.AddSiloBuilderConfigurator<SiloConfigurator>().Build();
        cluster.Deploy();

        var scheduler = cluster.GrainFactory.GetGrain<ISchedulerGrain>("SchedulerGrain");
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await scheduler.AddImageGenerationRequest("parent1", REQUEST_ID_1, now);
        await scheduler.AddImageGenerationRequest("parent2", REQUEST_ID_2, now);
        await scheduler.AddImageGenerationRequest("parent3", REQUEST_ID_3, now);

        await Tick();
        
        Assert.That(scheduler.GetPendingImageGenerationRequestsAsync().Result, Has.Count.EqualTo(2));
        Assert.That(scheduler.GetStartedImageGenerationRequestsAsync().Result, Has.Count.EqualTo(1));
        
        cluster.StopAllSilos();
    }
    

    private static async Task Tick()
    {
        var timerEntries = FakeTimerRegistry.GetAll();
        foreach (var fakeTimerEntry in timerEntries)
        {
            await fakeTimerEntry.TickAsync();
        }
    }

    public static FakeTimerRegistry FakeTimerRegistry;
    
    private class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder) => siloBuilder
            .AddMemoryGrainStorage(Constants.MySqlSchrodingerImageStore)
            .ConfigureServices(services =>
            {
                var state = new SchedulerState();
                
                state.ApiAccountInfoList = new List<APIAccountInfo>
                {
                    new APIAccountInfo
                    {
                        ApiKey = "apiKey1",
                        Description = "mocked api key 1",
                        Email = "mock@mock.com",
                        Tier = 0,
                        MaxQuota = 2
                    }
                };
                
        
                var mockSchedulerState = new Mock<IPersistentState<SchedulerState>>();
                mockSchedulerState.SetupGet(o => o.State).Returns(state);
                
                var mockMapper = new Mock<IAttributeToFactoryMapper<PersistentStateAttribute>>();
                mockMapper.Setup(o => o.GetFactory(It.IsAny<ParameterInfo>(), It.IsAny<PersistentStateAttribute>())).Returns(context => mockSchedulerState.Object);
                
                FakeTimerRegistry = new FakeTimerRegistry();
                
                services.AddSingleton(FakeTimerRegistry);
                services.AddSingleton(mockMapper.Object);
                services.AddSingleton<ITimerRegistry>(_ => _.GetService<FakeTimerRegistry>());
            });
    }
}