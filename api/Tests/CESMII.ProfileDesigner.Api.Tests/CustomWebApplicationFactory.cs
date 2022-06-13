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
            //Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
            return base.CreateHostBuilder()
                .ConfigureHostConfiguration(
                    config => config.AddEnvironmentVariables("ASPNETCORE")
                        .AddInMemoryCollection(new Dictionary<string, string>
                        {
                            { "ServicePassword", "testpw" },
                            { "ConnectionStrings:ProfileDesignerDB", "Server=localhost;Username=testuser;Database=profile_designer_local_test;Port=5432;Password=password;SSLMode=Prefer;Include Error Detail=true" },
                        }))
                        ;
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<ProfileController>();

                //var descriptor = services.SingleOrDefault(
                //    d => d.ServiceType ==
                //        typeof(DbContextOptions<ApplicationDbContext>));

                //services.Remove(descriptor);

                //services.AddDbContext<ApplicationDbContext>(options =>
                //{
                //    options.UseInMemoryDatabase("InMemoryDbForTesting");
                //});

                //var sp = services.BuildServiceProvider();

                //using (var scope = sp.CreateScope())
                //{
                //    var scopedServices = scope.ServiceProvider;
                //    //var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                //    var logger = scopedServices
                //        .GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                //    //db.Database.EnsureCreated();

                //    try
                //    {
                //        //Utilities.InitializeDbForTests(db);
                //    }
                //    catch (Exception ex)
                //    {
                //        logger.LogError(ex, "An error occurred seeding the " +
                //            "database with test messages. Error: {Message}", ex.Message);
                //    }
                //}
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
            var nswagClient = new MyNamespace.Client(client.BaseAddress.ToString(), client);
            return nswagClient;
        }
        internal void AddUserAuth(HttpClient client)
        {
            var apiClient = GetApiClient(client);
            var response = apiClient.LoginAsync(new MyNamespace.LoginModel { UserName = "cesmii", Password = "cesmii", }).Result;
            var token = (response.Data as JObject).First.Values().FirstOrDefault()?.ToString();

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        string _token;
        protected override void ConfigureClient(HttpClient client)
        {
            base.ConfigureClient(client);
            if (false && client.DefaultRequestHeaders.Authorization == null)
            {
                if (_token == null)
                {
                    var nswagClient = new MyNamespace.Client(client.BaseAddress.ToString(), client);

                    // Act
                    var response = nswagClient.LoginAsync(new MyNamespace.LoginModel { UserName = "cesmii", Password = "cesmii", }).Result;
                    var token = (response.Data as JObject).First.Values().FirstOrDefault()?.ToString();
                    _token = token;
                }
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            }
        }
    }
    #endregion
}