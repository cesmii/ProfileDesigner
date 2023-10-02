using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CESMII.ProfileDesigner.WebJobs
{
    class Program
    {
        private static ILogger<Program> _logger;

        public static async Task Main(string[] args)
        {
            var startup = new Startup();

            var builder = new HostBuilder()
                //this is set in the launch settings json (editable in debug tab of proj properties UI)
                .UseEnvironment(Environment.GetEnvironmentVariable("MyHostingEnvironment"))
                .ConfigureWebJobs(b =>
                {
                    b.AddAzureStorageCoreServices() //need this for storage integration  
                    .AddAzureStorageBlobs()         //need this for storage integration
                    .AddAzureStorageQueues()        //need this for queue based triggers in storage queue
                    //.AddTimers()                    //need this for time based triggers
                    //.AddServiceBus();
                    //.AddServiceBus()
                    //.AddEventHubs();
                    ;
                })
                .ConfigureAppConfiguration((hostingContext, b) =>
                {
                    // Adding command line as a configuration source
                    //override env specific settings
                    //TBD - make this environment specific
                    string envName = hostingContext.HostingEnvironment.EnvironmentName;
                    b.AddCommandLine(args)
                        .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                        .AddJsonFile($"appsettings.json", false, true)
                        .AddJsonFile($"appsettings.{envName}.json", true, true)
                        .AddEnvironmentVariables();
                })//;

                //builder
                .ConfigureLogging((context, b) => 
                    startup.ConfigureLogging(b, context.Configuration)
                    )
                //DI
                .ConfigureServices((context,services) => 
                    startup.ConfigureServices(services, context.Configuration)
                    )
                .UseConsoleLifetime();

            var host = builder.Build();

            _logger = host.Services.GetService<ILogger<Program>>();

            using (host)
            {
                _logger.LogInformation("Starting the host");
                await host.RunAsync();
            }
        }


    }
}