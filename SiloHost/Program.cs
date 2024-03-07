using Grains;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace SiloHost
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new SiloHostBuilder()
                .UseLocalhostClustering()
                //.UseDashboard(options => { })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansService";
                })
                    .UseAdoNetClustering(options =>
                    {
                        options.Invariant = "MySql.Data.MySqlClient";
                        options.ConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
                        Console.WriteLine("Connection string: " + options.ConnectionString);
                    })
                    //.ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(UserGrain).Assembly).WithReferences())
                    .AddAdoNetGrainStorage(Constants.MySqlSchrodingerImageStore, options =>
                    {
                        options.Invariant = "MySql.Data.MySqlClient";
                        options.ConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
                        options.UseJsonFormat = false;
                        Console.WriteLine("Connection string: " + options.ConnectionString);
                    })
                //.AddMemoryGrainStorage("PubSubStore")
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ISchrodingerGrain).Assembly).WithReferences())
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug); // Set log level to Debug for more detailed logging
                }).Build();

            await host.StartAsync();

            await Task.Delay(-1);
        }
    }
}