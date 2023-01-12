namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class User : AbstractEntity
    {

        /// <summary>
        /// Object Id value stored in Azure. This should not change during lifetime of Azure AD user account.
        /// </summary>
        [Column(name: "objectid_aad")]
        public string ObjectIdAAD { get; set; }
        /// <summary>
        /// Display Name from Azure AD. This is a convenience helper to make it eaiser to display friendly
        /// name within our eco system. This will be updated on each login. 
        /// This is not expected to be unique AND is expected it can change. 
        /// </summary>
        [Column(name: "display_name")]
        public string DisplayName { get; set; }

        [Column(name: "date_joined")]
        public DateTime? Created { get; set; }

        [Column(name: "last_login")]
        public DateTime? LastLogin { get; set; }

        [Column(name: "organization_id")]
        public int? OrganizationId { get; set; }

        [Column(name: "email_address")]
        public string EmailAddress { get; set; }

        public virtual Organization Organization { get; set; }
    }

}