//#define NODESETDBTEST
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CESMII.OpcUa.NodeSetModel;
using CESMII.OpcUa.NodeSetImporter;
using Microsoft.AspNetCore.Builder;

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
                        .UseNpgsql(connectionStringNodeSetModel, o =>
                        {
                            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                            o.MigrationsAssembly("CESMII.ProfileDesigner.Api");
                        })
                        .EnableSensitiveDataLogging()
            );
#endif
            services.AddCloudLibraryResolver();
            //services.AddCloudLibraryResolver(configuration.GetSection("CloudLibrary"));
        }
        public static void UseOpcUaImporter(this IApplicationBuilder app)
        {
#if NODESETDBTEST
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var nsDBContext = scope.ServiceProvider.GetService<NodeSetModelContext>();
                nsDBContext.Database.Migrate();
            }
#endif
        }

    }

}