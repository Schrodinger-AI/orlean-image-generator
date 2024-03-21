using Orleans.Configuration;
using Serilog;
using Serilog.Formatting.Json;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
         .WriteTo.Console(new JsonFormatter())
         .WriteTo.File(new JsonFormatter(), "logs/WebApiLog-.log", rollingInterval: RollingInterval.Day)
         .CreateLogger();

        try
        {
            Log.Information("Starting web host");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseOrleansClient(c =>
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                var configuration = builder.Build();

                var connectionString = configuration.GetValue<string>("ConnectionString");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("ConnectionString must be non-empty.");
                }
                
                c.UseAdoNetClustering((Action<AdoNetClusteringClientOptions>)(options =>
                    {
                        options.Invariant = "MySql.Data.MySqlClient";
                        options.ConnectionString = connectionString;
                    }))
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "OrleansService";
                    });
            })
            /*
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug); // Set log level to Debug for more detailed logging
            })*/
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            })
            .UseSerilog();
}