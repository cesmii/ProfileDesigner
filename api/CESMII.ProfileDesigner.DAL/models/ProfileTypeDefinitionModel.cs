namespace CESMII.ProfileDesigner.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// TBD - handle extended attributes for compositions and variable types now that they are split out.
    /// </remarks>
    public class ProfileTypeDefinitionModel : AbstractProfileDesignerModel
    {
        [Required(ErrorMessage = "Required")]
        public new int? ID { get => base.ID; set { base.ID = value; } }

        #region OPC UA specific Fields
        /// <summary>
        /// This identifier is purely used in OPC UA nodesets. This will help us identify a node
        /// using something other than name and namespace. 
        /// </summary>
        public string OpcNodeId { get; set; }

        /// <summary>
        /// In most cases, browse name is the same as name but not always.
        /// Syntax: opcNamespaceUri:name
        /// </summary>
        public string BrowseName { get; set; }

        /// <summary>
        /// Symbolic name would be used for code generation scenarios. 
        /// </summary>
        public string SymbolicName { get; set; }

        /// <summary>
        /// A url that points to documentation of the node set.  
        /// </summary>
        public string DocumentUrl { get; set; }

        public bool IsAbstract { get; set; }

        #endregion

        [Required(ErrorMessage = "Required")]
        public string Name { get; set; }

        //[Range(1, Int32.MaxValue, ErrorMessage = "Required")]
        public int? ProfileId { get; set;}

        public ProfileModel Profile{ get; set; }

        public string Description { get; set; }

        public ProfileTypeDefinitionSimpleModel Parent { get; set; }

        // OPC Objects have a parent in the type hierarchy (Parent property) and a parent in the instance hierarchy (InstanceParent)
        virtual public ProfileTypeDefinitionModel InstanceParent { get; set; }

        // OPC enumerations can be marked as an option set (bit mask)
        public bool? IsOptionSet { get; set; }

        // OPC Variable Types have a DataType, independent of the variable type inheritance hierarchy
        virtual public ProfileTypeDefinitionModel VariableDataType { get; set; }
        public int? VariableValueRank { get; set; }
        public string VariableArrayDimensions { get; set; }
        public string VariableValue { get; set; }

        /// <summary>
        /// Valid values defined for convenience in ProfileItemTypeEnum
        /// </summary>
        [Range(1, Int32.MaxValue, ErrorMessage = "Required")]
        public int? TypeId { get; set; }

        public LookupItemModel Type { get; set; }

        public int? AuthorId { get; set; }

        public UserSimpleModel Author { get; set; }
        
        public string ExternalAuthor { get; set; }

        /// <summary>
        /// TBD - eventually rename this just to MergedAttributes. Requires front end re-factor.
        /// </summary>
        public List<ProfileAttributeModel> ProfileAttributes { get; set; }

        /// <summary>
        /// Profile can have many properties (attributes) or data variables. 
        /// Properties. These items are relatively static and typically primitive properties (ie. serial number.) over the life of the object instance.
        /// Data Variables. These items are more dynamic in the object instance (ie. RPM, temperature.).  
        /// These are distinguished by AttributeTypeId
        /// </summary>
        /// <remarks>
        /// This is mapped to Properties & Data Variables in the importer class.
        /// </remarks>
        public List<ProfileAttributeModel> Attributes { get; set; }

        /// <summary>
        /// TBD - get this data. 
        /// These are attributes from the ancestors of this profile.
        /// </summary>
        public List<ProfileAttributeModel> ExtendedProfileAttributes { get; set; }

        /// <summary>
        /// A profile can implement many interfaces 
        /// </summary>
        /// <remarks>
        /// This is mapped to interfaces in the importer class.
        /// </remarks>
        public virtual List<ProfileTypeDefinitionModel> Interfaces { get; set; }

        /// <summary>
        /// Profile can have many compositions.
        /// These are typically Sub-systems (The engine profile has a starter, a control unit). Each sub-system is itself its own profile.
        /// (ie. starter, control unit). These are complete sub-system that could be used by other profiles. 
        /// </summary>
        /// <remarks>
        /// This is mapped to Objects in the importer class.
        /// </remarks>
        public virtual List<ProfileTypeDefinitionRelatedModel> Compositions { get; set; }

        public List<string> MetaTags { get; set; }

        public string MetaTagsConcatenated { get; set; }

        /// <summary>
        /// Read only list of dependencies. This is informational data only
        /// </summary>
        public List<ProfileTypeDefinitionSimpleModel> Dependencies { get; set; }

        /// <summary>
        /// Read only list of ancestory items. This is informational for front end.
        /// </summary>
        public List<ProfileTypeDefinitionSimpleModel> Ancestory { get; set; }

        public bool? IsFavorite { get; set; }

        public int PopularityIndex { get; set; }

        public bool IsReadOnly{ 
            get
            {
                //on add/update, the controller sets profile to null so it isn't seen as an item to insert/update in 
                //the db. Account for that possibility here. the returned value won't have an impact on the update
                if (this.Profile == null) return false;
                //read only logic for most cases when pulling data and profile is present
                return !this.Profile.AuthorId.HasValue || !string.IsNullOrEmpty(this.Profile.CloudLibraryId);
            }
        }

        public override string ToString()
        {
            return $"{Name} ({Profile?.Namespace};{OpcNodeId})";
        }
    }

    public class ProfileTypeDefinitionSimpleModel : AbstractModel
    {
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public virtual ProfileTypeDefinitionModel ProfileTypeDefinition { get; set; }
        /// <summary>
        /// This identifier is purely used in OPC UA nodesets. This will help us identify a node
        /// using something other than name and namespace. 
        /// </summary>
        public string OpcNodeId { get; set; }

        public bool IsAbstract { get; set; }

        public string Name { get; set; }
        public string BrowseName { get; set; }
        public string SymbolicName { get; set; }
        public string DocumentationUrl { get; set; }
        public List<string> MetaTags { get; set; }

        public int? ProfileId { get; set; }

        public ProfileModel Profile { get; set; }

        public string Description { get; set; }

        public UserSimpleModel Author { get; set; }

        public LookupItemModel Type { get; set; }

        public int? VariableDataTypeId { get; set; }

        /// <summary>
        /// Only used when returning a list of descendants or ancestors and we need to know
        /// how items relate to one another. If item is level 0, then its parent is -1, grandparent -2, child 1, etc. 
        /// </summary>
        public int Level { get; set; }
    }

    public class ProfileTypeDefinitionAncestoryModel : ProfileTypeDefinitionSimpleModel
    {
        public List<ProfileTypeDefinitionAncestoryModel> Children { get; set; }
    }

    public class ProfileTypeDefinitionRelatedModel : ProfileTypeDefinitionSimpleModel
    {
        public bool? IsRequired { get; set; }

        public string ModelingRule { get; set; }

        public int? RelatedProfileTypeDefinitionId { get; set; }
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public ProfileTypeDefinitionModel RelatedProfileTypeDefinition{ get;set;}
        public int? IntermediateObjectId { get; set; }
        public string IntermediateObjectName { get; set; }
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public ProfileTypeDefinitionModel IntermediateObject { get; set; }

        /// <summary>
        /// This is the composition's profile type definition name
        /// </summary>
        /// <remarks>
        /// Note the profile_composition will have its own name specific to this profile_composition.
        /// Still used by front end. This avoids infinte recursion in the JSON structure. 
        /// </remarks>
        public string RelatedName { 
            get 
            {
                return RelatedProfileTypeDefinition?.Name;
            } 
        }

        /// <summary>
        /// This is the composition's profile type definition description.
        /// </summary>
        /// <remarks>
        /// Note the profile_composition can have its own description specific to this profile_composition.
        /// </remarks>
        public string RelatedDescription {
            get
            {
                return RelatedProfileTypeDefinition?.Description;
            }
        }

        [Obsolete("Warning - use IsRequired rather than RelatedIsRequired. Reserve Related to mean the related type definition.")]
        public bool? RelatedIsRequired {
            get
            {
                return IsRequired;
            }
        }

        [Obsolete("Warning - use ModelingRule rather than RelatedModelingRule. Reserve Related to mean the related type definition.")]
        public string RelatedModelingRule {
            get
            {
                return ModelingRule;
            }
        }

        public bool? RelatedIsEvent { get; set; }
        /// <summary>
        /// Captures the id of custom references
        /// </summary>
        public string RelatedReferenceId { get; set; }
        public bool? RelatedReferenceIsInverse { get; set; }
    }

    /// <summary>
    /// When retrieving an interface implemented by a 
    /// profile, we must also get its attributes. 
    /// </summary>
    public class ProfileTypeDefinitionSimpleWithAttrsModel : ProfileTypeDefinitionSimpleModel
    {
        public List<ProfileAttributeModel> ProfileAttributes { get; set; }
    }

    /// <summary>
    /// When retrieving an variable type we must also get the permissible data types. 
    /// </summary>
    public class ProfileTypeDefinitionSimpleWithDataTypesModel : ProfileTypeDefinitionSimpleModel
    {
        public List<ProfileTypeDefinitionSimpleModel> DataTypes { get; set; }
    }

}