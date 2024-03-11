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
        var grain = new SchedulerGrain(state, new FakeTimerRegistry(), Mock.Of<ILogger<SchedulerGrain>>());

        // set a new value
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await grain.AddImageGenerationRequest("myRequestId", "myAccountInfo", now);

        // assert the state was saved
        Mock.Get(state).Verify(_ => _.WriteStateAsync());
        Assert.That(state.State.StartedImageGenerationRequests, Has.Count.EqualTo(1));
    }
    
    [Test]
    public async Task Scheduler_Test()
    {
        // mock a persistent state item
        var state = Mock.Of<IPersistentState<SchedulerState>>(_ => _.State == Mock.Of<SchedulerState>());
        state.State.StartedImageGenerationRequests = new Dictionary<string, RequestAccountUsageInfo>();
        state.State.FailedImageGenerationRequests = new Dictionary<string, RequestAccountUsageInfo>();
        state.State.CompletedImageGenerationRequests = new Dictionary<string, RequestAccountUsageInfo>();
        state.State.PendingImageGenerationRequests = new Dictionary<string, RequestAccountUsageInfo>();
        state.State.ApiAccountInfoList = new List<APIAccountInfo> { new APIAccountInfo { ApiKey = "ApiKey1", MaxQuota = 5 }, new APIAccountInfo { ApiKey = "ApiKey2", MaxQuota = 5 } };

        var mockTimerRegistry = new FakeTimerRegistry();
        // create a new grain - we dont mock the grain here because we do not need to override any base methods
        var grain = new SchedulerGrain(state, mockTimerRegistry, Mock.Of<ILogger<SchedulerGrain>>());

        // set a new value
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await grain.AddImageGenerationRequest("myRequestId", "myAccountInfo", now);

        while (mockTimerRegistry.GetAll().Count() < 2)
        {
        }
        
        // assert the state was saved
        Mock.Get(state).Verify(_ => _.WriteStateAsync());
        Assert.That(state.State.PendingImageGenerationRequests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Test1()
    {
        var state = new SchedulerState();
        
        var mockSchedulerState = new Mock<IPersistentState<SchedulerState>>();
        mockSchedulerState.SetupGet(o => o.State).Returns(state);
        
        /*
        var mockMapper = new Mock<IAttributeToFactoryMapper<PersistentStateAttribute>>();
        mockMapper.Setup(o => o.GetFactory(It.IsAny<ParameterInfo>(), It.IsAny<PersistentStateAttribute>())).Returns(context => mockSchedulerState.Object);

        silo.AddService(mockMapper.Object);*/
        
        var builder = new TestClusterBuilder();
        var cluster = builder.AddSiloBuilderConfigurator<SiloConfigurator>().Build();
        cluster.Deploy();

        var scheduler = cluster.GrainFactory.GetGrain<ISchedulerGrain>("SchedulerGrain");
        
        var now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        await scheduler.AddImageGenerationRequest("myRequestId", "myAccountInfo", now);

        Assert.Equals(1, scheduler.GetStartedImageGenerationRequestsAsync().Result.Count);
        
        cluster.StopAllSilos();
    }
    
    private class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder) => siloBuilder
            .AddMemoryGrainStorage(Constants.MySqlSchrodingerImageStore)
            .ConfigureServices(static services =>
            {
                services.AddSingleton<FakeTimerRegistry>();
                services.AddSingleton<ITimerRegistry>(_ => _.GetService<FakeTimerRegistry>());
            });
    }
}