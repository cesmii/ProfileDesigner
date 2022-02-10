namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    public class EngineeringUnit : AbstractEntityWithTenant 
    {
        [Column(name: "display_name")]
        public string DisplayName { get; set; }

        [Column(name: "description")]
        public string Description { get; set; }
        [Column(name: "namespace_uri")]
        public string NamespaceUri { get; set; }

        [Column(name: "unit_id")]
        public int? UnitId { get; set; }

        [Column(name: "is_active")]
        public bool IsActive { get; set; }

    }

    public class EngineeringUnitRanked : EngineeringUnit
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