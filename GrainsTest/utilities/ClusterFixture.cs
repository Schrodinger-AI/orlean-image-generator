using System.Reflection;
using Schrodinger.Backend.Grains.Constants;
using Schrodinger.Backend.Grains.types;
using Schrodinger.Backend.Grains.usage_tracker;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans.Runtime;
using Orleans.TestingHost;
using Orleans.Timers;
using Schrodinger.Backend.Abstractions.ApiKeys;
using Schrodinger.Backend.Abstractions.Constants;
using Xunit;

namespace GrainsTest.utilities;

public sealed class ClusterFixture : IDisposable
{
    public static FakeTimerRegistry FakeTimerRegistry;
    public static FakeReminderRegistry FakeReminderRegistry;
    
    public TestCluster Cluster { get; } = new TestClusterBuilder()
        .AddSiloBuilderConfigurator<SiloConfigurator>()
        .Build();

    public ClusterFixture() => Cluster.Deploy();

    void IDisposable.Dispose() => Cluster.StopAllSilos();
    
    public static async Task Tick()
    {
        var timerEntries = FakeTimerRegistry.GetAll();
        foreach (var fakeTimerEntry in timerEntries)
        {
            await fakeTimerEntry.TickAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ClusterCollection : ICollectionFixture<ClusterFixture>
{
    public const string Name = nameof(ClusterCollection);
}

public class SiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder) => siloBuilder
        .AddMemoryGrainStorage(GrainConstants.MySqlSchrodingerImageStore)
        .ConfigureServices(services =>
        {
            var state = new SchedulerState();
                
            state.ApiAccountInfoList = new List<APIAccountInfo>
            {
                new APIAccountInfo
                {
                    ApiKey = new ApiKey
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
                
            var mockMapper = new Mock<IAttributeToFactoryMapper<PersistentStateAttribute>>();
            mockMapper.Setup(o => o.GetFactory(It.IsAny<ParameterInfo>(), It.IsAny<PersistentStateAttribute>())).Returns(context => mockSchedulerState.Object);
            
            ClusterFixture.FakeTimerRegistry = new FakeTimerRegistry();
            ClusterFixture.FakeReminderRegistry = new FakeReminderRegistry();
            services.AddSingleton(ClusterFixture.FakeTimerRegistry);
            services.AddSingleton(ClusterFixture.FakeReminderRegistry);
            services.AddSingleton(mockMapper.Object);
            services.AddSingleton<ITimerRegistry>(_ => _.GetService<FakeTimerRegistry>());
            services.AddSingleton<IReminderRegistry>(_ => _.GetService<FakeReminderRegistry>());
        });
}