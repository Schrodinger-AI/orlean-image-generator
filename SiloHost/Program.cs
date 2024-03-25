using Grains;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Json;
using Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Orleans.Providers.MongoDB.Configuration;

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

            var host = new SiloHostBuilder()
                .ConfigureEndpoints(siloPort: siloPort, gatewayPort: gatewayPort)
                .UseMongoDBClient(configuration.GetValue<string>("MongoDBClient"))
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configuration.GetValue<string>("MongoDataBase");;
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .AddMongoDBGrainStorage("MySqlSchrodingerImageStore",(MongoDBGrainStorageOptions op) =>
                {
                    op.CollectionPrefix = "GrainStorage";
                    op.DatabaseName = configuration.GetValue<string>("MongoDataBase");
                    op.ConfigureJsonSerializerSettings = jsonSettings =>
                    {
                        // jsonSettings.ContractResolver = new PrivateSetterContractResolver();
                        jsonSettings.NullValueHandling = NullValueHandling.Include;
                        jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;
                        jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    };
                })
                .UseMongoDBReminders(options =>
                {
                    options.DatabaseName = configuration.GetValue<string>("MongoDataBase");
                    options.CreateShardKeyForCosmos = false;
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<ImageSettings>(configuration.GetSection("ImageSettings"));
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansImageGeneratorService";
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ImageGeneratorGrain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddSerilog())
                //.UseDashboard(options => { })
                .Build();

            await host.StartAsync();

            await Task.Delay(-1);
        }
    }
}