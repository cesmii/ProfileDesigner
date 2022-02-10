namespace CESMII.ProfileDesigner.Api.Shared.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;

    using CESMII.ProfileDesigner.Common.Enums;
    using CESMII.ProfileDesigner.Common.Utils;

    public static class UserExtension
    {
        public static bool HasPermission(this ClaimsPrincipal user, PermissionEnum permission)
        {
            return user.HasClaim(ClaimTypes.Role, EnumUtils.GetEnumDescription(permission));
        }

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

        public static int GetUserID(this ClaimsPrincipal user)
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

        public static DAL.UserToken DalUserToken(this ClaimsPrincipal user)
        {
            return new DAL.UserToken { UserId = user.GetUserID(), };
        }
    }
}
