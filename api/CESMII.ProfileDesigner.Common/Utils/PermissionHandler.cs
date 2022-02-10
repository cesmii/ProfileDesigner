namespace CESMII.ProfileDesigner.Common.Utils
{
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;

    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var permission = EnumUtils.GetEnumDescription(requirement.Permission);
            if (context.User.HasClaim(ClaimTypes.Role, permission))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}