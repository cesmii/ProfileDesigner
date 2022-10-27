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
    public class PagerKeywordsFilterModel : PagerModel
    {
    }
    public class CloudLibFilterModel
    {
        /// <summary>
        /// This is the start index
        /// </summary>
        public int Take { get; set; }

        /// <summary>
        /// This is the number of items to include in the page
        /// </summary>
        public string Cursor { get; set; }
        public List<string> Keywords { get; set; }
        /// <summary>
        /// Adds any profiles in the user's library to the returned list if they are not already in the cloud library result
        /// </summary>
        public bool AddLocalLibrary { get; set; }
        /// <summary>
        /// Removes any profiles that are already in the user's library from the returned list
        /// </summary>
        public bool ExcludeLocalLibrary { get; set; }
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

    public class ProfileFilterModel : PagerFilterSimpleModel
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
