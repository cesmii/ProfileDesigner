﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using Xunit;
using Xunit.Abstractions;

using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Repositories;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.Data.Contexts;

namespace CESMII.ProfileDesigner.Api.Tests.Integration
{
    [TestCaseOrderer("CESMII.ProfileDesigner.Api.Tests.Integration.ProfileControllerTestCaseOrderer", "CESMII.ProfileDesigner.Api.Tests.Integration")]
    public class ProfileControllerIntegrationTest : ControllerTestBase
    {
        private readonly ServiceProvider _serviceProvider;
        //for some tests, tie together a common guid so we can delete the created items at end of test. 
        private Guid _guidCommon = Guid.NewGuid();

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
        private const string NAMESPACE_CLOUD_PATTERN = "https://CloudLibrary.Mock.org/";
        private const string TITLE_PATTERN = "CESMII.ProfileDesigner.Api.Tests.Integration";
        private const string CATEGORY_PATTERN = "category-test";
        private const string VERSION_PATTERN = "1.0.0.";
        private const int CORE_NODESET_COUNT = 5;  // ua, ua/di, ua/robotics, fdi5, fdi7
        #endregion

        public ProfileControllerIntegrationTest(
            CustomWebApplicationFactory<Api.Startup> factory, 
            ITestOutputHelper output):
            base(factory, output)
        {
            var services = new ServiceCollection();

            //wire up db context to be used by repo
            base.InitDBContext(services);
            
            // DI - directly inject repo so we can add some test data directly and then have API test against it.
            // when running search tests. 
            services.AddSingleton< IConfiguration>(factory.Configuration);
            services.AddScoped<IRepository<Profile>, BaseRepo<Profile, ProfileDesignerPgContext>>();
            //need to get user id of test user when we add profile
            services.AddScoped<IRepository<User>, BaseRepo<User, ProfileDesignerPgContext>>();
            
            _serviceProvider = services.BuildServiceProvider();
        }

#pragma warning disable xUnit1026  // Stop warnings related to parameters not used in test cases. 

        [Theory]
        [InlineData(NAMESPACE_CLOUD_PATTERN, 8, 8, 2)]
        [InlineData(NAMESPACE_PATTERN, 4, 6, 4)]
        [InlineData(CATEGORY_PATTERN, 0, 5, 5)]
        public async Task DeleteMany(string query, int expectedCount, int numItemsToAdd, int numCloudItemsToAdd)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;

            //add some test rows to search against
            await InsertMockEntitiesForSearchTests(numItemsToAdd, false);
            await InsertMockEntitiesForSearchTests(numCloudItemsToAdd, true);

            //get stock filter
            var filter = base.ProfileFilter;
            filter.Take = 1000;  //make sure we get all items
            filter.Query = query;

            //get a partial list of items to delete, convert to list of ids
            var matches = (await apiClient.ApiGetManyAsync<ProfileModel>(URL_LIBRARY, filter)).Data;
            var model = matches.Select(y => new Shared.Models.IdIntModel() { ID = y.ID.Value }).ToList();

            // ACT
            //delete the items
            var result = await apiClient.ApiExecuteAsync<Shared.Models.ResultMessageModel>(URL_DELETE_MANY, model);

            //ASSERT
            Assert.True(result.IsSuccess);
            Assert.Contains("deleted", result.Message.ToLower());

            //Try to get the remaining items and should equal expected count,
            //always add the extra where clause after the fact of _guidCommon in case another test is adding stuff in parallel. 
            filter.Query = _guidCommon.ToString();
            var itemsRemaining = (await apiClient.ApiGetManyAsync<ProfileModel>(URL_LIBRARY, filter)).Data
                .Where(x => x.Keywords != null && string.Join(",", x.Keywords).ToLower().Contains(_guidCommon.ToString())).ToList();
            Assert.Equal(expectedCount, itemsRemaining.Count);
        }

