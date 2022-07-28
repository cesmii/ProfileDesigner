namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class ProfileTypeDefinitionBase : ProfileDesignerAbstractEntityWithTenant
    {
        #region OPC UA specific Fields
        /// <summary>
        /// This identifier is purely used in OPC UA nodesets. This will help us identify a node
        /// using something other than name and namespace. 
        /// </summary>
        [Column(name: "opc_node_id")]
        public string OpcNodeId { get; set; }

        /// <summary>
        /// In most cases, browse name is the same as name but not always. 
        /// </summary>
        [Column(name: "browse_name")]
        public string BrowseName { get; set; }

        /// <summary>
        /// Symbolic name would be used for code generation scenarios. 
        /// </summary>
        [Column(name: "symbolic_name")]
        public string SymbolicName { get; set; }

        /// <summary>
        /// A url that points to documentation of the node set.  
        /// </summary>
        [Column(name: "document_url")]
        public string DocumentUrl { get; set; }

        [Column(name: "is_abstract")]
        public bool IsAbstract { get; set; }

        #endregion

        [Column(name: "name")]
        public string Name { get; set; }

        //TBD - change this to not nullable...
        [Column(name: "profile_id")]
        public int? ProfileId { get; set; }

        public virtual Profile Profile { get; set; }

        [Column(name: "description")]
        public string Description { get; set; }

        /// <remarks>
        /// This is equivalent to super in the nodeset.
        /// </remarks>
        [Column(name: "parent_id")]
        public int? ParentId { get; set; }

        [Column(name: "instance_parent_id")]
        public int? InstanceParentId { get; set; }

        [Column(name: "is_option_set")] 
        public bool? IsOptionSet { get; set; }
        [Column(name: "variable_data_type_id")]
        public int? VariableDataTypeId {get;set;}
        [Column(name: "variable_value_rank")]
        public int? VariableValueRank{ get; set; }
        [Column(name: "variable_array_dimensions")]
        public string VariableArrayDimensions { get; set; }
        [Column(name: "variable_value")]
        public string VariableValue{ get; set; }

        /// <summary>
        /// Profile can be a type of class, interface or variable type
        /// </summary>
        [Column(name: "type_id")]
        public int? ProfileTypeId { get; set; }

        public virtual LookupItem ProfileType { get; set; }

        /// <summary>
        /// This may be created by or updated by but not always
        /// </summary>
        [Column(name: "author_id")]
        public int? AuthorId { get; set; }

        /// <summary>
        /// TBD - this may not be a user in the system
        /// </summary>
        public virtual User Author { get; set; }

        /// <summary>
        /// The author may not be someone within the system. In this case, show as a simple string field.
        /// </summary>
        [Column(name: "author_name")]
        public string ExternalAuthor { get; set; }

        /// <summary>
        /// profile can have many metatags - this is stored and retrieved in JSON format as a list of strings
        /// </summary>
        [Column(name: "metatags")]
        public string MetaTags { get; set; }

        [NotMapped]
        public override bool IsActive { get; set; }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// This is equivalent to ObjectTypeModel.
    /// </remarks>
    public class ProfileTypeDefinition : ProfileTypeDefinitionBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// This is mapped to SuperType in the importer class.
        /// </remarks>
        public virtual ProfileTypeDefinition Parent { get; set; }

        public virtual ProfileTypeDefinition InstanceParent { get; set; }

        public virtual ProfileTypeDefinition VariableDataType { get; set; }

        ///// <remarks>
        ///// This is mapped to SubTypes in the importer class.
        ///// </remarks>
        //public virtual List<ProfileTypeDefinition> Children { get; set; }

        /// <summary>
        /// A profile can implement many interfaces 
        /// </summary>
        /// <remarks>
        /// This is mapped to Properties in the importer class.
        /// </remarks>
        public virtual List<ProfileInterface> Interfaces { get; set; }

        /// <summary>
        /// Profile can have many properties (attributes) or data variables. 
        /// Properties. These items are relatively static and typically primitive properties (ie. serial number.) over the life of the object instance.
        /// Data Variables. These items are more dynamic in the object instance (ie. RPM, temperature.).  
        /// These are distinguished by AttributeTypeId
        /// </summary>
        /// <remarks>
        /// This is mapped to Properties & Data Variables in the importer class.
        /// </remarks>
        public virtual List<ProfileAttribute> Attributes { get; set; }

        /// <summary>
        /// Profile can have many compositions.
        /// These are typically Sub-systems (The engine profile has a starter, a control unit). Each sub-system is itself its own profile.
        /// (ie. starter, control unit). These are complete sub-system that could be used by other profiles. 
        /// </summary>
        /// <remarks>
        /// This is mapped to Objects in the importer class.
        /// </remarks>
        public virtual List<ProfileComposition> Compositions { get; set; }

        public override string ToString() => $"{Name} {ID}";

        public virtual ProfileTypeDefinitionFavorite Favorite { get; set; }

        public virtual ProfileTypeDefinitionAnalytic Analytics { get; set; }
    }


    /// <summary>
    /// A profile which is represented as a composition on another profile. 
    /// </summary>
    public class Composition : ProfileTypeDefinitionBase
    {
        /// <summary>
        /// profile can implement many interfaces 
        /// </summary>
        public virtual List<ProfileComposition> Compositions { get; set; }
    }

}