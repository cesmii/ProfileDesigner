namespace CESMII.ProfileDesigner.Common.Models
{
    //using System.Collections.Generic;

    public class AuthenticationConfig
    {
        //TBD - add in password settings - max retries, password minimums, etc. 
        public int SessionLength { get; set; }
        public string LoginPath { get; set; }
        public string LogoutPath { get; set; }
        public string AccessDeniedPath { get; set; }

        public CookieConfig CookieSettings { get; set; }
    }

    public class CookieConfig
    {
        //TBD - add in additional properties
        public string Name { get; set; }

    }

}
