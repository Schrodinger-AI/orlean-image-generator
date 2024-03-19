using System.Configuration;
using Orleans;
using Orleans.Hosting;
using Orleans.Configuration;
using Grains;
using Microsoft.OpenApi.Models;
using System.Net;
using Serilog;

// ReSharper disable ComplexConditionExpression

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }


    public void ConfigureServices(IServiceCollection services)
    {
        var environment = Configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT", "Production") ?? "Production";

        var clientBuilder = new ClientBuilder();
        if (environment == "Development")
        {
            clientBuilder.UseLocalhostClustering();
        }
        else
        {
            var connectionString = Configuration.GetValue<string>("ConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("ConnectionString must be non-empty.");
            }

            clientBuilder
                .UseAdoNetClustering(options =>
                {
                    options.Invariant = "MySql.Data.MySqlClient";
                    options.ConnectionString = connectionString;
                });
        }

        var client = clientBuilder
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "dev"; // TODO: Make this configurable
                options.ServiceId = "OrleansService";
            })
            .ConfigureApplicationParts(parts =>
                parts.AddApplicationPart(typeof(ImageGeneratorGrain).Assembly).WithReferences())
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug); // Set log level to Debug for more detailed logging
            })
            .Build();

        services.AddControllers();
        services.AddSingleton<IClusterClient>(serviceProvider =>
        {
            
            // TODO: Don't do connect and wait here
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

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}