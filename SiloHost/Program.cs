using Grains;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace SiloHost
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new SiloHostBuilder()
                .UseLocalhostClustering()
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<TraitConfigOptions>(hostContext.Configuration.GetSection("TraitConfig"));
                    services.AddSingleton<PromptBuilder>();
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
                .AddAdoNetGrainStorage(Constants.MySqlSchrodingerImageStore, options =>
                {
                    options.Invariant = "MySql.Data.MySqlClient";
                    options.ConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
                    options.UseJsonFormat = false;
                    Console.WriteLine("Connection string: " + options.ConnectionString);
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ImageGeneratorGrain).Assembly).WithReferences())
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug); // Set log level to Debug for more detailed logging
                })
                .UseDashboard(options => { })
                .Build();

            await host.StartAsync();

            await Task.Delay(-1);
        }
    }
}