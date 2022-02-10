namespace CESMII.ProfileDesigner.DAL.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using CESMII.ProfileDesigner.Common.Enums;

    public class EngineeringUnitModel : AbstractModel
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string NamespaceUri { get; set; }
        public int? UnitId{ get; set; }
        public bool IsActive { get; set; }

    }

    public class EngineeringUnitRankedModel : EngineeringUnitModel
    {
        public int PopularityLevel { get; set; }
        public int PopularityIndex { get; set; }
        public int UsageCount { get; set; }
        public int ManualRank { get; set; }
    }

}