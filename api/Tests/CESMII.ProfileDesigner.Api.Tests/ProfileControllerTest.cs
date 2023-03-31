using System.Collections.Generic;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

using Newtonsoft.Json;
using CESMII.ProfileDesigner.Common.Enums;

namespace CESMII.ProfileDesigner.Api.Tests.Integration
{
    [TestCaseOrderer("CESMII.ProfileDesigner.Api.Tests.Integration.ProfileControllerTestCaseOrderer", "CESMII.ProfileDesigner.Api.Tests.Integration")]
    public class ProfileControllerTest : ControllerTestBase
    {
        #region API constants
        private const string URL_ADD = "/api/profile/add";
        private const string URL_LIBRARY = "/api/profile/library";
        private const string URL_GETBYID = "/api/profile/getbyid";
        private const string URL_DELETE = "/api/profile/delete";
        #endregion

        #region data naming constants
        private const string NAMESPACE_PATTERN = "https://CESMII.ProfileDesigner.Api.Test.org/";
        private const string TITLE_PATTERN = "CESMII.ProfileDesigner.Api.Test.";
        private const string CATEGORY_PATTERN = "test";
        private const string VERSION_PATTERN = "1.0.0.";
        #endregion

        private const string _filterPayload = @"{'filters':[{'items':" +
            "[{'selected':false,'visible':true,'name':'My Profiles','code':null,'lookupType':0,'typeId':null,'displayOrder':0,'isActive':false,'id':1}," +
            " {'selected':false,'visible':true,'name':'Cloud Profiles','code':null,'lookupType':0,'typeId':null,'displayOrder':0,'isActive':false,'id':2}," +
            " {'selected':false,'visible':false,'name':'Cloud Library','code':null,'lookupType':0,'typeId':null,'displayOrder':0,'isActive':false,'id':3}],'name':'Source','id':1}" +
            "]" +
            ",'sortByEnum':3,'query':null,'take':25,'skip':0}";

        public ProfileControllerTest(CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup> factory, ITestOutputHelper output):
            base(factory, output)
        {
        }

#pragma warning disable xUnit1026  // Stop warnings related to parameters not used in test cases. 

        [Theory]
        [MemberData(nameof(ProfileControllerTestData))]
        public async Task AddItem(DAL.Models.ProfileModel model)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;

            // ACT
            //add an item
            var result = await apiClient.ApiExecuteAsync<Shared.Models.ResultMessageWithDataModel>(URL_ADD, model);

            //ASSERT
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [MemberData(nameof(ProfileControllerTestEditData))]
        public async Task GetItem(int i, string nameSpace)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            var model = new Shared.Models.IdIntModel { ID = await GetItemIdByNamespace(nameSpace) };

            // ACT
            //get an item - first search to retrieve by title then get by id
            var result = await apiClient.ApiGetItemAsync<DAL.Models.ProfileModel>(URL_GETBYID, model);

            //ASSERT
            Assert.Equal($"{TITLE_PATTERN}{i}", result.Title);
            Assert.Equal($"{NAMESPACE_PATTERN}{i}", result.Namespace);
            Assert.Equal($"{VERSION_PATTERN}{i}", result.Version);
            Assert.Equal($"{CATEGORY_PATTERN}", result.CategoryName);
            Assert.Equal((i % 3 == 0 ? "Other" : (i % 2 == 0) ? "Custom" : "MIT"), result.License);
            Assert.Equal((i % 3 == 0 ? "Unique description for 3" : (i % 2 == 0) ? "Unique description for 2" : "Common description"), result.Description);
            Assert.Equal(System.DateTime.Now.Year, result.PublishDate.Value.Year);
        }

        [Theory]
        [MemberData(nameof(ProfileControllerTestDeleteData))]
        public async Task DeleteItem(int i, string nameSpace)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            var model = new Shared.Models.IdIntModel { ID = await GetItemIdByNamespace(nameSpace) };

            // ACT
            //delete the item
            var result = await apiClient.ApiExecuteAsync<Shared.Models.ResultMessageModel>(URL_DELETE, model);

