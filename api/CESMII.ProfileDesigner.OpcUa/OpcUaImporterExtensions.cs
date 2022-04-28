#define NODESETDBTEST
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CESMII.OpcUa.NodeSetModel;
using CESMII.OpcUa.NodeSetImporter;

#if NODESETDBTEST
using Microsoft.EntityFrameworkCore;
#endif

namespace CESMII.ProfileDesigner.OpcUa
{
    public static class OpcUaImporterExtensions
    {
        public static void AddOpcUaImporter(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<OpcUaImporter>();
#if NODESETDBTEST
            // TODO turn into Option class
            var connectionStringNodeSetModel = configuration.GetConnectionString("NodeSetModelDB");

            services.AddDbContext<NodeSetModelContext>(options =>
                options
                        .UseNpgsql(connectionStringNodeSetModel, b => b.MigrationsAssembly("CESMII.ProfileDesigner.Api"))
                        .EnableSensitiveDataLogging()
            );
#endif
            services.AddCloudLibraryResolver(configuration.GetSection("CloudLibrary"));
        }
    }

}