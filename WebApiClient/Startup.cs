using Orleans;
using Orleans.Hosting;
using Orleans.Configuration;
using Grains;
using Microsoft.OpenApi.Models;
using System.Net;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddSingleton<IClusterClient>(serviceProvider =>
        {
            var client = new ClientBuilder()
                 .UseAdoNetClustering(options =>
                {
                    options.Invariant = "MySql.Data.MySqlClient";
                    options.ConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
                    Console.WriteLine("Connection string: " + options.ConnectionString);
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansService";
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ImageGeneratorGrain).Assembly).WithReferences())
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug); // Set log level to Debug for more detailed logging
                })
                .Build();

            client.Connect().Wait();
            return client;
        });
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

            // Set the comments path for the Swagger JSON and UI.
            //var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            //var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            //c.IncludeXmlComments(xmlPath);
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseDeveloperExceptionPage();

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