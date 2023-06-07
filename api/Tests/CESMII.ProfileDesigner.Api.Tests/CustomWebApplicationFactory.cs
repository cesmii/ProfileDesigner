using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using MyNamespace;
using CESMII.ProfileDesigner.Api.Controllers;
using CESMII.Common.CloudLibClient;

namespace CESMII.ProfileDesigner.Api.Tests
{
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        private readonly Dictionary<string, string> _settings = new ()
        {
            {
                "ConnectionStrings:ProfileDesignerDB",
                "Server=localhost;Username=profiledesigner;Database=profile_designer_local_test;Port=5432;Password=cesmii;SSLMode=Prefer;Include Error Detail=true"
            }
        };

        private IConfiguration _config;

        /// <summary>
        /// This will be the config used within the test cases to access settings used on this side of the test. 
        /// The settings added below in the ConfigureWebHost method are settings used within the API. There is a 
        /// de-coupled effect when calling the API endpoints. 
        /// </summary>
        public IConfiguration Config { 
            get 
            {
                if (_config == null)
                {
                    _config = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile(path: "appsettings.test.json", optional: true, reloadOnChange: true)  //not yet used but can be a place to store settings
                        .AddInMemoryCollection(_settings)
                        .AddUserSecrets(typeof(CloudLibraryController).Assembly)
                        .Build();
                }
                return _config; 
            } 
        }

        public TestUserModel _localUser;
        public TestUserModel LocalUser
        {
            get
            {
                if (_localUser == null)
                {
                    _localUser = new TestUserModel();
                }
                return _localUser;
            }
        }

        protected override IHostBuilder CreateHostBuilder()
        {
            return base.CreateHostBuilder()
                .ConfigureHostConfiguration(
                    config => config
                        .AddEnvironmentVariables("ASPNETCORE")
                        .AddInMemoryCollection(_settings).Build())

            ;
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<ProfileController>();
                services.AddTransient<AuthController>(); // For login test hook
                services.AddAuthentication(//JwtBearerDefaults.AuthenticationScheme)
                    options =>
                    {
                        options.DefaultScheme = "TestBearerOrAzureAd";
                        options.DefaultChallengeScheme = "TestBearerOrAzureAd";
                    })
                    .AddJwtBearer("TestBearer", options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = false,
                            ValidateLifetime = false,
                            ValidateAudience = false,
                            ValidateIssuerSigningKey = false,
                            ValidIssuer = "testissuer",
                            RequireSignedTokens = false,
                        };
                    }
                    )
                    .AddPolicyScheme("TestBearerOrAzureAd", "TestBearerOrAzureAd", options =>
                    {
                        options.ForwardDefaultSelector = context =>
                        {
                            string authorization = context.Request.Headers.Authorization.FirstOrDefault();
                            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                            {
                                var token = authorization.Substring("Bearer ".Length).Trim();
                                var jwtHandler = new JwtSecurityTokenHandler();

                                return (jwtHandler.CanReadToken(token) && jwtHandler.ReadJwtToken(token).Issuer.Equals("testissuer"))
                                    ? "TestBearer" : "AzureAd";
                            }
                            return "Bearer";
                        };
                    })
                ;
                // Inject Cloud Library Mock - comment out to test against live cloud lib
                // Delete mock data files to record reponses from a live cloud lib
                services.RemoveAll<ICloudLibWrapper>();
                services.AddScoped<CloudLibWrapper>();
                services.AddScoped<ICloudLibWrapper, CloudLibMock>();
            });
        }

        bool bLoggedIn = false;
        internal Client GetApiClientAuthenticated(bool isAdmin = false)
        {
            var client = CreateClient();
            client.Timeout = TimeSpan.FromMinutes(60);
            AddUserAuth(client, isAdmin);
            var apiClient = GetApiClient(client);
            if (!bLoggedIn)
            {
                apiClient.OnAADLoginAsync().Wait();
                bLoggedIn = true;
            }
            return apiClient;
        }

        internal Client GetApiClient(HttpClient client)
        {
            var nswagClient = new Client(client.BaseAddress.ToString(), client);
            return nswagClient;
        }
        internal void AddUserAuth(HttpClient client, bool isAdmin)
        {
            var claims = new List<Claim>
                {
                    new Claim("objectidentifier", LocalUser.ObjectIdAAD),
                    new Claim("preferred_username", LocalUser.UserName),
                    new Claim("ClaimTypes.Surname", LocalUser.UserName),
                    new Claim("ClaimTypes.GivenName", LocalUser.UserName),
                    new Claim("name", LocalUser.UserName),
                    //new Claim("role", "cesmii.profiledesigner.user"),
                };
            if (isAdmin) claims.Add(new Claim("role", "cesmii.profiledesigner.admin"));

            var testUser = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var tokenInfo = new JwtSecurityToken(issuer: "testissuer", claims: testUser.Claims);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.WriteToken(tokenInfo);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);
        }
    }

    /// <summary>
    /// A model to faciliate re-use of the test user that will be generated and re-used in multiple tests 
    /// </summary>
    public class TestUserModel
    {
        public string ObjectIdAAD { get; set; } = "1234";
        public string UserName { get; set; } = "cesmiitest";
    }

}