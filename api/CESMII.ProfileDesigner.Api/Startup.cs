using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;

using NLog;
using NLog.Extensions.Logging;

using CESMII.ProfileDesigner.Common;
using CESMII.ProfileDesigner.Common.Utils;
using CESMII.ProfileDesigner.Api.Shared.Utils;
using CESMII.ProfileDesigner.Data.Contexts;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.Data.Repositories;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.OpcUa;
using CESMII.ProfileDesigner.Api.Shared.Extensions;

namespace CESMII.ProfileDesigner.Api
{
    public class Startup
    {
        private readonly string _corsPolicyName = "SiteCorsPolicy";
        private readonly string _version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionStringProfileDesigner = Configuration.GetConnectionString("ProfileDesignerDB");
            //PostgreSql context
            services.AddDbContext<ProfileDesignerPgContext>(options =>
                    options.UseNpgsql(connectionStringProfileDesigner).EnableSensitiveDataLogging());


            //set variables used in nLog.config
            NLog.LogManager.Configuration.Variables["connectionString"] = connectionStringProfileDesigner;
            NLog.LogManager.Configuration.Variables["appName"] = "CESMII-ProfileDesigner";

            //profile and related data
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
            services.AddScoped<IRepository<StandardNodeSet>, BaseRepo<StandardNodeSet, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<Profile>, BaseRepo<Profile, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<NodeSetFile>, BaseRepo<NodeSetFile, ProfileDesignerPgContext>>();

            //stock tables
            services.AddScoped<IRepository<User>, BaseRepo<User, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<Permission>, BaseRepo<Permission, ProfileDesignerPgContext>>();

            //DAL objects
            services.AddScoped<UserDAL>();  //this one has extra methods outside of the IDal interface
            services.AddScoped<IDal<ProfileTypeDefinition, ProfileTypeDefinitionModel>, ProfileTypeDefinitionDAL>();
            services.AddScoped<IDal<LookupItem, LookupItemModel>, LookupDAL>();
            services.AddScoped<IDal<LookupDataType, LookupDataTypeModel>, LookupDataTypeDAL>();
            services.AddScoped<IDal<LookupDataTypeRanked, LookupDataTypeRankedModel>, LookupDataTypeRankedDAL>();
            services.AddScoped<IDal<EngineeringUnit, EngineeringUnitModel>, EngineeringUnitDAL>();
            services.AddScoped<IDal<EngineeringUnitRanked, EngineeringUnitRankedModel>, LookupEngUnitRankedDAL>();
            services.AddScoped<IDal<LookupType, LookupTypeModel>, LookupTypeDAL>();
            services.AddScoped<IDal<ImportLog, ImportLogModel>, ImportLogDAL>();
            services.AddScoped<IDal<ProfileTypeDefinitionAnalytic, ProfileTypeDefinitionAnalyticModel>, ProfileTypeDefinitionAnalyticDAL>();

            //NodeSet related
            services.AddScoped<IDal<StandardNodeSet, StandardNodeSetModel>, StandardNodeSetDAL>();
            services.AddScoped<IDal<Profile, ProfileModel>, ProfileDAL>();
            services.AddScoped<IDal<NodeSetFile, NodeSetFileModel>, NodeSetFileDAL>();

            // Configuration, utils, one off objects
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddSingleton<ConfigUtil>();  //helper to allow us to bind to app settings data 
            services.AddSingleton<MailRelayService>();  //helper for emailing
            services.AddScoped<DAL.Utils.ProfileMapperUtil>();  //helper to allow us to modify profile data for front end 
            services.AddOpcUaImporter(Configuration);

