using Grains;
using Grains.AzureOpenAI;
using Grains.DalleOpenAI;
using Grains.ImageGenerator;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Json;
using Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans.Serialization;

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
                .UseOrleans(c =>
                {
                    c.UseAdoNetClustering(options =>
                        {
                            options.Invariant = "MySql.Data.MySqlClient";
                            options.ConnectionString = connectionString;
                        })
                        .ConfigureEndpoints(siloPort: siloPort, gatewayPort: gatewayPort)
                        .ConfigureServices(services =>
                        {
                            services.Configure<ImageSettings>(configuration.GetSection("ImageSettings"));
                            services.AddTransient<IImageGenerator, DalleOpenAIImageGenerator>();
                            services.AddTransient<IImageGenerator, AzureOpenAIImageGenerator>();
                            services.AddSerializer(serializerBuilder =>
                            {
                                serializerBuilder.AddNewtonsoftJsonSerializer(
                                        isSupported: type => type.Namespace.StartsWith("Grains"))
                                    .AddNewtonsoftJsonSerializer(
                                        isSupported: type => type.Namespace.StartsWith("Shared"));
                            });
                        })
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "OrleansImageGeneratorService";
                        })
                        .AddAdoNetGrainStorage(Constants.MySqlSchrodingerImageStore,
                            (Action<AdoNetGrainStorageOptions>)(options =>
                            {
                                options.Invariant = "MySql.Data.MySqlClient";
                                options.ConnectionString = connectionString;
                            }))
                        .ConfigureLogging(logging => logging.AddSerilog())
                        .UseDashboard(options =>
                        {
                            options.CounterUpdateIntervalMs = 10000;
                        });
                })
                .UseConsoleLifetime()
                .Build();

            await host.StartAsync();

            await Task.Delay(-1);
        }
    }
}