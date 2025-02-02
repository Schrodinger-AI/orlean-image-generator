using Serilog;
using Serilog.Formatting.Json;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
         .WriteTo.Console(new JsonFormatter())
         .WriteTo.File(new JsonFormatter(), "logs/WebApiLog-.log", 
             rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3, fileSizeLimitBytes: 2L * 1024 * 1024 * 1024)
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
            .UseSerilog();
}