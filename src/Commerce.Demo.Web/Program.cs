using Serilog;

namespace Commerce.Demo.Web;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureCmsDefaults()
            .UseSerilog(
                (ctx, provider, lc) => lc.ReadFrom.Configuration(ctx.Configuration),
                writeToProviders: true)
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}
