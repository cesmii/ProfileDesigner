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
            // WARNING: DO NOT SET THIS FLAG!
            // With this flag, Npqsql will interpret DateTime.Kind Utc as local time, resulting in nasty publicationdate mismatches. This is one of the reasons this behavior was deprecated in Npgsql.
            // If you encounter an error writing a DateTime that is not Utc to the dataase, adjust to Utc before writing.
            //System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);  // Accept Postgres 'only UTC' rule.
            // END WARNING
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
