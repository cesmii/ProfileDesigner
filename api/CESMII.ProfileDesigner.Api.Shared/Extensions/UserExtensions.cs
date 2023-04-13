namespace CESMII.ProfileDesigner.Api.Shared.Extensions
{
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


            string strClaimOrg = $"{permission}_org";

            var strOrgName = user.FindFirst(x => x.Type.ToLower().Equals(strClaimOrg.ToLower()))?.Value;
            if (strOrgName != null)
            {
                result.Organization = new OrganizationModel()
                {
                    Name = strOrgName
                };
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

        public static DAL.UserToken GetDalUserToken(this ClaimsPrincipal user, int userId)
        {
            return new DAL.UserToken { UserId = userId };
        }
    }
}
