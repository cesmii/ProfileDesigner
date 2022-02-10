namespace CESMII.ProfileDesigner.Common.Utils
{
    using Microsoft.AspNetCore.Authorization;

    using CESMII.ProfileDesigner.Common.Enums;

    public class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionRequirement(PermissionEnum permission)
        {
            Permission = permission;
        }

        public PermissionEnum Permission { get; }
    }
}
