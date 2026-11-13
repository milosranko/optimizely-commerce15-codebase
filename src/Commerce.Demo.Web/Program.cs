using Serilog;

namespace Commerce.Demo.Web;

public class Program
{
    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureCmsDefaults()
            .UseSerilog((ctx, provider, lc) => lc.ReadFrom.Configuration(ctx.Configuration))
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}
