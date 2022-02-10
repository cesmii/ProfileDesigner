namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Organization : AbstractEntity 
    {
        [Column(name: "name")]
        public string Name { get; set; }

    }

}