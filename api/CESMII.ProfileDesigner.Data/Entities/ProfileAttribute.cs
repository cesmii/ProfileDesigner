namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class ProfileAttributeBase : ProfileDesignerAbstractEntity
    {
        /// <summary>
        /// This identifier is purely used in OPC UA nodesets. This will help us identify a node
        /// using something other than name and namespace. 
        /// </summary>
        [Column(name: "opc_node_id")]
        public string OpcNodeId { get; set; }
        [Column(name: "opc_browse_name")]
        public string BrowseName { get; set; }
        [Column(name: "symbolic_name")]
        public string SymbolicName { get; set; }

        [Column(name: "namespace")]
        public string Namespace { get; set; }

        /// <summary>
        /// Profile can have many properties (attributes) or data variables. Attribute type allows us to indicate which type of attribute this is. 
        /// Properties. These items are relatively static and typically primitive properties (ie. serial number.) over the life of the object instance.
        /// Data Variables. These items are more dynamic in the object instance (ie. RPM, temperature.).  
        /// </summary>
        /// <remarks>
        /// This also allows for other types to be assigned should the need present itself. 
        /// </remarks>
        [Column(name: "attribute_type_id")]
        public int? AttributeTypeId { get; set; }

        /// <summary>
        /// Profile can have many properties (attributes) or data variables. Attribute type allows us to indicate which type of attribute this is. 
        /// Properties. These items are relatively static and typically primitive properties (ie. serial number.) over the life of the object instance.
        /// Data Variables. These items are more dynamic in the object instance (ie. RPM, temperature.).  
        /// </summary>
        /// <remarks>
        /// This also allows for other types to be assigned should the need present itself. 
        /// </remarks>
        public virtual LookupItem AttributeType { get; set; }

        [Column(name: "profile_type_definition_id")]
        public virtual int? ProfileTypeDefinitionId { get; set; }

        [Column(name: "variable_type_definition_id")]
        public virtual int? VariableTypeDefinitionId { get; set; }
        public virtual ProfileTypeDefinition VariableTypeDefinition { get; set; }

        [Column(name: "name")]
        public string Name { get; set; }

        [Column(name: "min_value")]
        public decimal? MinValue { get; set; }

        [Column(name: "max_value")]
        public decimal? MaxValue { get; set; }

        [Column(name: "instrument_min_value")]
        public decimal? InstrumentMinValue { get; set; }
        [Column(name: "instrument_max_value")]
        public decimal? InstrumentMaxValue { get; set; }
        [Column(name: "instrument_range_nodeid")]
        public string InstrumentRangeOpcNodeId { get; set; }
        [Column(name: "instrument_range_modeling_rule")]
        public string InstrumentRangeModelingRule { get; set; }
        [Column(name: "instrument_range_access_level")]
        public uint? InstrumentRangeAccessLevel { get; set; }


        [Column(name: "enum_value")]
        public long? EnumValue { get; set; }

        [Column(name: "eng_unit_id")]
        public int? EngUnitId { get; set; }
        [Column(name: "eng_unit_nodeid")]
        public string EngUnitOpcNodeId { get; set; }
        [Column(name: "eng_unit_modeling_rule")]
        public string EngUnitModelingRule { get; set; }
        [Column(name: "eng_unit_access_level")]
        public uint? EngUnitAccessLevel { get; set; }

        public virtual EngineeringUnit EngUnit { get; set; }

        [Column(name: "eu_range_nodeid")]
        public string EURangeOpcNodeId { get; set; }
        [Column(name: "eu_range_modeling_rule")]
        public string EURangeModelingRule { get; set; }
        [Column(name: "eu_range_access_level")]
        public uint? EURangeAccessLevel { get; set; }
        [Column(name: "minimum_sampling_interval")]
        public double? MinimumSamplingInterval { get; set; }

        [Column(name: "data_type_id")]
        public int? DataTypeId { get; set; }

        public virtual LookupDataType DataType { get; set; }

        [Column(name: "data_variable_nodeids")]
        public string DataVariableNodeIds { get; set; }

        [Column(name: "description")]
        public string Description { get; set; }

        [Column(name: "display_name")]
        public string DisplayName { get; set; }

        [Column(name: "is_array")]
        public bool IsArray { get; set; }
        [Column(name: "value_rank")]
        public int? ValueRank { get; set; }
        [Column(name: "array_dimensions")]
        public string ArrayDimensions { get; set; }
        [Column(name: "max_string_length")]
        public uint? MaxStringLength { get; set; }
        [Column(name: "is_required")]
        public bool? IsRequired { get; set; }
        [Column(name: "allow_sub_types")]
        public bool? AllowSubTypes { get; set; }
        [Column(name: "modeling_rule")]
        public string ModelingRule { get; set; }

        [Column(name: "access_level")]
        public uint? AccessLevel { get; set; }
        [Column(name: "access_restrictions")]
        public ushort? AccessRestrictions { get; set; }
        [Column(name: "write_mask")]
        public uint? WriteMask { get; set; }
        [Column(name: "user_write_mask")]
        public uint? UserWriteMask { get; set; }


        /// <summary>
        /// This allows for additional data to be captured and stored in a JSON string. 
        /// </summary>
        /// <remarks>This is not yet in use by the front end.</remarks>
        [Column(name: "addl_data_json")]
        public string AdditionalData { get; set; }

        [NotMapped]
        public override bool IsActive { get; set; }
    }

    /// <remarks>
    /// This is equivalent to PropertyModel.
    /// </remarks>
    public class ProfileAttribute : ProfileAttributeBase
    {
        public virtual ProfileTypeDefinition ProfileTypeDefinition { get; set; }
    }

}