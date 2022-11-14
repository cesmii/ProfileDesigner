namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Profile : AbstractEntityWithTenant
    {
        [Column(name: "namespace")]
        public string Namespace { get; set; }

        [Column(name: "version")]
        public string Version { get; set; }

        [Column(name: "publish_date", TypeName = "Date")]
        public DateTime? PublishDate { get; set; }

        [Column(name: "ua_standard_profile_id")]
        public int? StandardProfileID { get; set; }

        public virtual StandardNodeSet StandardProfile { get; set; }

        public virtual List<ImportProfileWarning> ImportWarnings { get; set; }

        [Column(name: "author_id")]
        public int? AuthorId { get; set; }

        /// <summary>
        /// TBD - this may not be a user in the system
        /// </summary>
        public virtual User Author { get; set; }

        // Many-to-many relationship: EF managed
        virtual public List<NodeSetFile> NodeSetFiles { get; set; }

    }
}