            //ASSERT
            Assert.True(result.IsSuccess);
            Assert.Contains("item was deleted", result.Message.ToLower());
            //Try to get the item and should throw bad request
            await Assert.ThrowsAsync<MyNamespace.ApiException>(
                async () => await apiClient.ApiGetItemAsync<DAL.Models.ProfileModel>(URL_GETBYID, model));
        }

        [Theory]
        [InlineData(null, 11)]
        [InlineData("/ua/", 2)]
        [InlineData("/di/", 1)]
        [InlineData(NAMESPACE_PATTERN, 7)]
        [InlineData(TITLE_PATTERN, 7)]
        public async Task GetLibrary(string query, int expectedCount)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            //get stock filter
            Shared.Models.ProfileTypeDefFilterModel filter = JsonConvert.DeserializeObject<Shared.Models.ProfileTypeDefFilterModel>
                (_filterPayload); 
            //apply specifics to filter
            filter.Query = query;

            // ACT
            //get the list of type defs
            var items = (await apiClient.ApiGetManyAsync<DAL.Models.ProfileModel>(URL_LIBRARY, filter)).Data;

            //ASSERT
            Assert.Equal(expectedCount, items.Count);
        }

        [Theory]
        [InlineData(null, 7)]
        [InlineData("/ua/", 0)]
        [InlineData("/di/", 0)]
        [InlineData(NAMESPACE_PATTERN, 7)]
        [InlineData(TITLE_PATTERN, 7)]
        public async Task GetLibraryMine(string query, int expectedCount)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            //get stock filter
            Shared.Models.ProfileTypeDefFilterModel filter = JsonConvert.DeserializeObject<Shared.Models.ProfileTypeDefFilterModel>
                (_filterPayload);

            //get profiles that are mine only
            var f = filter.Filters.Find(x => x.Name.ToLower().Equals("source"))?.Items
                .Find(y => y.ID.Equals((int)ProfileSearchCriteriaSourceEnum.Mine));
            f.Selected = true;

            //apply specifics to filter
            filter.Query = query;

            // ACT
            //get the list of type defs
            var items = (await apiClient.ApiGetManyAsync<DAL.Models.ProfileModel>(URL_LIBRARY, filter)).Data;

            //ASSERT
            Assert.Equal(expectedCount, items.Count);
        }

        #region Helper Methods
        /// <summary>
        /// We won't know the id but we need to get the id by searching the db with a specific profile namespace.
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        private async Task<int> GetItemIdByNamespace(string ns)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            //get stock filter
            Shared.Models.ProfileTypeDefFilterModel filter = JsonConvert.DeserializeObject<Shared.Models.ProfileTypeDefFilterModel>
                (_filterPayload);
            //apply specifics to filter
            filter.Query = ns;

            // ACT
            //get the list of type defs
            var items = (await apiClient.ApiGetManyAsync<DAL.Models.ProfileModel>(URL_LIBRARY, filter)).Data;

            //get first item in list
            if (items.Count == 0) throw new System.InvalidOperationException($"Item not found: {ns}");
            return items[0].ID.Value;
        }

        #endregion

        #region Test Data
        public static IEnumerable<object[]> ProfileControllerTestData()
        {
            var result = new List<DAL.Models.ProfileModel[]>();
            for (int i = 1; i <= 10; i++)
            {
                result.Add(new DAL.Models.ProfileModel[] { new DAL.Models.ProfileModel() { 
                    Namespace = $"{NAMESPACE_PATTERN}{i}", 
                    Title = $"{TITLE_PATTERN}{i}", 
                    Version = $"{VERSION_PATTERN}{i}",
                    CategoryName=$"{CATEGORY_PATTERN}",
                    PublishDate=new System.DateTime(System.DateTime.Now.Year, 1, i),
                    License = (i % 3== 0 ? "Other" : (i % 2== 0) ? "Custom" : "MIT"),
                    Description=(i % 3== 0 ? "Unique description for 3" : (i % 2== 0) ? "Unique description for 2" : "Common description")
                }});
            }
            return result;
        }

        /// <summary>
        /// Get a subset of the items we created during add tests
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<object[]> ProfileControllerTestEditData()
        {
            var result = new List<object[]>();
            for (int i = 1; i <= 5; i++)
            {
                result.Add(new object[] { i, $"https://CESMII.ProfileDesigner.Api.Test.org/{i}" });
            }
            return result;
        }

        /// <summary>
        /// Delete a subset of the items we created during add tests
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<object[]> ProfileControllerTestDeleteData()
        {
            var result = new List<object[]>();
            for (int i = 8; i <= 10; i++)
            {
                result.Add(new object[] { i, $"https://CESMII.ProfileDesigner.Api.Test.org/{i}" });
            }
            return result;
        }
        #endregion


    }
}
