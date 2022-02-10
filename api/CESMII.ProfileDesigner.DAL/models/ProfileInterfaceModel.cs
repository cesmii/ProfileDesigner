namespace CESMII.ProfileDesigner.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class ProfileInterfaceModel : AbstractProfileDesignerModel
    {
        [Range(1, Int32.MaxValue, ErrorMessage = "Required")]
        public int ProfileId { get; set; }

        [Range(1, Int32.MaxValue, ErrorMessage = "Required")]
        public int InterfaceId { get; set; }

        public ProfileTypeDefinitionModel Interface { get; set; }
    }
}