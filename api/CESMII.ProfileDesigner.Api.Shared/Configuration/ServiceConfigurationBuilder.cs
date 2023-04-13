using Microsoft.Extensions.Configuration;

namespace CESMII.ProfileDesigner.Api.Shared.Configuration
{
    public class ServiceConfigurationBuilder
    {
        public static string CreateConfiguration(string directory)
        {
//            var builder = new ConfigurationBuilder()
//                .SetBasePath(directory)
//#if DEBUG
//                .AddJsonFile("appsettings.json",  false,  true);
//#else
//                .AddJsonFile("appsettings.Development.json", true, true);
//#endif

//            var configuration = builder.Build();
//            return configuration.GetConnectionString("ProfileDesignerDB");
              throw new System.Exception("Maybe Unused Code?");
              return "";
        }
    }
}