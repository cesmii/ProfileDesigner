namespace CESMII.ProfileDesigner.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class OrganizationModel : AbstractModel
    {
        [Required(ErrorMessage = "Required")]
        public string Name { get; set; }
    }
}