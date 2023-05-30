using System;

using Xunit;
using Xunit.Abstractions;

using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CESMII.ProfileDesigner.Api.Tests
{
    public class PostgresTableTests : IClassFixture<CustomWebApplicationFactory<Api.Startup>>, IDisposable
    {
        protected readonly CustomWebApplicationFactory<Api.Startup> _factory;
        protected readonly ITestOutputHelper output;
        //private MyNamespace.Client _apiClient;

        public PostgresTableTests(CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup> factory, ITestOutputHelper output)
        {
            _factory = factory;
            this.output = output;
        }

        //[Fact]
        //public void DoesThisRun()
        //{
        //    int i = 4;
        //    int j = 54;
        //    if (i+j != 58)
        //        throw new System.Exception("This test should not fail");
        //}

        /// <summary>
        /// CheckTableColumnCount - New unit test to track when columns get added to tables. Just one for now, more later.
        /// </summary>
        /// <param name="strTable"></param>
        /// <param name="expectedCount"></param>
        [Theory]
        [InlineData("profile", 23)]
        [InlineData("profile_type_definition", 25)]
        [InlineData("profile_attribute", 44)]
        public void CheckTableColumnCount(string strTable, int expectedCount)
        {
            //IConfiguration MyConfig = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json", true, true).Build();
            //var connectionStringProfileDesigner = MyConfig.GetConnectionString("ProfileDesignerDB");
            var connectionStringProfileDesigner = _factory.Config.GetConnectionString("ProfileDesignerDB");
            NpgsqlConnection conn = new NpgsqlConnection(connectionStringProfileDesigner);
            try
            {
                conn.Open();

                // Define a query returning a single row result set
                string strSql = $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema='public' AND table_name='{strTable}'";
                NpgsqlCommand command = new NpgsqlCommand(strSql, conn);

                // Execute the query and obtain the value of the first column of the first row
                Int64 count = (Int64)command.ExecuteScalar();

                if (expectedCount == count) output.WriteLine($"Expected: {expectedCount}, Actual: {count}");
                Assert.Equal(expectedCount, count);
            }
            finally {
                conn.Close();
            }
        }

        public virtual void Dispose()
        {
            //do clean up here
        }
    }
}
