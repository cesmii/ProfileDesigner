/* Author:      Markus Horstmann, C-Labs
 * Last Update: 4/13/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2022
 */

using Microsoft.Extensions.Options;
//using Microsoft.Extensions.Options.ConfigurationExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace CESMII.OpcUa.NodeSetImporter
{
    public static class UANodeSetCloudLibraryResolverExtensions
    {
        public static IServiceCollection AddCloudLibraryResolver(this IServiceCollection services, IConfiguration configurationSection)
        {
            services
                .Configure<UANodeSetCloudLibraryResolver.CloudLibraryOptions>(configurationSection)
                .AddScoped<IUANodeSetResolverWithProgress, UANodeSetCloudLibraryResolver>(sp => new UANodeSetCloudLibraryResolver(sp.GetRequiredService<IOptions<UANodeSetCloudLibraryResolver.CloudLibraryOptions>>().Value))
                ;
            return services;
        }
    }
}
