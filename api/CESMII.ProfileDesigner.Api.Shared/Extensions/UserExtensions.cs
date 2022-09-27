namespace CESMII.ProfileDesigner.Api.Shared.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;

    using CESMII.ProfileDesigner.Common.Enums;
    using CESMII.ProfileDesigner.Common.Utils;
    using CESMII.ProfileDesigner.DAL.Models;

    public static class UserExtension
    {
        public static bool HasPermission(this ClaimsPrincipal user, PermissionEnum permission)
        {
            return user.HasClaim(ClaimTypes.Role, EnumUtils.GetEnumDescription(permission));
        }

        /*
        /// <summary>
        /// If the user has any permission considered an admin permission, then return true.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool HasAdminPermission(this ClaimsPrincipal user)
        {
            if (user.HasClaim(ClaimTypes.Role, PermissionEnum.CanImpersonateUsers.ToString())) return true;
            if (user.HasClaim(ClaimTypes.Role, PermissionEnum.CanManageSystemSettings.ToString())) return true;
            if (user.HasClaim(ClaimTypes.Role, PermissionEnum.CanManageUsers.ToString())) return true;
            return false;
        }

        // Boolean to determine if a user is currently impersonating another user. False if cannot parse/find.
        public static bool IsImpersonating(this ClaimsPrincipal user)
        {
            return user.HasClaim(CustomClaimTypes.IsImpersonating, true.ToString());
        }

        public static int ImpersonationTargetUserID(this ClaimsPrincipal user)
        {
            // Only attempt to parse the target user id if the user has the is impersonation claim.
            if (user.IsImpersonating())
            {
                // Attempt to parse and return if successful.
                if (int.TryParse(user.FindFirst(CustomClaimTypes.TargetUserID).Value, out var targetId))
                {
                    return targetId;
                }
            }

            // Otherwise return 0.
            return 0;
        }
		*/

        public static UserModel GetUserAAD(this ClaimsPrincipal user)
        {
            var result = new UserModel()
            {
                ObjectIdAAD = user.FindFirst(x => x.Type.Contains("objectidentifier")).Value,
                UserName = user.FindFirst(x => x.Type.Equals("preferred_username"))?.Value,
                LastName = user.FindFirst(ClaimTypes.Surname)?.Value,
                FirstName = user.FindFirst(ClaimTypes.GivenName)?.Value,
                DisplayName = user.FindFirst(x => x.Type.Equals("name"))?.Value,
                Email = GetUserAADEmail(user),
                TenantId = user.FindFirst(x => x.Type.Contains("tenantid"))?.Value,
                Roles = string.Join(", ", user.FindAll(x => x.Type.Contains("role")).Select(x => x.Value).ToArray()),
                Scope = user.FindFirst(x => x.Type.Contains("scope"))?.Value
            };
            //apply id - should be present after onlogin handler, it gets set by middleware when request is inbound
            var permission = EnumUtils.GetEnumDescription(PermissionEnum.UserAzureADMapped);
            if (int.TryParse(user.FindFirst(x => x.Type.ToLower().Equals(permission.ToLower()))?.Value, out int oId))
            { 
                result.ID = oId;
            }

            return result;
        }

        private static string GetUserAADEmail(ClaimsPrincipal user)
        {
            if (user.FindFirst(ClaimTypes.Email) != null) return user.FindFirst(ClaimTypes.Email).Value;
            if (user.FindFirst(ClaimTypes.Upn) != null) return user.FindFirst(ClaimTypes.Upn).Value;
            return user.FindFirst(x => x.Type.Equals("preferred_username"))?.Value;
        }

        public static string GetUserIdAAD(this ClaimsPrincipal user)
        {
            return user.FindFirst(x => x.Type.Contains("objectidentifier")).Value;
        }

        /*
        [System.Obsolete("GetUserID is obsolete", true)]
        public static string GetUserID(this ClaimsPrincipal user)
        {
            if (user.IsImpersonating())
            {
                return user.ImpersonationTargetUserID();
            }

            // Value cannot be null if user is authorized. If it is null, let it error occur as this would be a serious problem.
            //TBD - why is user.Identity.Name == null, There are claims but this is not transferring over to the expected identity.name as the user's id 
            //return int.Parse(user.Identity.Name);
            return int.Parse(user.FindFirst(ClaimTypes.Sid).Value);
        }
		*/

        /*
        /// <summary>
        /// This method allows for a simple access to the real user's ID regardless of impersonation.
        /// </summary>
        /// <param name="user">The User</param>
        /// <returns>The real, non-impersonated UserID.</returns>
        public static int GetRealUserID(this ClaimsPrincipal user)
        {
            //TBD - why is user.Identity.Name == null, There are claims but this is not transferring over to the expected identity.name as the user's id 
            //return int.Parse(user.Identity.Name);
            return int.Parse(user.FindFirst(ClaimTypes.Sid).Value);
        }

        /// <summary>
        /// Get list of sites associated with user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static List<int> GetSiteIDs(this ClaimsPrincipal user)
        {
            string idList = user.IsImpersonating() ?
                user.FindFirst(CustomClaimTypes.TargetSiteIDs).Value :
                user.FindFirst(ClaimTypes.PrimarySid).Value;
            return string.IsNullOrEmpty(idList) ? new List<int>() :
                   idList.Split(',').Select<string, int>(int.Parse).ToList();
        }
		*/

        public static DAL.UserToken GetDalUserToken(this ClaimsPrincipal user, int userId)
        {
            return new DAL.UserToken { UserId = userId };
        }
    }
}
