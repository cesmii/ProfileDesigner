namespace CESMII.ProfileDesigner.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class ServiceCodeModel : AbstractProfileDesignerModel 
    {
        [Required(ErrorMessage = "Required")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Required")]
        public string Modifier { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Invalid")]
        public int? MinUnit { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Invalid")]
        public int? MaxUnit { get; set; }

        [Required(ErrorMessage = "Required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid")]
        public int DefaultUnit { get; set; }

        [Required(ErrorMessage = "Required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid")]
        public string CodeNameConcatenated { get; set; }

    }

}