            //AAD - no longer need this
            // Add token builder.
            //var configUtil = new ConfigUtil(Configuration);
            //services.AddTransient(provider => new TokenUtils(configUtil));

            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "CESMII.ProfileDesigner.Api",
                    Version = "v1"
                });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Name = "Authorization",
                    Description = "Please insert JWT with Bearer into field"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                {
                    new OpenApiSecurityScheme {
                    Reference = new OpenApiReference {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                    },Array.Empty<string>()
                }
                });
            });

            // https://stackoverflow.com/questions/46112258/how-do-i-get-current-user-in-net-core-web-api-from-jwt-token
            //services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //    .AddJwtBearer(options =>
            //    {
            //        options.TokenValidationParameters = new TokenValidationParameters
            //        {
            //            ValidateIssuer = true,
            //            ValidateLifetime = true,
            //            ValidateAudience = false,
            //            ValidateIssuerSigningKey = true,
            //            ValidIssuer = configUtil.JWTSettings.Issuer,
            //            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configUtil.JWTSettings.Key)),
            //            // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
            //            ClockSkew = TimeSpan.Zero
            //        };

            //        options.Events = new JwtBearerEvents
            //        {
            //            OnTokenValidated = context => {
            //                return Task.CompletedTask;
            //            },
            //            OnAuthenticationFailed = context =>
            //            {
            //                //Code Smell: string tokenVal = context.Request.Headers["Authorization"];
            //                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            //                {
            //                    context.Response.Headers.Add("Token-Expired", "true");
            //                }
            //                return Task.CompletedTask;
            //            }
            //        };
            //    });
            //New - Azure AD approach replaces previous code above
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(Configuration, "AzureAdSettings");

            //TBD - may not need these at all anymore since AAD implementation
            // Add permission authorization requirements.
            services.AddAuthorization(options =>
            {
                /*
                // Ability to...
                options.AddPolicy(
                    nameof(PermissionEnum.CanViewProfile),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.CanViewProfile)));

                // Ability to...
                options.AddPolicy(
                    nameof(PermissionEnum.CanManageProfile),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.CanManageProfile)));

                // Ability to...
                options.AddPolicy(
                    nameof(PermissionEnum.CanDeleteProfile),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.CanDeleteProfile)));

                // Ability to...
                options.AddPolicy(
                    nameof(PermissionEnum.CanManageSystemSettings),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.CanManageSystemSettings)));

                // Ability to...
                options.AddPolicy(
                    nameof(PermissionEnum.CanManageUsers),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.CanManageUsers)));

                // Ability to...
                options.AddPolicy(
                    nameof(PermissionEnum.CanImpersonateUsers),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.CanImpersonateUsers)));
                */

                // this "permission" is set once AD user has a mapping to a user record in the Profile Designer DB
                options.AddPolicy(
                    nameof(PermissionEnum.UserAzureADMapped),
                    policy => policy.Requirements.Add(new PermissionRequirement(PermissionEnum.UserAzureADMapped)));
            });

            services.AddCors(options =>
            {
                options.AddPolicy(_corsPolicyName,
                builder =>
                {
                    //TBD - uncomment, come back to this and lock down the origins based on the appsettings config settings
                    //Code Smell: builder.WithOrigins(configUtil.CorsSettings.AllowedOrigins);
                    builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });

            services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

            //Add support for background processing of long running import
            services.AddHostedService<LongRunningService>();
            services.AddSingleton<BackgroundWorkerQueue>();
            services.AddScoped<Utils.ImportService>();

            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
            });

            // Add in-memory caching
            services.AddMemoryCache();

            // Add response caching.
            services.AddResponseCaching();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(o => {
                    if (o is HttpContext ctx)
                    {
                        ctx.Response.Headers["x-api-version"] = _version;
                    }
                    return Task.CompletedTask;
                }, context);
                await next();
            });

            if (env.IsDevelopment() || env.IsEnvironment("Development.Local"))
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CESMII.ProfileDesigner.Api v1"));
            }
            else
            {
                app.UseHsts();
            }
            app.UseOpcUaImporter();
            app.UseHttpsRedirection();

            app.UseRouting();

            // Enable CORS. - this needs to go after UseRouting.
            app.UseCors(_corsPolicyName);

            // Enable authentications (Jwt in our case)
            app.UseAuthentication();

            app.UseMiddleware<UserAzureADMapping>();

            app.UseAuthorization();

            // Allow use of static files.
            app.UseStaticFiles();

            // https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-2.2
            app.UseResponseCaching();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
