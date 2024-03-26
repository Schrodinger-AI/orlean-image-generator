using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Serilog;
using Serilog.Formatting.Json;

namespace SiloHost
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(new JsonFormatter())
                .WriteTo.File(new JsonFormatter(), "logs/SiloHostLog-.log", rollingInterval: RollingInterval.Hour)
                .CreateLogger();

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var configuration = builder.Build();

            var siloPort = configuration.GetValue<int>("SiloPort");

            if (siloPort == 0)
            {
                throw new Exception("SiloPort must be non-zero.");
            }

            var gatewayPort = configuration.GetValue<int>("GatewayPort");

            if (gatewayPort == 0)
            {
                throw new Exception("GatewayPort must be non-zero.");
            }

            var connectionString = configuration.GetValue<string>("ConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("ConnectionString must be non-empty.");
            }

            var host = new HostBuilder()
                .UseOrleans((ctx, siloBuilder) => siloBuilder
                    .UseMongoDBClient(connectionString)
                    .UseMongoDBClustering(options =>
                    {
                        options.DatabaseName = configuration.GetValue<string>("MongoDataBase");
                        options.CreateShardKeyForCosmos = false;
                        options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                    })
                    // todo ??
                    // .AddStartupTask<SchedulerGrainStartupTask>()
                    .UseMongoDBReminders(options =>
                    {
                        options.DatabaseName = configuration.GetValue<string>("MongoDataBase");
                        options.CreateShardKeyForCosmos = false;
                    })
                    // .AddMemoryStreams<DefaultMemoryMessageBodySerializer>("OrleansTestStream")
                    .Configure<JsonGrainStateSerializerOptions>(options => options.ConfigureJsonSerializerSettings =
                        settings =>
                        {
                            settings.NullValueHandling = NullValueHandling.Include;
                            settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                            settings.DefaultValueHandling = DefaultValueHandling.Populate;
                        })
                    // .ConfigureServices(services => services
                    // .AddKeyedSingleton<IGrainStateSerializer, BinaryGrainStateSerializer>(ProviderConstants.DEFAULT_PUBSUB_PROVIDER_NAME)
                    // .AddKeyedSingleton<IGrainStateSerializer, BsonGrainStateSerializer>("MySqlSchrodingerImageStore"))
                    .AddMongoDBGrainStorage("MySqlSchrodingerImageStore", options =>
                    {
                        options.DatabaseName = configuration.GetValue<string>("MongoDataBase");
                        options.CreateShardKeyForCosmos = false;
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "OrleansImageGeneratorService";
                    })
                    .ConfigureEndpoints(IPAddress.Loopback, siloPort, gatewayPort)
                    .ConfigureLogging(logging => logging.AddSerilog())
                    .UseDashboard(options =>
                    {
                        options.CounterUpdateIntervalMs = 10000;
                    }))
                .UseConsoleLifetime()
                .Build();

            await host.StartAsync();

            await Task.Delay(-1);
        }
    }
}