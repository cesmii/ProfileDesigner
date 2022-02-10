namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// A customized version of a look up item to accomodate special handling of data types
    /// </summary>
    public class LookupDataTypeBase: AbstractEntityWithTenant
    {
        [Column(name: "name")]
        public string Name { get; set; }

        [Column(name: "code")]
        public string Code { get; set; }

        [Column(name: "use_min_max")]
        public bool UseMinMax { get; set; }

        [Column(name: "use_eng_unit")]
        public bool UseEngUnit { get; set; }

        [Column(name: "is_numeric")]
        public bool IsNumeric { get; set; }

        [Column(name: "display_order")]
        public int DisplayOrder { get; set; }

        [Column(name: "is_active")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Optional - Custom types point to profile item of type custom data type. 
        /// FK to profile item table.
        /// </summary>
        [Column(name: "custom_type_id")]
        public int? CustomTypeId { get; set; }

    }

    /// <summary>
    /// A customized version of a look up item to accomodate special handling of data types
    /// </summary>
    public class LookupDataType : LookupDataTypeBase
    {
        /// <summary>
        /// Optional - Custom types point to profile item of type custom data type. 
        /// FK to profile item table.
        /// </summary>
        public virtual ProfileTypeDefinition CustomType { get; set; }

    }

    public class LookupDataTypeRanked : LookupDataTypeBase
    {
        [Column(name: "popularity_level")]
        public int PopularityLevel { get; set; }

        [Column(name: "popularity_index")]
        public int PopularityIndex { get; set; }
        
        [Column(name: "usage_count")]
        public int UsageCount { get; set; }
        
        [Column(name: "manual_rank")]
        public int ManualRank { get; set; }

    }

}