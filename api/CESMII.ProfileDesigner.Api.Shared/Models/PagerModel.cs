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
    public class CloudLibFilterModel: PagerFilterSimpleModel
    {
        /// <summary>
        /// A list of search categories each with a set of filters. These can be combined to allow a user to 
        /// search for multiple criteria within a group and combine groups to filter in complex manners. 
        /// For instance, filter on (my stuff OR this publisher's stuff) AND (category = blah)
        /// </summary>
        public List<LookupGroupByModel> Filters { get; set; }

        /// <summary>
        /// This is the cursor of the item after which to query (usually last item of the previous page)
        /// </summary>
        public string Cursor { get; set; }
        /// <summary>
        /// if true, indicates backwards paging (cursor is usually the first item of the current page)
        /// </summary>
        public bool PageBackwards { get; set; }
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
