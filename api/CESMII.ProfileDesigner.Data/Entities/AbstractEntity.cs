namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public abstract class AbstractEntity
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column(name: "id")]
        public int? ID { get; set; }
    }
    public abstract class AbstractEntityWithTenant : AbstractEntity
    {
        [Column(name: "owner_id")]
        public int? OwnerId { get; set; }
    }


    public interface IProfileDesignerAbstractEntity
    {
        [Column(name: "created_by_id")]
        int CreatedById { get; set; }

        [Column(name: "created")]
        DateTime Created { get; set; }

        User CreatedBy { get; set; }

        [Column(name: "updated_by_id")]
        int UpdatedById { get; set; }

        [Column(name: "updated")]
        DateTime? Updated { get; set; }

        User UpdatedBy { get; set; }

        [Column(name: "is_active")]
        bool IsActive { get; set; }

    }

    public abstract class ProfileDesignerAbstractEntity : AbstractEntity, IProfileDesignerAbstractEntity
    {
        [Column(name: "created_by_id")]
        public int CreatedById { get; set; }

        [Column(name: "created")]
        public DateTime Created { get; set; }

        public virtual User CreatedBy { get; set; }

        [Column(name: "updated_by_id")]
        public int UpdatedById { get; set; }

        [Column(name: "updated")]
        public DateTime? Updated { get; set; }

        public virtual User UpdatedBy { get; set; }

        [Column(name: "is_active")]
        public virtual bool IsActive { get; set; }

    }

    public abstract class ProfileDesignerAbstractEntityWithTenant : AbstractEntityWithTenant, IProfileDesignerAbstractEntity
    {
        [Column(name: "created_by_id")]
        public int CreatedById { get; set; }

        [Column(name: "created")]
        public DateTime Created { get; set; }

        public virtual User CreatedBy { get; set; }

        [Column(name: "updated_by_id")]
        public int UpdatedById { get; set; }

        [Column(name: "updated")]
        public DateTime? Updated { get; set; }

        public virtual User UpdatedBy { get; set; }

        [Column(name: "is_active")]
        public virtual bool IsActive { get; set; }

    }

}