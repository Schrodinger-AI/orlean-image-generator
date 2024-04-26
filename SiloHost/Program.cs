using Grains;
using Grains.AzureOpenAI;
using Grains.DalleOpenAI;
using Grains.ImageGenerator;
using Grains.utilities;
using Orleans;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Serialization;
using OrleansDashboard;
using ProofService.interfaces;
using Serilog;
using Serilog.Formatting.Json;
using Shared;
using SiloHost.startup;
using TimeProvider = Grains.utilities.TimeProvider;

namespace SiloHost
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(new JsonFormatter())
                .WriteTo.File(new JsonFormatter(), "logs/SiloHostLog-.log",
                    rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3, fileSizeLimitBytes: 2L * 1024 * 1024 * 1024)
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
                    .UseKubernetesHosting()
                    .UseMongoDBClient(connectionString)
                    .UseMongoDBClustering(options =>
                    {
                        options.DatabaseName = configuration.GetValue<string>("MongoDataBase");
                        options.CreateShardKeyForCosmos = false;
                        options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                    })
                    .UseMongoDBReminders(options =>
                    {
                        options.DatabaseName = configuration.GetValue<string>("MongoDataBase");
                        options.CreateShardKeyForCosmos = false;
                    })
                    .AddReminders()
                    .Configure<JsonGrainStateSerializerOptions>(options => options.ConfigureJsonSerializerSettings =
                        settings =>
                        {
                            settings.NullValueHandling = NullValueHandling.Include;
                            settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                            settings.DefaultValueHandling = DefaultValueHandling.Populate;
                        })
                    .AddMongoDBGrainStorage("MySqlSchrodingerImageStore", options =>
                    {
                        options.DatabaseName = configuration.GetValue<string>("MongoDataBase");
                        options.CreateShardKeyForCosmos = false;
                    })
                    .ConfigureServices(services =>
                    {
                        services.Configure<ImageSettings>(configuration.GetSection("ImageSettings"));
                        services.AddScoped<IDalleOpenAIImageGenerator, DalleOpenAIImageGenerator>();
                        services.AddScoped<IAzureOpenAIImageGenerator, AzureOpenAIImageGenerator>();
                        services.AddSingleton<TimeProvider, DefaultTimeProvider>();
                        services.Configure<ZkProverSetting>(configuration.GetSection("ProverSetting"));
                        services.Configure<ContractClient>(configuration.GetSection("ContractClient"));
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "OrleansImageGeneratorService";
                    })
                    .ConfigureEndpoints(siloPort, gatewayPort)
                    .ConfigureLogging(logging => logging.AddSerilog())
                    .UseDashboard(options =>
                    {
                        options.CounterUpdateIntervalMs = 10000;
                    })
                    .AddStartupTask<SchedulerGrainStartupTask>()
                )
                .UseConsoleLifetime()
                .Build();

            await host.StartAsync();

            await Task.Delay(-1);
        }
    }
}