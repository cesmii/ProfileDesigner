namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class User : AbstractEntity 
    {
        [Column(name: "username")]
        public string UserName { get; set; }

        [Column(name: "password")]
        public string Password { get; set; }

        [Column(name: "first_name")]
        public string FirstName { get; set; }

        [Column(name: "last_name")]
        public string LastName { get; set; }

        [Column(name: "email")]
        public string Email { get; set; }

        // User can belong to many permissions
        public virtual List<UserPermission> UserPermissions { get; set; }

        [Column(name: "is_active")]
        public bool IsActive { get; set; }

        [Column(name: "date_joined")]
        public DateTime Created { get; set; }

        [Column(name: "last_login")]
        public DateTime? LastLogin { get; set; }

        [Column(name: "registration_complete")]
        public DateTime? RegistrationComplete { get; set; }

        [Column(name: "organization_id")]
        public int? OrganizationId { get; set; }

        public virtual Organization Organization { get; set; }

    }

}