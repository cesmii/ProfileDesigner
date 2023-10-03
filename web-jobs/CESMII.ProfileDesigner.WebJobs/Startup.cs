using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
//using Microsoft.Azure.WebJobs;

using Microsoft.EntityFrameworkCore;

using NLog;
using NLog.Extensions.Logging;

using Opc.Ua.Cloud.Library.Client;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.Data.Contexts;
using CESMII.ProfileDesigner.Importer;
using CESMII.ProfileDesigner.Data.Repositories;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.Common;
using CESMII.Common.CloudLibClient;
using CESMII.Common.SelfServiceSignUp.Services;
using CESMII.ProfileDesigner.Api.Shared.Utils;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using CESMII.OpcUa.NodeSetImporter;
using CESMII.ProfileDesigner.OpcUa;
using CESMII.ProfileDesigner.Opc.Ua.NodeSetDBCache;

//[assembly: FunctionsStartup(typeof(CESMII.ProfileDesigner.Functions.Startup))]
namespace CESMII.ProfileDesigner.WebJobs
{
    public class Startup
    {

        public Startup()
        {
        }

        public void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            var connectionStringProfileDesigner = config.GetConnectionString("ProfileDesignerDB");
            //PostgreSql context
#if DEBUG
            services.AddDbContext<ProfileDesignerPgContext>(options =>
                    options.UseNpgsql(connectionStringProfileDesigner)
                    //options.UseNpgsql(connectionStringProfileDesigner, options => options.EnableRetryOnFailure())
                    .EnableSensitiveDataLogging());
#else
            services.AddDbContext<ProfileDesignerPgContext>(options =>
                    options.UseNpgsql(connectionStringProfileDesigner));
#endif

            //import related di
            services.Configure<UACloudLibClient.Options>(config.GetSection("CloudLibrary"));

            services.AddScoped<IRepository<User>, BaseRepo<User, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<ImportLog>, BaseRepo<ImportLog, ProfileDesignerPgContext>>();

            services.AddSingleton<ConfigUtil>();  // helper to allow us to bind to app settings data 

            services.AddScoped<UserDAL>();                  // Has extra methods outside of the IDal interface
            services.AddScoped<IDal<ImportLog, ImportLogModel>, ImportLogDAL>();
            services.AddScoped<ICloudLibDal<CloudLibProfileModel>, CloudLibDAL>();
            services.AddScoped<ICloudLibWrapper, CloudLibWrapper>();

            services.AddScoped<ImportNotificationUtil>();  // helper to allow import service to send notification email
            services.AddScoped<ImportDirectWrapper>();  // helper to call Azure function
            services.AddScoped<ImportService>();
            services.AddOpcUaImporter(config);

            //indirectly related di
            services.AddSingleton<MailRelayService>();                   // helper for emailing (in CESMII.Common.SelfServiceSignUp)
            services.AddScoped<ICustomRazorViewEngine, CustomRazorViewEngine>();  //this facilitates sending formatted emails w/o dependency on controller

            //other repos, dals, etc. used by opc ua importer
            services.AddScoped<IRepository<ProfileTypeDefinition>, BaseRepo<ProfileTypeDefinition, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<ProfileTypeDefinition>, BaseRepo<ProfileTypeDefinition, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<ProfileAttribute>, BaseRepo<ProfileAttribute, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<ProfileInterface>, BaseRepo<ProfileInterface, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<LookupType>, BaseRepo<LookupType, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<EngineeringUnit>, BaseRepo<EngineeringUnit, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<EngineeringUnitRanked>, BaseRepo<EngineeringUnitRanked, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<LookupDataType>, BaseRepo<LookupDataType, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<LookupDataTypeRanked>, BaseRepo<LookupDataTypeRanked, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<LookupItem>, BaseRepo<LookupItem, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<ImportLog>, BaseRepo<ImportLog, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<ProfileTypeDefinitionAnalytic>, BaseRepo<ProfileTypeDefinitionAnalytic, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<ProfileTypeDefinitionFavorite>, BaseRepo<ProfileTypeDefinitionFavorite, ProfileDesignerPgContext>>();

            //NodeSet Related Tables
            services.AddScoped<IRepository<Profile>, BaseRepo<Profile, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<NodeSetFile>, BaseRepo<NodeSetFile, ProfileDesignerPgContext>>();

            //other repos, dals, etc. used by opc ua importer
            services.AddScoped<IDal<Profile, ProfileModel>, ProfileDAL>();
            services.AddScoped<IDal<ProfileTypeDefinition, ProfileTypeDefinitionModel>, ProfileTypeDefinitionDAL>();
            services.AddScoped<IDal<LookupDataType, LookupDataTypeModel>, LookupDataTypeDAL>();
            services.AddScoped<IDal<NodeSetFile, NodeSetFileModel>, NodeSetFileDAL>();
            services.AddScoped<IDal<EngineeringUnit, EngineeringUnitModel>, EngineeringUnitDAL>();
            services.AddScoped<IDal<ProfileTypeDefinitionAnalytic, ProfileTypeDefinitionAnalyticModel>, ProfileTypeDefinitionAnalyticDAL>();

            //adding this so we can send emails using Razor view engine for nice formatting
            var diagnosticSource = new System.Diagnostics.DiagnosticListener("Microsoft.AspNetCore");
            services.AddSingleton<System.Diagnostics.DiagnosticListener>(diagnosticSource);
            services.AddSingleton<System.Diagnostics.DiagnosticSource>(diagnosticSource);
            services.AddRazorPages();
            services.AddMvc();
        }

        public void ConfigureLogging(ILoggingBuilder b, IConfiguration config)
        {
            var connectionStringProfileDesigner = config.GetConnectionString("ProfileDesignerDB");
            b.AddConsole();
            //set up nLog
            //set variables used in nLog.config
            NLog.LogManager.Configuration.Variables["connectionString"] = connectionStringProfileDesigner;
            NLog.LogManager.Configuration.Variables["appName"] = "CESMII-ProfileDesigner-WebJobs";
            b.AddNLog();
        }
    }
}

