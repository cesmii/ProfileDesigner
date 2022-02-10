namespace CESMII.ProfileDesigner.Api.Shared.Models
{
    using System;
    using System.Collections.Generic;
    using CESMII.ProfileDesigner.Common.Enums;
    using CESMII.ProfileDesigner.DAL.Models;

    public class AppLookupModel
    {
        public List<LookupItemModel> ProfileTypes { get; set; }
        public List<EngineeringUnitRankedModel> EngUnits { get; set; }
        public List<LookupItemModel> AttributeTypes { get; set; }
        public List<LookupDataTypeRankedModel> DataTypes { get; set; }
        public List<LookupDataTypeModel> Structures { get; set; }
    }
}