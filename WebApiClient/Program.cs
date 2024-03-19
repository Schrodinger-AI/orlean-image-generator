using Google.Cloud.Diagnostics.Common;
using Serilog;
using Serilog.Formatting.Json;
using Shared;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
         .WriteTo.Console(new JsonFormatter())
         .WriteTo.File(new JsonFormatter(), "logs/WebApiLog-.log", rollingInterval: RollingInterval.Hour)
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
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            })
            // .ConfigureLogging((context, logging) =>
            // {
            //     var logType = context.Configuration.GetValue<string>("LogType");
            //     if (logType is LogTypeConstants.Gcp)
            //     {
            //         logging.AddGoogle(new LoggingServiceOptions {ProjectId = "schrodingerai-dev"});
            //     }
            //     else
            //     {
            //         logging.AddSerilog();
            //     }
            // });
    .UseSerilog();
}