/* Author:      Markus Horstmann, C-Labs
 * Last Update: 4/13/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2022
 */

using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Opc.Ua.Cloud.Library.Client;

namespace CESMII.OpcUa.NodeSetImporter
{
    public static class UANodeSetCloudLibraryResolverExtensions
    {
        public static IServiceCollection AddCloudLibraryResolver(this IServiceCollection services)
        {
            services
                .AddScoped<IUANodeSetResolverWithPending, UANodeSetCloudLibraryResolver>(sp => new UANodeSetCloudLibraryResolver(sp.GetRequiredService<IOptions<UACloudLibClient.Options>>().Value))
                ;
            return services;
        }
        public static IServiceCollection AddCloudLibraryResolver(this IServiceCollection services, IConfiguration configurationSection)
        {
            services
                .Configure<UACloudLibClient.Options>(configurationSection)
                .AddScoped<IUANodeSetResolverWithPending, UANodeSetCloudLibraryResolver>(sp => new UANodeSetCloudLibraryResolver(sp.GetRequiredService<IOptions<UACloudLibClient.Options>>().Value))
                ;
            return services;
        }
    }
}
