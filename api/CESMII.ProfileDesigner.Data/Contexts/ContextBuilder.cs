using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CESMII.ProfileDesigner.Data.Contexts
{
    /// <summary>
    /// Context Builder is utilized for Unit Tests project. Does not affect runtime otherwise but the context while unit testing cannot be created via DI.
    /// </summary>
    public class ContextBuilder
    {
        private readonly IConfiguration _configuration;

        public ContextBuilder(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /* TBD - come back to this
        public static FpDbContext CreateWebContext(string sqlString)
        {
            if (string.IsNullOrEmpty(sqlString))
            {
                throw new ArgumentNullException(sqlString, "A SQL connection string must be provided.");
            }
            var optionsBuilder = new DbContextOptionsBuilder<FpDbContext>();
            optionsBuilder.UseSqlServer(sqlString);
            var context = new FpDbContext(optionsBuilder.Options);
            return context;
        }
        */
    }
}