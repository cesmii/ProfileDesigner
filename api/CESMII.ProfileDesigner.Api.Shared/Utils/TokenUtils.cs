namespace CESMII.ProfileDesigner.Api.Shared.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using Microsoft.IdentityModel.Tokens;

    using CESMII.ProfileDesigner.Common;
    using CESMII.ProfileDesigner.Common.Models;
    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Api.Shared.Models;

    public class TokenUtils
    {
        // TODO Add "One Time Token" 24 hours for both "first login" and reset password.
        private readonly JWTConfig _config;

        public TokenUtils(ConfigUtil configUtil)
        {
            _config = configUtil.JWTSettings;
        }


        public TokenModel BuildToken(UserModel user)
        {
            var expirationDate = DateTime.UtcNow.AddMinutes(_config.DefaultExpiration);
            var tokenDescriptor = BuildTokenDescriptor(user, expirationDate);
            var tokenHandler = new JwtSecurityTokenHandler();
            var newToken = tokenHandler.CreateToken(tokenDescriptor);
            return new TokenModel { UserID = user.ID.Value, UserName = user.UserName, Token = tokenHandler.WriteToken(newToken), ExpirationDate = expirationDate, IsImpersonating = false };
        }

        /// <summary>
        /// Build a special token with impersonation information attached.
        /// </summary>
        /// <param name="user">The current, actual user.</param>
        /// <param name="targetUserID">The target user ID that will be impersonated.</param>
        /// <returns>A token model with impersonation information.</returns>
        public TokenModel BuildImpersonationToken(UserModel user, long targetUserID)
        {
            var expirationDate = DateTime.UtcNow.AddMinutes(_config.DefaultExpiration);
            var tokenDescriptor = BuildTokenDescriptor(user, expirationDate);

            // Add custom claims for impersonation values. Set IsImpersonating to true.
            tokenDescriptor.Subject.AddClaim(new Claim(CustomClaimTypes.IsImpersonating, true.ToString()));

            // Add the target User ID.
            tokenDescriptor.Subject.AddClaim(new Claim(CustomClaimTypes.TargetUserID, targetUserID.ToString()));

            var tokenHandler = new JwtSecurityTokenHandler();
            var newToken = tokenHandler.CreateToken(tokenDescriptor);
            return new TokenModel { UserID = user.ID.Value, UserName = user.UserName, Token = tokenHandler.WriteToken(newToken), ExpirationDate = expirationDate, IsImpersonating = true };
        }

        /// <summary>
        /// The intent of this token is to be sent to user as part of a url. They will click on url and this token value will 
        /// be plucked out and used during confirmation of account creation.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="expirationDate"></param>
        /// <returns></returns>
        public string BuildRegistrationToken(UserModel user, DateTime expirationDate)
        {
            var tokenDescriptor = BuildTokenDescriptor(user, expirationDate);
            var tokenHandler = new JwtSecurityTokenHandler();
            var newToken = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(newToken);
        }

        /// <summary>
        /// Validate and return the user id associated with this token. 
        /// Failures would occur if any of the issuer info, the expiration info is not matching.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public string ValidateRegistrationToken(string token)
        {
            SecurityToken validatedToken;
            ClaimsPrincipal principal = new JwtSecurityTokenHandler().ValidateToken(token, this.TokenValidationParams, out validatedToken);
            return principal?.FindFirst(c => c.Type.Equals(ClaimTypes.Sid))?.Value;
        }

        /// <summary>
        /// Get a minimalist list of claims that we can use downstream when decrypting the token and using for 
        /// completion of registration.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private static List<Claim> GetDefaultClaims(UserModel user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, user.ID.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };
            return claims;
        }

        private TokenValidationParameters TokenValidationParams
        {
            get
            {
                var result = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.Key)),
                    ValidateLifetime = true,
                    LifetimeValidator = CustomLifetimeValidator,
                    ValidIssuer = _config.Issuer
                };
                return result;
            }
        }

        /// <summary>
        /// This was added to tighten up the expiration check which did seemed to have a 5 minute offset. 
        /// </summary>
        /// <remarks>TBD - We may be able to remove this as there is a ClockSkew property set in startup.cs to eliminate the 5 minute offset.</remarks>
        /// <param name="notBefore"></param>
        /// <param name="expires"></param>
        /// <param name="tokenToValidate"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        private bool CustomLifetimeValidator(DateTime? notBefore, DateTime? expires, SecurityToken tokenToValidate, TokenValidationParameters @param)
        {
            if (expires != null)
            {
                return expires > DateTime.UtcNow;
            }
            return false;
        }

        private SecurityTokenDescriptor BuildTokenDescriptor(UserModel user, DateTime expirationDate)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var result = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(),
                Expires = expirationDate,
                SigningCredentials = credentials,
                Issuer = _config.Issuer,
                IssuedAt = DateTime.UtcNow
            };

            var claims = GetDefaultClaims(user);
            //add claims for permissions
            foreach (var name in user.PermissionNames)
            {
                claims.Add(new Claim(ClaimTypes.Role, name));
            }
            result.Subject.AddClaims(claims);

            return result;
        }
    }
}