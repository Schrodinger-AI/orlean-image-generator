using Grains;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Json;
using Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

// ReSharper disable ComplexConditionExpression

namespace SiloHost
{
    class Program
    {
        static async Task Main(string[] args)
        {

            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    hostingContext.HostingEnvironment.EnvironmentName =
                        hostingContext.Configuration.GetValue("ASPNETCORE_ENVIRONMENT", "Production") ??
                        "Production";
                })
                .UseOrleans((ctx, siloBuilder) =>
                {
                    // NOTE: Should use ctx.HostingEnvironment.EnvironmentName instead of Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                    // But don't know why it's not working
                    var environment = ctx.Configuration.GetValue("ASPNETCORE_ENVIRONMENT", "Production") ??
                                      "Production";

                    var siloPort = ctx.Configuration.GetValue<int>("SiloPort");

                    if (siloPort == 0)
                    {
                        throw new Exception("SiloPort must be non-zero.");
                    }

                    var gatewayPort = ctx.Configuration.GetValue<int>("GatewayPort");

                    if (gatewayPort == 0)
                    {
                        throw new Exception("GatewayPort must be non-zero.");
                    }

                    if (environment == "Development")
                    {
                        siloBuilder.UseLocalhostClustering();
                        siloBuilder.AddMemoryGrainStorage("definitions")
                            .ConfigureEndpoints(siloPort: siloPort, gatewayPort: gatewayPort,
                                listenOnAnyHostAddress: true);
                    }
                    else
                    {
                        var connectionString = ctx.Configuration.GetValue<string>("ConnectionString");

                        if (string.IsNullOrEmpty(connectionString))
                        {
                            throw new Exception("ConnectionString must be non-empty.");
                        }

                        siloBuilder.UseAdoNetClustering(options =>
                            {
                                options.Invariant = "MySql.Data.MySqlClient";
                                options.ConnectionString = connectionString;
                            })
                            .AddAdoNetGrainStorage(Grains.Constants.MySqlSchrodingerImageStore, options =>
                            {
                                options.Invariant = "MySql.Data.MySqlClient";
                                options.ConnectionString = connectionString;
                                options.UseJsonFormat = false;
                            })
                            .ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000)
                            .ConfigureLogging(logging => logging.AddSerilog());

                        Log.Logger = new LoggerConfiguration()
                            .WriteTo.Console(new JsonFormatter())
                            .WriteTo.File(new JsonFormatter(), "logs/SiloHostLog-.log",
                                rollingInterval: RollingInterval.Hour)
                            .CreateLogger();
                    }

                    siloBuilder
                        .ConfigureApplicationParts(parts =>
                            parts.AddApplicationPart(typeof(ImageGeneratorGrain).Assembly).WithReferences())
                        .ConfigureServices((hostContext, services) =>
                        {
                            services.Configure<ImageSettings>(ctx.Configuration.GetSection("ImageSettings"));
                        })
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "OrleansImageGeneratorService";
                        });
                });


            await hostBuilder.RunConsoleAsync();
        }
    }
}