using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CESMII.ProfileDesigner.Api.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyNamespace;
using Newtonsoft.Json.Linq;

namespace CESMII.ProfileDesigner.Api.Tests
{
    #region snippet1
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {

        protected override IHostBuilder CreateHostBuilder()
        {
            return base.CreateHostBuilder()
                .ConfigureHostConfiguration(
                    config => config.AddEnvironmentVariables("ASPNETCORE")
                        .AddInMemoryCollection(new Dictionary<string, string>
                        {
                            { "ConnectionStrings:ProfileDesignerDB", "Server=localhost;Username=cesmii;Database=profile_designer_local_test;Port=5432;Password=cesmii;SSLMode=Prefer;Include Error Detail=true" },
                        }))
                        ;
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<ProfileController>();
            });
        }

        internal Client GetApiClientAuthenticated()
        {
            var client = CreateClient();
            AddUserAuth(client);
            return GetApiClient(client);
        }
        internal Client GetApiClient(HttpClient client)
        {
            var nswagClient = new Client(client.BaseAddress.ToString(), client);
            return nswagClient;
        }
        internal void AddUserAuth(HttpClient client)
        {
            var apiClient = GetApiClient(client);
            var response = apiClient.LoginAsync(new MyNamespace.LoginModel { UserName = "cesmii", Password = "cesmii", }).Result;
            var token = (response.Data as JObject).First.Values().FirstOrDefault()?.ToString();

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
    #endregion
}