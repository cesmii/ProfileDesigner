using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using Xunit;

namespace CESMII.ProfileDesigner.Api.Tests
{
    public class PostgresTableTests
    {
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
        /// <param name="cColumns"></param>
        [Theory]
        [InlineData("profile", 22)]
        public void CheckTableColumnCount(string strTable, int cColumns)
        {
            IConfiguration MyConfig = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json", true, true).Build();
            var connectionStringProfileDesigner = MyConfig.GetConnectionString("ProfileDesignerDB");

            NpgsqlConnection conn = new NpgsqlConnection(connectionStringProfileDesigner);
            conn.Open();

            // Define a query returning a single row result set
            string strSql = $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema='public' AND table_name='{strTable}'";
            NpgsqlCommand command = new NpgsqlCommand(strSql, conn);

            // Execute the query and obtain the value of the first column of the first row
            Int64 count = (Int64)command.ExecuteScalar();

            Assert.Equal(cColumns, count);

            conn.Close();
        }
    }
}
