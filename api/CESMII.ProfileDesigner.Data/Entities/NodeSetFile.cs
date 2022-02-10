using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CESMII.ProfileDesigner.Data.Entities
{
    public class NodeSetFile : AbstractEntityWithTenant
    {
        [Column(name: "filename")]
        public string FileName { get; set; }

        [Column(name: "version")]
        public string Version { get; set; }

        [Column(name: "publish_date")]
        public DateTime PublicationDate { get; set; }

        [Column(name: "imported_by_id")]
        public int? AuthorId { get; set; }

        [Column(name: "file_cache")]
        public string FileCache { get; set; }

        //// Many-to-many relationship: EF managed
        virtual public List<Profile> Profiles { get; set; }
    }
}

