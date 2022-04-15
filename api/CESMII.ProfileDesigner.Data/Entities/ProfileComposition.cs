namespace CESMII.ProfileDesigner.Data.Entities
{
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Join table between a profile and a composition (which is a type of profile)
    /// </summary>
    /// <remarks>A profile can can have many compositions and a composition can have many profiles.</remarks>
    public class ProfileComposition : AbstractEntity
    {
        [Column(name: "profile_type_definition_id")]
        public int? ProfileTypeDefinitionId { get; set; }

        [Column(name: "composition_id")]
        public int? CompositionId { get; set; }

        /// <summary>
        /// Different than the profile's name. This is the name for this usage of this as the composition. 
        /// Profile.name could be starter but this composition calls it MyStarter.
        /// </summary>
        [Column(name: "name")]
        public string Name { get; set; }
        [Column(name: "opc_browse_name")]
        public string BrowseName { get; set; }

        // Compositions don't have nodeids. Compare on BrowseName
        //[Column(name: "opc_node_id")]
        //public string OpcNodeId { get; set; }

        [Column(name: "is_required")]
        public bool? IsRequired { get; set; }
        [Column(name: "modeling_rule")]
        public string ModelingRule { get; set; }
        [Column(name: "is_event")]
        public bool? IsEvent { get; set; }

        /// <summary>
        /// Captures the id of custom references
        /// </summary>
        [Column(name: "reference_id")]
        public string ReferenceId { get; set; }

        /// <summary>
        /// Different than the profile's description. This is the description for this usage of this as the composition. 
        /// </summary>
        [Column(name: "description")]
        public string Description { get; set; }

        public virtual ProfileTypeDefinition ProfileTypeDefinition { get; set; }

        public virtual ProfileTypeDefinition Composition { get; set; }
    }
}