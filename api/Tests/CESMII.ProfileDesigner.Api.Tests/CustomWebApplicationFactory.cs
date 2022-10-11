using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using CESMII.ProfileDesigner.Api.Controllers;
using CESMII.ProfileDesigner.Data.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MyNamespace;
using Newtonsoft.Json.Linq;

namespace CESMII.ProfileDesigner.Api.Tests
{
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {

        protected override IHostBuilder CreateHostBuilder()
        {
            return base.CreateHostBuilder()
                .ConfigureHostConfiguration(
                    config => config
                        .AddEnvironmentVariables("ASPNETCORE")
                        .AddInMemoryCollection(new Dictionary<string, string>
                        {
                            { "ConnectionStrings:ProfileDesignerDB", "Server=localhost;Username=cesmii;Database=profile_designer_local_test;Port=5432;Password=cesmii;SSLMode=Prefer;Include Error Detail=true" },
                        }))
                .ConfigureServices(services =>
                {
                })
                ;

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
            });
        }

        bool bLoggedIn = false;
        internal Client GetApiClientAuthenticated()
        {
            var client = CreateClient();
            client.Timeout = TimeSpan.FromMinutes(60);
            AddUserAuth(client);
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
        internal void AddUserAuth(HttpClient client)
        {
            var apiClient = GetApiClient(client);

            var testUser = new ClaimsPrincipal(new ClaimsIdentity(
                new List<Claim>
                {
                    new Claim("objectidentifier", "1234"),
                    new Claim("preferred_username", "cesmiitest"),
                    new Claim("ClaimTypes.Surname", "cesmiitest"),
                    new Claim("ClaimTypes.GivenName", "cesmiitest"),
                    new Claim("name", "cesmiitest"),
                    new Claim("role", "cesmii.profiledesigner.user"),
                }));
            var tokenInfo = new JwtSecurityToken(issuer: "testissuer", claims: testUser.Claims);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.WriteToken(tokenInfo);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);
        }
    }
}