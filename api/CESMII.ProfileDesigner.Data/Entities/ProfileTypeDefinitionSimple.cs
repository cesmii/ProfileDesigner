namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Streamlined, Special instance of profile type definitions
    /// Used in stored procedures which perform a complex action and pull 
    /// back rows of data in a flat,, simplified manner. 
    /// </summary>
    public class ProfileTypeDefinitionSimple : AbstractEntity
    {
        [Column(name: "browse_name")]
        public string BrowseName { get; set; }

        [Column(name: "description")]
        public string Description { get; set; }

        [Column(name: "is_abstract")]
        public bool IsAbstract { get; set; }

        [Column(name: "name")]
        public string Name { get; set; }

        [Column(name: "opc_node_id")]
        public string OpcNodeId { get; set; }

        [Column(name: "parent_id")]
        public int? ParentId { get; set; }

        [Column(name: "type_id")]
        public int TypeId { get; set; }

        [Column(name: "type_name")]
        public string TypeName { get; set; }

        [Column(name: "variable_data_type_id")]
        public int? VariableDataTypeId { get; set; }

        [Column(name: "profile_id")]
        public int ProfileId { get; set; }

        [Column(name: "profile_author_id")]
        public int? AuthorId { get; set; }

        [Column(name: "profile_author_objectid_aad")]
        public string AuthorObjectIdAAD { get; set; }

        [Column(name: "profile_namespace")]
        public string ProfileNamespace { get; set; }

        [Column(name: "profile_owner_id")]
        public int? OwnerId { get; set; }

        [Column(name: "profile_publish_date")]
        public DateTime? ProfilePublishDate { get; set; }

        [Column(name: "profile_title")]
        public string Title { get; set; }

        [Column(name: "profile_version")]
        public string ProfileVersion { get; set; }

        [Column(name: "level")]
        public int Level { get; set; }
    }

}