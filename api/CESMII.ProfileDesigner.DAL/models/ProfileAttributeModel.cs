namespace CESMII.ProfileDesigner.DAL.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class ProfileAttributeModel : AbstractProfileDesignerModel
    {
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
        public string SymbolicName { get; set; }
        public string Namespace { get; set; }

        [Required(ErrorMessage = "Required")]
        public string Name { get; set; }

        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public decimal? InstrumentMinValue { get; set; }
        public decimal? InstrumentMaxValue { get; set; }

        /// <summary>
        /// Value of an enum field for AttributeType AttributeTypeIdEnum.EnumField, and the field order for AttributeType AttributeTypeIdEnum.StructureField)
        /// </summary>
        public long? EnumValue { get; set; }

        public virtual EngineeringUnitModel EngUnit { get; set; }
        public string EngUnitOpcNodeId { get; set; }
        public string EngUnitModelingRule { get; set; }
        public uint? EngUnitAccessLevel { get; set; }
        public string EURangeOpcNodeId { get; set; }
        public string EURangeModelingRule { get; set; }
        public uint? EURangeAccessLevel { get; set; }

        /// <summary>
        /// Profile can have many properties (attributes) or data variables. Attribute type allows us to indicate which type of attribute this is. 
        /// Properties. These items are relatively static and typically primitive properties (ie. serial number.) over the life of the object instance.
        /// Data Variables. These items are more dynamic in the object instance (ie. RPM, temperature.).  
        /// See <see cref="AttributeTypeIdEnum"/> for ID values.
        /// </summary>
        /// <remarks>
        /// This also allows for other types to be assigned should the need present itself. 
        /// </remarks>
        public LookupItemModel AttributeType { get; set; }

        /// <summary>
        ///  The profile type to which this attribute belongs
        /// </summary>
        public int? TypeDefinitionId { get; set; }

        public virtual ProfileTypeDefinitionModel TypeDefinition { get; set; }

        /// <summary>
        /// The VariableType for the attribute: indicates Property vs Datavariable (or derived)
        /// </summary>
        public int? VariableTypeDefinitionId { get; set; }

        public virtual ProfileTypeDefinitionModel VariableTypeDefinition { get; set; }

        //[Range(1, Int32.MaxValue, ErrorMessage = "Required")]
        /// <summary>
        /// The types are listed in the DB for common types (ie bool, string, int, float, etc. )
        /// </summary>
        /// <remarks>If the attribute uses a custom data type, there is a data type named custom data type w/in this list.</remarks>
        public int? DataTypeId { get; set; }

        /// <summary>
        /// The types are listed in the DB for common types (ie bool, string, int, float, etc. )
        /// </summary>
        /// <remarks>If the attribute uses a custom data type, there is a data type named custom data type w/in this list.</remarks>
        public virtual LookupDataTypeModel DataType { get; set; }

        /// <summary>
        /// Comma separated list of nodeids to use for sub-datavariables as defined by VariableTypeDefinition (workaround for preserving nodeids across import/export)
        /// </summary>
        public string DataVariableNodeIds { get; set; }

        public int? CompositionId { get; set; }

        public virtual ProfileTypeDefinitionRelatedModel Composition { get; set; }

        public string Description { get; set; }

        public string DisplayName { get; set; }

        public bool IsDeleted { get; set; }

        /// <summary>
        /// This will help the front end UI color code like interfaces together. 
        /// This will be comarable to the interface index.
        /// </summary>
        public int? InterfaceGroupId { get; set; }
        public virtual ProfileTypeDefinitionRelatedModel Interface { get; set; }

        /// <summary>
        /// This will help the front end detect an attribute assoc w/ an interface more easily.
        /// </summary>
        public int? InterfaceId
        {
            get
            {
                return this.Interface == null ? null : this.Interface.ID;
            }
        }

        public bool IsArray { get; set; }
        public int? ValueRank { get; set; }
        public string ArrayDimensions { get; set; }
        public uint? MaxStringLength { get; set; }
        public bool? IsRequired { get; set; }
        public string ModelingRule { get; set; }
        public double? MinimumSamplingInterval { get; set; }

        public uint? AccessLevel { get; set; }
        public ushort? AccessRestrictions { get; set; }
        public uint? WriteMask { get; set; }
        public uint? UserWriteMask { get; set; }

        /// <summary>
        /// This allows for additional data to be captured and stored in a JSON string. 
        /// </summary>
        /// <remarks>This is not yet in use by the front end.</remarks>
        public string AdditionalData { get; set; }

        public override string ToString() => $"{Name} ({Namespace};{OpcNodeId})";
    }

}