namespace CESMII.ProfileDesigner.Api.Shared.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    using CESMII.ProfileDesigner.Common.Enums;
    using CESMII.ProfileDesigner.Common.Utils;
    using CESMII.ProfileDesigner.DAL;

    public class UserAzureADMapping
    {
        private readonly RequestDelegate _next;

        public UserAzureADMapping(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ILogger<UserAzureADMapping> logger, UserDAL dalUser)
        {
            //if authenticated (by Azure AD) and if we can get the user's local app id, then we add the claim
            //downstream - on login, the local app id is created on endpoint called after onLogin.
            //downstream - the id set here is retrieved and used throughout app as we relate our data to a locally
            //maintained user id. In that table, there is an Azure object id which maps us to Azure.
            if (context.User != null && context.User.Identity.IsAuthenticated)
            {
                var oId = GetAppUserId(context.User, logger, dalUser);

                if (oId.HasValue)
                {
                    var permission = EnumUtils.GetEnumDescription(PermissionEnum.UserAzureADMapped);
                    var claims = new List<Claim>()
                    {
                        new Claim(permission, oId.Value.ToString())
                    };

                    var appIdentity = new ClaimsIdentity(claims);
                    context.User.AddIdentity(appIdentity);
                }
            }

            await _next(context);
        }


        protected int? GetAppUserId(ClaimsPrincipal user, ILogger<UserAzureADMapping> logger, UserDAL dalUser)
        {
            //Get Object id from user.identity. Then try and lookup in the local db to get the local id associated with 
            //that Azure object id. If not present yet, the onAADLogin endpoint will add it there. 
            var oId = user.GetUserIdAAD();
            var matches = dalUser.Where(x => x.ObjectIdAAD.ToLower().Equals(oId), null).Data;
            switch (matches.Count)
            {
                case 1:
                    return matches[0].ID.Value;
                case 0:
                    return null;
                default:
                    logger.LogWarning($"GetAppUserId||More than one user record found with user name {oId}.");
                    throw new InvalidOperationException($"GetAppUserId: More than one record user found with user name {oId}.");
            }
        }
    }



}