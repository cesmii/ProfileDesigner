namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    public class LookupType : AbstractEntity
    {
        [Column(name: "name")]
        public string Name { get; set; }
    }
}