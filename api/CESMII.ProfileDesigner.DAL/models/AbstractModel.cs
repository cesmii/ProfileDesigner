namespace CESMII.ProfileDesigner.DAL.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public abstract class AbstractModel
    {
        // This must be nullable for the Entity Framework to properly assign ids across entities
        // In the data base the column is not nullable
        public int? ID { get; set; }
    }

    public abstract class AbstractProfileDesignerModel : AbstractModel
    {
        public UserSimpleModel CreatedBy { get; set; }

        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime Created { get; set; }
        public UserSimpleModel UpdatedBy { get; set; }

        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? Updated { get; set; }

        public bool IsActive { get; set; }
    }
}