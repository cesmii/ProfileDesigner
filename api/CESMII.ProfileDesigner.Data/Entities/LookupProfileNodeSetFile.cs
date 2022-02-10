namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Many-to-many relationship between Profile and NodeSetFile
    /// </summary>
    [Table("profile_nodeset_file")]
    public class LookupProfileNodeSetFile : AbstractEntity
    {
        /// <summary>
        /// FK to profile item table.
        /// </summary>
        [Column(name: "nodeset_file_id")]
        public int NodeSetFileId { get; set; }

        /// <summary>
        /// FK to profile item table.
        /// </summary>
        virtual public NodeSetFile NodeSetFile { get; set; }

        /// <summary>
        /// FK to profile item table.
        /// </summary>
        [Column(name: "profile_id")]
        public int ProfileId { get; set; }

        /// <summary>
        /// FK to profile item table.
        /// </summary>
        virtual public Profile Profile { get; set; }
    }
}