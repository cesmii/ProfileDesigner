namespace CESMII.ProfileDesigner.DAL.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using CESMII.ProfileDesigner.Common.Enums;

    public class LookupItemModel : AbstractModel
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public LookupTypeEnum LookupType { get; set; }

        public int? TypeId { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; }
    }

}