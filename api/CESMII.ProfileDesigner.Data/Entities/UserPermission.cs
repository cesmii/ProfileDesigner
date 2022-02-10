namespace CESMII.ProfileDesigner.Data.Entities
{
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// User to Permission Join Table.
    /// A user may have many permission and a permission may have many users.
    /// </summary>
    public class UserPermission
    {
        // user reference.
        [Column(name: "user_id")]
        public int? UserId { get; set; }

        public virtual User User { get; set; }

        [Column(name: "permission_id")]
        public int? PermissionId { get; set; }

        public virtual Permission Permission { get; set; }
    }
}