        /// <summary>
        /// Add an item and then get the item to confirm its existence and key values are present
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Theory]
        [MemberData(nameof(ProfileControllerTestData))]
        public async Task AddItem_GetItem(ProfileModel model)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            //update guid common so we can delete after test. memberData cannot inject module level _guidCommon value (its static). 
            _guidCommon = Guid.Parse(model.Keywords.FirstOrDefault());

            // ACT
            //add an item
            var resultAdd = await apiClient.ApiExecuteAsync<Shared.Models.ResultMessageWithDataModel>(URL_ADD, model);
            var modelGet = new Shared.Models.IdIntModel() { ID = (int)resultAdd.Data };
            var resultGet = await apiClient.ApiGetItemAsync<ProfileModel>(URL_GETBYID, modelGet);

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
        public async Task DeleteItem(ProfileModel model)
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
                async () => await apiClient.ApiGetItemAsync<ProfileModel>(URL_GETBYID, modelDelete));
        }

        [Theory]
        [InlineData(CATEGORY_PATTERN, 10, 8, 2)]
        [InlineData(NAMESPACE_CLOUD_PATTERN, 2, 4, 2)]
        [InlineData(NAMESPACE_PATTERN, 7, 7, 2)]
        [InlineData(TITLE_PATTERN, 16, 14, 2)]
        [InlineData("zzzz", 0, 10, 10)]
        [InlineData("yyyy", 0, 10, 10)]
        public async Task GetLibrary(string query, int expectedCount, int numItemsToAdd, int numCloudItemsToAdd)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            //get stock filter
            var filter = base.ProfileFilter;
            //apply specifics to filter
            filter.Query = query;

            //add some test rows to search against
            await InsertMockEntitiesForSearchTests(numItemsToAdd, false);
            await InsertMockEntitiesForSearchTests(numCloudItemsToAdd, true);

            // ACT
            //get the list of items
            //always add the extra where clause after the fact of _guidCommon in case another test is adding stuff in parallel. 
            var items = (await apiClient.ApiGetManyAsync<ProfileModel>(URL_LIBRARY, filter)).Data
                .Where(x => x.Keywords != null && string.Join(",", x.Keywords).ToLower().Contains(_guidCommon.ToString())).ToList();

            //ASSERT
            Assert.Equal(expectedCount, items.Count);
        }

        [Theory]
        [InlineData(CATEGORY_PATTERN, 8, 8, 4)]
        [InlineData(NAMESPACE_CLOUD_PATTERN, 0, 4, 5)]
        [InlineData(NAMESPACE_PATTERN, 7, 7, 2)]
        [InlineData(TITLE_PATTERN, 14, 14, 6)]
        [InlineData("zzzz", 0, 10, 10)]
        [InlineData("yyyy", 0, 10, 10)]
        public async Task GetLibraryMine(string query, int expectedCount, int numItemsToAdd, int numCloudItemsToAdd)
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

            //add some test rows to search against
            var guidCommon = Guid.NewGuid();
            await InsertMockEntitiesForSearchTests(numItemsToAdd, false);
            await InsertMockEntitiesForSearchTests(numCloudItemsToAdd, true);

            // ACT
            //get the list of items
            //always add the extra where clause after the fact of _guidCommon in case another test is adding stuff in parallel. 
            var items = (await apiClient.ApiGetManyAsync<ProfileModel>(URL_LIBRARY, filter)).Data
                .Where(x => x.Keywords != null && string.Join(",", x.Keywords).ToLower().Contains(_guidCommon.ToString())).ToList();

            //ASSERT
            Assert.Equal(expectedCount, items.Count);
        }

        #region Helper Methods
        //private async Task ImportCloudLibItemsForSearchTests()
        //{
        //    //get api client
        //    var apiClient = base.ApiClient;

        //    //get cloud lib items of interest (ua, ua/di, ua/robotics, /fdi5, /fdi7 )
        //    //mock only accepts certain search query combinations
        //    var filter = base.CloudLibFilter;
        //    filter.Query = null;
        //    filter.Take = 100;
        //    filter.PageBackwards = false;
        //    filter.Cursor = null;
        //    var items = (await apiClient.ApiGetManyAsync<CloudLibProfileModel>(URL_CLOUD_LIBRARY, base.CloudLibFilter)).Data;

        //    //get list of cloud lib ids to import
        //    //TODO: namespace of mocks is not actual namespaces...
        //    var model = items
        //        .Where(x =>
        //                (x.Namespace.ToLower().Equals("http://opcfoundation.org/ua/") & (!string.IsNullOrEmpty(x.Version) && x.Version.Equals("1.05.02"))) ||
        //                (x.Namespace.ToLower().Equals("http://opcfoundation.org/ua/di/") & (!string.IsNullOrEmpty(x.Version) && x.Version.Equals("1.04.0"))) ||
        //                (x.Namespace.ToLower().Equals("http://opcfoundation.org/ua/robotics/") & (!string.IsNullOrEmpty(x.Version) && x.Version.Equals("1.01.2"))) ||
        //                (x.Namespace.ToLower().Equals("http://fdi-cooperation.com/opcua/fdi5/")) ||
        //                (x.Namespace.ToLower().Equals("http://fdi-cooperation.com/opcua/fdi7/"))
        //              )

        //        .Select(y => new Shared.Models.IdStringModel() { ID = y.CloudLibraryId }).ToList();
        //    //run the import
        //    var result = await apiClient.ApiExecuteAsync<Shared.Models.ResultMessageWithDataModel>(URL_CLOUD_IMPORT, model);

        //    //wait for import to complete before proceeding
        //    await base.PollImportStatus((int)result.Data);
        //}

        /*
        private async Task InsertMockProfilesForSearchTests(int upperBound = 10)
        {
            //get api client
            var apiClient = base.ApiClient;

            //get items, loop over and add
            for (int i = 1; i <= upperBound; i++)
            {
                var uuid = Guid.NewGuid();
                var model = CreateNewItemModel(i, _guidCommon, uuid);
                await apiClient.ApiExecuteAsync<Shared.Models.ResultMessageWithDataModel>(URL_ADD, model);
            }
        }
        */

        private async Task InsertMockEntitiesForSearchTests(int upperBound, bool isCloudEntity)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repo = scope.ServiceProvider.GetService<IRepository<Profile>>();
                var repoUser = scope.ServiceProvider.GetService<IRepository<User>>();
                var user = GetTestUser(repoUser);

                //get items, loop over and add
                for (int i = 1; i <= upperBound; i++)
                {
                    var uuid = Guid.NewGuid();
                    var entity = CreateNewEntity(i, _guidCommon, uuid, user, isCloudEntity ? i.ToString() : null);
                    await repo.AddAsync(entity);
                }
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
            var filter = base.ProfileFilter;
            //apply specifics to filter
            filter.Query = ns;

            // ACT
            //get the list of items
            var items = (await apiClient.ApiGetManyAsync<ProfileModel>(URL_LIBRARY, filter)).Data;

            //get first item in list
            if (items.Count == 0) throw new System.InvalidOperationException($"Item not found: {ns}");
            return items[0].ID.Value;
        }
        */

        /*
        /// <summary>
        /// Delete most of the nodesets created from other integration tests except
        /// ua, ua/di, fdi v5, fdi v7, robotics
        /// </summary>
        private static async Task<List<Shared.Models.IdIntModel>> GetItemsToDelete(MyNamespace.Client apiClient, Shared.Models.ProfileTypeDefFilterModel filter)
        {
            filter.Take = 10000;
            //get the list of items, filter out some to preserve
            var items = (await apiClient.ApiGetManyAsync<ProfileModel>(URL_LIBRARY, filter)).Data
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
        */

        private static ProfileModel CreateNewItemModel(int i, Guid guidCommon, Guid uuid, string cloudLibraryId = null)
        {
            var entity = CreateNewEntity(i, guidCommon, uuid, null, cloudLibraryId);
            return new ProfileModel()
            {
                Namespace = entity.Namespace,
                Title = entity.Title,
                Version = entity.Version,
                CategoryName = entity.CategoryName,
                PublishDate = entity.PublishDate,
                License = entity.License,
                Description = entity.Description,
                CloudLibraryId = entity.CloudLibraryId, 
                Keywords = entity.Keywords?.ToList()
            };
        }

        /// <summary>
        /// This is used to create a row directly into DB. Bypasses everything except baseRepo
        /// </summary>
        /// <param name="i"></param>
        /// <param name="uuid"></param>
        /// <param name="user"></param>
        /// <param name="cloudLibraryId"></param>
        /// <returns></returns>
        private static Profile CreateNewEntity(int i, Guid guidCommon, Guid uuid, User user, string cloudLibraryId = null)
        {
            var namespacePattern = string.IsNullOrEmpty(cloudLibraryId) ? NAMESPACE_PATTERN : NAMESPACE_CLOUD_PATTERN;
            var dt = DateTime.SpecifyKind(new DateTime(DateTime.Now.Year, 1, i), DateTimeKind.Utc);
            return new Profile()
            {
                Namespace = $"{namespacePattern}{i}/{uuid}",
                Title = $"{TITLE_PATTERN}{i}",
                Version = $"{VERSION_PATTERN}{i}",
                CategoryName = $"{CATEGORY_PATTERN}",
                PublishDate = dt,
                License = (i % 3 == 0 ? "Other" : (i % 2 == 0) ? "Custom" : "MIT"),
                Description = (i % 3 == 0 ? "Unique description for 3" : (i % 2 == 0) ? "Unique description for 2" : "Common description"),
                CloudLibraryId = cloudLibraryId, 
                AuthorId = user?.ID,
                //set some owners to null
                OwnerId = (user != null && i % 2 == 0) ? user.ID : null,
                Keywords = new string[] { guidCommon.ToString() }
            };
        }

        /// <summary>
        /// Delete profiles created during each test
        /// User <_guidCommon> as way to find items to delete 
        /// </summary>
        /// <returns></returns>
        private async Task CleanupProfiles()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repo = scope.ServiceProvider.GetService<IRepository<Profile>>();
                var items = repo.FindByCondition(x => 
                    x.Keywords != null && string.Join(",", x.Keywords).ToLower().Contains(_guidCommon.ToString())).ToList();
                foreach (var item in items)
                { 
                    await repo.DeleteAsync(item);
                }
                await repo.SaveChangesAsync();
            }
        }
        #endregion

        #region Test Data
        public static IEnumerable<object[]> ProfileControllerTestData()
        {
            var result = new List<object[]>();
            for (int i = 1; i <= 10; i++)
            {
                var uuid = Guid.NewGuid();
                result.Add(new object[] { CreateNewItemModel(i, uuid, uuid) });
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
            ////get stock filter
            //var filter = base.ProfileFilter;
            ////do clean up here - get list of items to delete and then perform delete
            //Task<List<Shared.Models.IdIntModel>> model = GetItemsToDelete(base.ApiClient, filter);
            //model.Wait();
            ////delete the items
            //base.ApiClient.ApiExecuteAsync<Shared.Models.ResultMessageModel>(URL_DELETE_MANY, model.Result).Wait();
            CleanupProfiles().Wait();
        }

    }
}