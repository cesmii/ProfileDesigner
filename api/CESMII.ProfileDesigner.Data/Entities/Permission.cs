namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Permission : AbstractEntity 
    {
        [Column(name: "name")]
        public string Name { get; set; }

        [Column(name: "codename")]
        public int CodeName { get; set; }

        [Column(name: "description")]
        public string Description { get; set; }
    }
}