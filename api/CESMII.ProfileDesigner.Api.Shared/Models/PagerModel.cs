namespace CESMII.ProfileDesigner.Api.Shared.Models
{
    using System.Collections.Generic;
    using CESMII.ProfileDesigner.Common.Enums;
    using CESMII.ProfileDesigner.DAL.Models;

    public class PagerModel
    {
        /// <summary>
        /// This is the start index
        /// </summary>
        public int Take { get; set; }

        /// <summary>
        /// This is the number of items to include in the page
        /// </summary>
        public int Skip { get; set; }

    }

    public class PagerFilterSimpleModel : PagerModel
    {
        public string Query { get; set; }
    }

    public class PagerFilterLookupModel : PagerFilterSimpleModel
    {
        public int TypeId { get; set; }
    }

    public class ProfileTypeDefFilterModel : PagerFilterSimpleModel
    {
        public List<LookupGroupByModel> Filters { get; set; }

        public SearchCriteriaSortByEnum SortByEnum { get; set; } = SearchCriteriaSortByEnum.Name;
    }

    public class LookupGroupByModel : LookupTypeModel
    {
        public List<LookupItemFilterModel> Items { get; set; }
    }

    public class LookupItemFilterModel : LookupItemModel
    {
        public bool Selected { get; set; } = false;
        public bool Visible { get; set; } = true;
    }


}
