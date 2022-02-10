namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    public class LookupItem : AbstractEntityWithTenant 
    {
        [Column(name: "name")]
        public string Name { get; set; }

        [Column(name: "code")]
        public string Code { get; set; }

        [Column(name: "type_id")]
        public int? TypeId { get; set; }

        public virtual LookupType LookupType { get; set; }

        [Column(name: "display_order")]
        public int DisplayOrder { get; set; }

        [Column(name: "is_active")]
        public bool IsActive { get; set; }
    }
}