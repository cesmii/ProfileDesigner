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
        public string ObjectIdAAD { get; set; }
        /// <summary>
        /// Display Name from Azure AD. This is a convenience helper to make it eaiser to display friendly
        /// name within our eco system. This will be updated on each login. 
        /// This is not expected to be unique AND is expected it can change. 
        /// </summary>
        public string DisplayName { get; set; }

        public DateTime? Created { get; set; }

        public DateTime? LastLogin { get; set; }

        [Column(name: "organization_id")]
        public int? OrganizationId { get; set; }

        public virtual Organization Organization { get; set; }

    }

}