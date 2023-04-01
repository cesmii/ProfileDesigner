using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

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
        private const string URL_DELETE_MANY = "/api/profile/deletemany";

        private const string URL_CLOUD_LIBRARY = "/api/profile/cloudlibrary";
        private const string URL_CLOUD_IMPORT = "/api/profile/cloudlibrary/import";
        #endregion

        #region data naming constants
        private const string NAMESPACE_PATTERN = "https://CESMII.ProfileDesigner.Api.Test.org/";
        private const string TITLE_PATTERN = "CESMII.ProfileDesigner.Api.Test.";
        private const string CATEGORY_PATTERN = "test";
        private const string VERSION_PATTERN = "1.0.0.";
        private const int CORE_NODESET_COUNT = 5;  // ua, ua/di, ua/robotics, fdi5, fdi7
        #endregion

        public ProfileControllerTest(CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup> factory, ITestOutputHelper output):
            base(factory, output)
        {
        }

#pragma warning disable xUnit1026  // Stop warnings related to parameters not used in test cases. 

        /// <summary>
        /// Delete most of the nodesets created from other integration tests except
        /// ua, ua/di, fdi v5, fdi v7, robotics
        /// </summary>
        /// <remarks>This is somewhat a prep activity. run this as first test to allow downstream tests in here to 
        /// have accurate counts.
        /// Leaving it as a test for now because it exercises delete many endpoint.</remarks>
        /// <param name="model"></param>
        /// <returns></returns>
        [Fact]
        public async Task A_DeleteMany()
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            //get stock filter
            var filter = base.ProfileFilter;
            filter.Take = 1000;  //make sure we get all items

            List<Shared.Models.IdIntModel> model = await GetItemsToDelete(apiClient, filter);

            // ACT
            //delete the items
            var result = await apiClient.ApiExecuteAsync<Shared.Models.ResultMessageModel>(URL_DELETE_MANY, model);

            //ASSERT
            Assert.True(result.IsSuccess);
            Assert.Contains("deleted", result.Message.ToLower());

            //Try to get the remaining items and should equal 5
            var itemsRemaining = (await apiClient.ApiGetManyAsync<DAL.Models.ProfileModel>(URL_LIBRARY, filter)).Data;
            Assert.Equal(5, itemsRemaining.Count);
        }

        /// <summary>
        /// Add an item and then get the item to confirm its existence and key values are present
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Theory]
        [MemberData(nameof(ProfileControllerTestData))]
        public async Task AddItem_GetItem(DAL.Models.ProfileModel model)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;

            // ACT
            //add an item
            var resultAdd = await apiClient.ApiExecuteAsync<Shared.Models.ResultMessageWithDataModel>(URL_ADD, model);
            var modelGet = new Shared.Models.IdIntModel() { ID = (int)resultAdd.Data };
            var resultGet = await apiClient.ApiGetItemAsync<DAL.Models.ProfileModel>(URL_GETBYID, modelGet);

            //ASSERT - Add
            Assert.True(resultAdd.IsSuccess);
            Assert.True(modelGet.ID > 0);

            //ASSERT - Get
            Assert.Equal(model.Title, resultGet.Title);
            Assert.Equal(model.Namespace, resultGet.Namespace);
            Assert.Equal(model.Version, resultGet.Version);
            Assert.Equal(model.CategoryName, resultGet.CategoryName);
            Assert.Equal(model.Description, resultGet.Description);
            Assert.Equal(model.License, resultGet.License);
            //Assert.Equal((i % 3 == 0 ? "Other" : (i % 2 == 0) ? "Custom" : "MIT"), resultGet.License);
            //Assert.Equal((i % 3 == 0 ? "Unique description for 3" : (i % 2 == 0) ? "Unique description for 2" : "Common description"), resultGet.Description);
            Assert.Equal(model.PublishDate.Value, resultGet.PublishDate.Value);
        }

        [Theory]
        [MemberData(nameof(ProfileControllerTestData))]
        public async Task DeleteItem(DAL.Models.ProfileModel model)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            //add an item so that we can delete it
            var resultAdd = await apiClient.ApiExecuteAsync<Shared.Models.ResultMessageWithDataModel>(URL_ADD, model);
            var modelDelete = new Shared.Models.IdIntModel() { ID = (int)resultAdd.Data };

            // ACT
            //delete the item
            var result = await apiClient.ApiExecuteAsync<Shared.Models.ResultMessageModel>(URL_DELETE, modelDelete);

            //ASSERT
            Assert.True(result.IsSuccess);
            Assert.Contains("item was deleted", result.Message.ToLower());
            //Try to get the item and should throw bad request
            await Assert.ThrowsAsync<MyNamespace.ApiException>(
                async () => await apiClient.ApiGetItemAsync<DAL.Models.ProfileModel>(URL_GETBYID, modelDelete));
        }

        [Theory]
        [InlineData(null, 13, 8)]
        [InlineData("/ua/", 3, 4)]
        [InlineData("/di/", 1, 4)]
        [InlineData("/fdi", 2, 4)]
        [InlineData(NAMESPACE_PATTERN, 7, 7)]
        [InlineData(TITLE_PATTERN, 14, 14)]
        public async Task GetLibrary(string query, int expectedCount, int rowsToAdd)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            //get stock filter
            var filter = base.ProfileFilter;
            //apply specifics to filter
            filter.Query = query;

            //import nodesets from cloudlib
            await ImportCloudLibItemsForSearchTests();

            //add some test rows to search against
            await InsertProfilesForSearchTests(Guid.NewGuid(), rowsToAdd);

            // ACT
            //get the list of items
            var items = (await apiClient.ApiGetManyAsync<DAL.Models.ProfileModel>(URL_LIBRARY, filter)).Data;

            //ASSERT
            Assert.Equal(expectedCount, items.Count);
        }

        [Theory]
        [InlineData(null, 8, 8)]
        [InlineData("/ua/", 0, 4)]
        [InlineData("/di/", 0, 4)]
        [InlineData("/fdi", 0, 4)]
        [InlineData(NAMESPACE_PATTERN, 7, 7)]
        [InlineData(TITLE_PATTERN, 14, 14)]
        public async Task GetLibraryMine(string query, int expectedCount, int rowsToAdd)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            //get stock filter
            var filter = base.ProfileFilter;

            //get profiles that are mine only
            var f = filter.Filters.Find(x => x.Name.ToLower().Equals("source"))?.Items
                .Find(y => y.ID.Equals((int)ProfileSearchCriteriaSourceEnum.Mine));
            f.Selected = true;

            //apply specifics to filter
            filter.Query = query;

            //import nodesets from cloudlib
            await ImportCloudLibItemsForSearchTests();

            //add some test rows to search against
            await InsertProfilesForSearchTests(Guid.NewGuid(), rowsToAdd);

            // ACT
            //get the list of items
            var items = (await apiClient.ApiGetManyAsync<DAL.Models.ProfileModel>(URL_LIBRARY, filter)).Data;

            //ASSERT
            Assert.Equal(expectedCount, items.Count);
        }

        #region Helper Methods
        private async Task ImportCloudLibItemsForSearchTests()
        {
            //get api client
            var apiClient = base.ApiClient;

            //get cloud lib items of interest (ua, ua/di, ua/robotics, /fdi5, /fdi7 )
            //mock only accepts certain search query combinations
            var filter = base.CloudLibFilter;
            filter.Query = null;
            filter.Take = 100;
            filter.PageBackwards = false;
            filter.Cursor = null;
            var items = (await apiClient.ApiGetManyAsync<DAL.Models.CloudLibProfileModel>(URL_CLOUD_LIBRARY, base.CloudLibFilter)).Data;

            //get list of cloud lib ids to import
            //TODO: namespace of mocks is not actual namespaces...
            var model = items
                .Where(x =>
                        (x.Namespace.ToLower().Equals("http://opcfoundation.org/ua/") & (!string.IsNullOrEmpty(x.Version) && x.Version.Equals("1.05.02"))) ||
                        (x.Namespace.ToLower().Equals("http://opcfoundation.org/ua/di/") & (!string.IsNullOrEmpty(x.Version) && x.Version.Equals("1.04.0"))) ||
                        (x.Namespace.ToLower().Equals("http://opcfoundation.org/ua/robotics/") & (!string.IsNullOrEmpty(x.Version) && x.Version.Equals("1.01.2"))) ||
                        (x.Namespace.ToLower().Equals("http://fdi-cooperation.com/opcua/fdi5/")) ||
                        (x.Namespace.ToLower().Equals("http://fdi-cooperation.com/opcua/fdi7/"))
                      )

                .Select(y => new Shared.Models.IdStringModel() { ID = y.CloudLibraryId }).ToList();
            //run the import
            var result = await apiClient.ApiExecuteAsync<Shared.Models.ResultMessageWithDataModel>(URL_CLOUD_IMPORT, model);

            //wait for import to complete before proceeding
            await base.PollImportStatus((int)result.Data);
        }

        private async Task InsertProfilesForSearchTests(Guid uuidCommon, int upperBound = 10)
        {
            //get api client
            var apiClient = base.ApiClient;

            //get items, loop over and add
            for (int i = 1; i <= upperBound; i++)
            {
                var uuid = System.Guid.NewGuid().ToString();
                var model = CreateNewItemModel(i, $"{uuidCommon.ToString()}-{uuid}");
                await apiClient.ApiExecuteAsync<Shared.Models.ResultMessageWithDataModel>(URL_ADD, model);
            }
        }

        /*
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
            var filter = base.StockFilter;
                (_filterPayload);
            //apply specifics to filter
            filter.Query = ns;

            // ACT
            //get the list of items
            var items = (await apiClient.ApiGetManyAsync<DAL.Models.ProfileModel>(URL_LIBRARY, filter)).Data;

            //get first item in list
            if (items.Count == 0) throw new System.InvalidOperationException($"Item not found: {ns}");
            return items[0].ID.Value;
        }
        */

        /// <summary>
        /// Delete most of the nodesets created from other integration tests except
        /// ua, ua/di, fdi v5, fdi v7, robotics
        /// </summary>
        private static async Task<List<Shared.Models.IdIntModel>> GetItemsToDelete(MyNamespace.Client apiClient, Shared.Models.ProfileTypeDefFilterModel filter)
        {
            filter.Take = 10000;
            //get the list of items, filter out some to preserve
            var items = (await apiClient.ApiGetManyAsync<DAL.Models.ProfileModel>(URL_LIBRARY, filter)).Data
                .Where(x =>
                        (!x.Namespace.ToLower().Equals("http://opcfoundation.org/ua/") & (string.IsNullOrEmpty(x.Version) || !x.Version.Equals("1.05.02"))) &&
                        (!x.Namespace.ToLower().Equals("http://opcfoundation.org/ua/di/") & (string.IsNullOrEmpty(x.Version) || !x.Version.Equals("1.04.0"))) &&
                        (!x.Namespace.ToLower().Equals("http://opcfoundation.org/ua/robotics/") & (string.IsNullOrEmpty(x.Version) || !x.Version.Equals("1.01.2"))) &&
                        (!x.Namespace.ToLower().Equals("http://fdi-cooperation.com/opcua/fdi5/")) &&
                        (!x.Namespace.ToLower().Equals("http://fdi-cooperation.com/opcua/fdi7/")) 
                      )
                .ToList();

            return items.Select(y => new Shared.Models.IdIntModel() { ID = y.ID.Value }).ToList();
        }

        private static DAL.Models.ProfileModel CreateNewItemModel(int i, string uuid)
        {
            return new DAL.Models.ProfileModel()
            {
                Namespace = $"{NAMESPACE_PATTERN}{i}/{uuid}",
                Title = $"{TITLE_PATTERN}{i}",
                Version = $"{VERSION_PATTERN}{i}",
                CategoryName = $"{CATEGORY_PATTERN}",
                PublishDate = new System.DateTime(System.DateTime.Now.Year, 1, i),
                License = (i % 3 == 0 ? "Other" : (i % 2 == 0) ? "Custom" : "MIT"),
                Description = (i % 3 == 0 ? "Unique description for 3" : (i % 2 == 0) ? "Unique description for 2" : "Common description")
            };
        }
        #endregion

        #region Test Data
        public static IEnumerable<object[]> ProfileControllerTestData()
        {
            var result = new List<object[]>();
            for (int i = 1; i <= 10; i++)
            {
                var uuid = System.Guid.NewGuid().ToString();
                result.Add(new object[] { CreateNewItemModel(i, uuid) });
            }
            return result;
        }

        #endregion

        /// <summary>
        /// do any post test cleanup here.
        /// </summary>
        /// <remarks>this will run after each test. So, if AddItem has 10 iterations of data, this will run once for each iteration.</remarks>
        public override void Dispose()
        {
            //get stock filter
            var filter = base.ProfileFilter;
            //do clean up here - get list of items to delete and then perform delete
            Task<List<Shared.Models.IdIntModel>> model = GetItemsToDelete(base.ApiClient, filter);
            model.Wait();
            //delete the items
            base.ApiClient.ApiExecuteAsync<Shared.Models.ResultMessageModel>(URL_DELETE_MANY, model.Result).Wait();
        }

    }
}
