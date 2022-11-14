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

            if (context.User.HasClaim(x => x.Type.ToLower().Equals(permission.ToLower())))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}