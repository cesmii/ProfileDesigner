namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    public class StandardNodeSet : AbstractEntity
    {
        [Column(name: "namespace")]
        public string Namespace { get; set; }

        [Column(name: "version")]
        public string Version { get; set; }

        [Column(name: "filename")]
        public string Filename { get; set; }


        [Column(name: "publish_date")]
        public DateTime? PublishDate { get; set; }

        //public virtual List<NodeSet> Nodesets { get; set; } 
    }
}
