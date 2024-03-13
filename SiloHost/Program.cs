using Grains;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Json;
using Shared;

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

            var host = new SiloHostBuilder()
                .UseLocalhostClustering()
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<TraitConfigOptions>(hostContext.Configuration.GetSection("TraitConfig"));
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansImageGeneratorService";
                })
                .UseAdoNetClustering(options =>
                {
                    options.Invariant = "MySql.Data.MySqlClient";
                    options.ConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
                    Console.WriteLine("Connection string: " + options.ConnectionString);
                })
                .AddAdoNetGrainStorage(Grains.Constants.MySqlSchrodingerImageStore, options =>
                {
                    options.Invariant = "MySql.Data.MySqlClient";
                    options.ConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
                    options.UseJsonFormat = false;
                    Console.WriteLine("Connection string: " + options.ConnectionString);
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