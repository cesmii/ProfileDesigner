using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog.Web;

namespace CESMII.ProfileDesigner.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);  // Accept Postgres 'only UTC' rule.
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                 .ConfigureLogging(logging =>
                 {
                     logging.ClearProviders();
                     logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
#if DEBUG
                     logging.AddDebug();
#endif
                 })
                // Use NLog to provide ILogger instances.
                .UseNLog();
    }
}
