namespace CESMII.ProfileDesigner.DAL.Models
{
    public class LookupDataTypeModel : AbstractModel
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public bool UseMinMax { get; set; }

        public bool UseEngUnit { get; set; }

        public bool IsNumeric { get; set; }

        public int DisplayOrder { get; set; } = 9999;  //default to same value for all new data types. This will help downstream on sorting with other data types.

        public bool IsActive { get; set; }

        /// <summary>
        /// Optional - Custom types point to profile item of type custom data type. 
        /// FK to profile item table.
        /// </summary>
        public int? CustomTypeId { get; set; }

        /// <summary>
        /// Optional - Custom types point to profile item of type custom data type. 
        /// FK to profile item table.
        /// </summary>
        public ProfileTypeDefinitionModel CustomType { get; set; }

        /// <summary>
        /// Optional - If present, only current user should see this data type. 
        /// A user can create a custom data type for their stuff.  
        /// FK to user's table.
        /// </summary>
        public int? OwnerId { get; set; }

    }

    public class LookupDataTypeRankedModel : LookupDataTypeModel
    {
        /// <summary>
        /// Parent (base type) of the data type
        /// FK to data type table.
        /// </summary>
        public int? BaseDataTypeId { get; set; }

        public int PopularityLevel { get; set; }
        public int PopularityIndex { get; set; }
        public int UsageCount { get; set; }
        public int ManualRank { get; set; }
        public bool IsCustom { 
            get
            { 
                return this.CustomTypeId.HasValue; 
            }
        }
    }
}