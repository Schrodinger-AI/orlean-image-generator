using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Grains;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddSingleton<IClusterClient>(serviceProvider =>
        {
            var client = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansService";
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ISchrodingerGrain).Assembly).WithReferences())
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug); // Set log level to Debug for more detailed logging
                })
                .Build();

            client.Connect().Wait();
            return client;
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.Use(async (context, next) =>
        {
            logger.LogInformation("Received request: {Url}", context.Request.Path);
            await next.Invoke();
            logger.LogInformation("Finished processing request: {Url}", context.Request.Path);
        });

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}