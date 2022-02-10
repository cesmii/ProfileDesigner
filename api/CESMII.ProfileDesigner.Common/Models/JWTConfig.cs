namespace CESMII.ProfileDesigner.Common.Models
{
    public class JWTConfig
    {
        public string Key { get; set; }

        public string Issuer { get; set; }

        /// <summary>
        /// Gets or sets the value in minutes to expire the token.
        /// </summary>
        public int DefaultExpiration { get; set; }
    }
}
