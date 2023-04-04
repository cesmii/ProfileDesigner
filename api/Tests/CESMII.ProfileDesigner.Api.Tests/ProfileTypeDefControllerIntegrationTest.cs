using System;
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
using CESMII.ProfileDesigner.Api.Shared.Models;

namespace CESMII.ProfileDesigner.Api.Tests.Int
{
    public class ProfileTypeDefControllerIntegrationTest : ControllerTestBase
    {
        private readonly ServiceProvider _serviceProvider;
        //for some tests, tie together a common guid so we can delete the created items at end of test. 
        private Guid _guidCommon = Guid.NewGuid();

        private const string _filterPayload = @"{'filters':[{'items':" +
            "[{'selected':false,'visible':true,'name':'My Types','code':null,'lookupType':0,'typeId':null,'displayOrder':0,'isActive':false,'id':36}],'name':'Author','id':1}," +
            "{'items':[{'selected':false,'visible':true,'name':'Popular','code':null,'lookupType':0,'typeId':null,'displayOrder':0,'isActive':false,'id':-1}],'name':'Popular','id':2}," +
            "{'items':[{'selected':false,'visible':true,'name':'Class','code':null,'lookupType':0,'typeId':null,'displayOrder':9999,'isActive':false,'id':2}," +
            "{'selected':false,'visible':true,'name':'Data Type','code':null,'lookupType':0,'typeId':null,'displayOrder':9999,'isActive':false,'id':3}, " +
            "{'selected':false,'visible':true,'name':'Enumeration','code':null,'lookupType':0,'typeId':null,'displayOrder':9999,'isActive':false,'id':19}," +
            "{'selected':false,'visible':true,'name':'Interface','code':null,'lookupType':0,'typeId':null,'displayOrder':9999,'isActive':false,'id':1}," +
            "{'selected':false,'visible':true,'name':'Structure','code':null,'lookupType':0,'typeId':null,'displayOrder':9999,'isActive':false,'id':18}, " +
            "{'selected':false,'visible':true,'name':'Variable Type','code':null,'lookupType':0,'typeId':null,'displayOrder':9999,'isActive':false,'id':12}],'name':'TypeDefinitionType','id':3}," +
            /*
            "{'items':[{'selected':false,'visible':false,'name':'http://cesmii.org/CNC','code':null,'lookupType':0,'typeId':null,'displayOrder':999,'isActive':true,'id':190}," +
            "{'selected':false,'visible':false,'name':'http://fdi-cooperation.com/OPCUA/FDI5/ (1.1)','code':null,'lookupType':0,'typeId':null,'displayOrder':999,'isActive':true,'id':147}," + 
            "{'selected':false,'visible':false,'name':'http://opcfoundation.org/UA/ (1.05.02)','code':null,'lookupType':0,'typeId':null,'displayOrder':999,'isActive':true,'id':56}," + 
            "{'selected':false,'visible':false,'name':'http://opcfoundation.org/UA/AMB/ (1.01.0)','code':null,'lookupType':0,'typeId':null,'displayOrder':999,'isActive':true,'id':152}," +
            "{'selected':false,'visible':false,'name':'http://opcfoundation.org/UA/AML/ (1.00)','code':null,'lookupType':0,'typeId':null,'displayOrder':999,'isActive':true,'id':153}," +
            "{'selected':false,'visible':false,'name':'http://opcfoundation.org/UA/CNC (1.0.0)','code':null,'lookupType':0,'typeId':null,'displayOrder':999,'isActive':true,'id':160}," +
            "{'selected':false,'visible':false,'name':'http://opcfoundation.org/UA/DI/ (1.04.0)','code':null,'lookupType':0,'typeId':null,'displayOrder':999,'isActive':true,'id':57}," + 
            "{'selected':false,'visible':false,'name':'http://opcfoundation.org/UA/Robotics/ (1.01.2)','code':null,'lookupType':0,'typeId':null,'displayOrder':999,'isActive':true,'id':148}],'name':'Profile','id':4}" + 
            */
            "]," +
            "'sortByEnum':3,'query':null,'take':25,'skip':0}";

        #region API constants
        private const string URL_INIT = "/api/profiletypedefinition/init";
        private const string URL_EXTEND = "/api/profiletypedefinition/extend";
        private const string URL_ADD = "/api/profiletypedefinition/add";
        private const string URL_LIBRARY = "/api/profiletypedefinition/library";
        private const string URL_GETBYID = "/api/profiletypedefinition/getbyid";
        private const string URL_DELETE = "/api/profiletypedefinition/delete";
        private const string URL_DELETE_MANY = "/api/profiletypedefinition/deletemany";
        #endregion

        #region data naming constants
        private const string NAME_PATTERN = "CESMII.TypeDef";
        private const string PARENT_PROFILE_NAMESPACE = "https://CESMII.Profile.Mock.org/";
        private const string TITLE_PATTERN = "CESMII.ProfileDesigner.Api.Tests.Integration";
        private const string CATEGORY_PATTERN = "category-test";
        private const string VERSION_PATTERN = "1.0.0.";
        private const int TYPE_ID_DEFAULT = (int)ProfileItemTypeEnum.Class;  
        #endregion

        public ProfileTypeDefControllerIntegrationTest(
            CustomWebApplicationFactory<Api.Startup> factory, 
            ITestOutputHelper output):
            base(factory, output)
        {
            var services = new ServiceCollection();

            //wire up db context to be used by repos
            base.InitDBContext(services);
            
            // DI - directly inject repo so we can add some test data directly and then have API test against it.
            // when running search tests. 
            services.AddSingleton< IConfiguration>(factory.Config);
            services.AddScoped<IRepository<Profile>, BaseRepo<Profile, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<ProfileTypeDefinition>, BaseRepo<ProfileTypeDefinition, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<ProfileTypeDefinitionAnalytic>, BaseRepo<ProfileTypeDefinitionAnalytic, ProfileDesignerPgContext>>();
            //need to get user id of test user when we add profile
            services.AddScoped<IRepository<User>, BaseRepo<User, ProfileDesignerPgContext>>();
            
            _serviceProvider = services.BuildServiceProvider();
        }

#pragma warning disable xUnit1026  // Stop warnings related to parameters not used in test cases. 

        /// <summary>
        /// Extend from an item, then Add and then get the item to confirm its existence and key values are present
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Theory]
        [MemberData(nameof(ControllerTestCounterData))]
        public async Task ExtendItem(int counter)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;

            //create parent profile and entity to extend
            var itemExtend = await InsertMockProfileAndExtendEntity(_guidCommon);

            // ACT
            //extend item
            var resultExtend = await apiClient.ApiGetItemAsync<ProfileTypeDefinitionModel>(URL_EXTEND,
                new IdIntModel() { ID = itemExtend.ID.Value });

            //ASSERT - extended API call assets
            Assert.NotNull(resultExtend);
            Assert.Equal("", resultExtend.Name);
            Assert.Equal("", resultExtend.Description);
            Assert.Null(resultExtend.OpcNodeId);
            Assert.Null(resultExtend.BrowseName);
            Assert.Null(resultExtend.SymbolicName);
            Assert.Null(resultExtend.DocumentUrl);
            Assert.Equal(resultExtend.Parent.Name, itemExtend.Name);
            Assert.Equal(resultExtend.Parent.OpcNodeId, itemExtend.OpcNodeId);
            Assert.Equal(resultExtend.Parent.BrowseName, itemExtend.BrowseName);
            Assert.Equal(resultExtend.Parent.SymbolicName, itemExtend.SymbolicName);
        }

        /// <summary>
        /// Extend from an item, then Add and then get the item to confirm its existence and key values are present
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Theory]
        [MemberData(nameof(ProfileTypeDefControllerTestData))]
        public async Task AddItem_GetItem(ProfileTypeDefinitionModel model)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;

            //treat inbound item as the new type def. Once we call extend, we need to apply some updates to it and save. 
            //create parent profile and entity to extend
            var itemExtend = await InsertMockProfileAndExtendEntity(_guidCommon);
            var resultExtend = await MapModelToExtendedItem(apiClient, _guidCommon, itemExtend, model);
            ////extend item
            //var resultExtend = await apiClient.ApiGetItemAsync<ProfileTypeDefinitionModel>(URL_EXTEND, 
            //    new IdIntModel() { ID = itemExtend.ID.Value });
            ////map data to newly created extend
            //resultExtend.OpcNodeId = model.OpcNodeId;
            //resultExtend.Name = model.Name;
            //resultExtend.BrowseName = model.BrowseName;
            //resultExtend.SymbolicName = _guidCommon.ToString();  //so we can delete this item once done
            //resultExtend.Description = model.Description;
            //resultExtend.Created = model.Created;
            //resultExtend.MetaTags = model.MetaTags;
            //resultExtend.Attributes = model.Attributes;
            //resultExtend.ProfileId = itemExtend.ProfileId;
            //resultExtend.Profile = new ProfileModel() { ID = itemExtend.ProfileId };

            // ACT
            var resultAdd = await apiClient.ApiExecuteAsync<ResultMessageWithDataModel>(URL_ADD, resultExtend);
            var modelGet = new IdIntModel() { ID = (int)resultAdd.Data };
            var resultGet = await apiClient.ApiGetItemAsync<ProfileTypeDefinitionModel>(URL_GETBYID, modelGet);

            //ASSERT - add
            Assert.True(resultAdd.IsSuccess);
            Assert.True(modelGet.ID > 0);

            //assert item was created properly
            Assert.Equal(resultExtend.OpcNodeId, resultGet.OpcNodeId);
            Assert.Equal(resultExtend.Name, resultGet.Name);
            Assert.Equal(resultExtend.BrowseName, resultGet.BrowseName);
            Assert.Equal(resultExtend.SymbolicName, resultGet.SymbolicName);
            Assert.Equal(resultExtend.Description, resultGet.Description);

            //assert parent entity item was assigned properly
            Assert.NotNull(resultGet.Parent);
            Assert.Equal(itemExtend.ID, resultGet.Parent.ID);
            Assert.Equal(itemExtend.Name, resultGet.Parent.Name);
            Assert.Equal(itemExtend.OpcNodeId, resultGet.Parent.OpcNodeId);
            Assert.Equal(itemExtend.BrowseName, resultGet.Parent.BrowseName);
            Assert.Equal(itemExtend.SymbolicName, resultGet.Parent.SymbolicName);

            //assert profile was assigned properly
            Assert.NotNull(resultGet.Profile);
            Assert.Equal(itemExtend.Profile.ID, resultGet.Profile.ID);
            Assert.Equal(itemExtend.Profile.Namespace, resultGet.Profile.Namespace);
            Assert.Equal(itemExtend.Profile.Version, resultGet.Profile.Version);
        }

        [Theory]
        [MemberData(nameof(ProfileTypeDefControllerTestData))]
        public async Task DeleteItem(ProfileTypeDefinitionModel model)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            //add an item so that we can delete it
            //have to properly extend a base item, etc. before adding
            //then get the id of newly added item so we can call delete
            var itemExtend = await InsertMockProfileAndExtendEntity(_guidCommon);
            var resultExtend = await MapModelToExtendedItem(apiClient, _guidCommon, itemExtend, model);
            var resultAdd = await apiClient.ApiExecuteAsync<ResultMessageWithDataModel>(URL_ADD, resultExtend);
            var modelDelete = new IdIntModel() { ID = (int)resultAdd.Data };

            // ACT
            //delete the item
            var result = await apiClient.ApiExecuteAsync<ResultMessageModel>(URL_DELETE, modelDelete);

            //ASSERT
            Assert.True(result.IsSuccess);
            Assert.Contains("item was deleted", result.Message.ToLower());
            //Try to get the item and should throw bad request
            await Assert.ThrowsAsync<MyNamespace.ApiException>(
                async () => await apiClient.ApiGetItemAsync<ProfileTypeDefinitionModel>(URL_GETBYID, modelDelete));
        }

        [Theory]
        [InlineData(CATEGORY_PATTERN, 5, 10)]
        [InlineData(NAME_PATTERN, 3, 7)]
        [InlineData(TITLE_PATTERN, 8, 16)]
        [InlineData("zzzz", 5, 10)]
        [InlineData("yyyy", 5, 10)]
        [InlineData("xxxx", 0, 1)]
        public async Task GetLibrary(string query, int expectedCount, int numItemsToAdd)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            //get stock filter
            var filter = base.ProfileFilter;
            //apply specifics to filter
            filter.Query = query;

            //add some test rows to search against
            await InsertMockEntitiesForSearchTests(numItemsToAdd, query); //apply query to desc for 1/2 the items

            // ACT
            //get the list of items
            //always add the extra where clause after the fact of _guidCommon in case another test is adding stuff in parallel. 
            var items = (await apiClient.ApiGetManyAsync<ProfileTypeDefinitionModel>(URL_LIBRARY, filter)).Data
                .Where(x => x.SymbolicName != null && x.SymbolicName.ToLower().Contains(_guidCommon.ToString())).ToList();

            //ASSERT
            Assert.Equal(expectedCount, items.Count);
        }

        /*
        [Theory]
        [InlineData(CATEGORY_PATTERN, 8, 8, 4)]
        //[InlineData(NAMESPACE_CLOUD_PATTERN, 0, 4, 5)]
        [InlineData(NAME_PATTERN, 7, 7, 2)]
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
            await InsertMockEntitiesForSearchTests(numItemsToAdd);

            // ACT
            //get the list of items
            //always add the extra where clause after the fact of _guidCommon in case another test is adding stuff in parallel. 
            var items = (await apiClient.ApiGetManyAsync<ProfileTypeDefinitionModel>(URL_LIBRARY, filter)).Data
                .Where(x => x.SymbolicName != null && x.SymbolicName.ToLower().Contains(_guidCommon.ToString())).ToList();

            //ASSERT
            Assert.Equal(expectedCount, items.Count);
        }
        */

        #region Helper Methods
        /// <summary>
        /// Create a mock parent item. Then set up a newly extended item with that parent item.
        /// Finally, apply the model values to the newly extended item and return.
        /// </summary>
        /// <returns></returns>
        private async Task<ProfileTypeDefinitionModel> MapModelToExtendedItem(MyNamespace.Client apiClient, Guid guidCommon,
            ProfileTypeDefinition itemExtend, ProfileTypeDefinitionModel model)
        {
            //extend item
            var result = await apiClient.ApiGetItemAsync<ProfileTypeDefinitionModel>(URL_EXTEND,
                new IdIntModel() { ID = itemExtend.ID.Value });
            //map data to newly created extend
            result.OpcNodeId = model.OpcNodeId;
            result.Name = model.Name;
            result.BrowseName = model.BrowseName;
            result.SymbolicName = guidCommon.ToString();  //so we can delete this item once done
            result.Description = model.Description;
            result.Created = model.Created;
            result.MetaTags = model.MetaTags;
            result.Attributes = model.Attributes;
            result.ProfileId = itemExtend.ProfileId;
            result.Profile = new ProfileModel() { ID = itemExtend.ProfileId };
            return result;
        }
        
        /// <summary>
        /// Create a parent profile and an entity to extend from. 
        /// </summary>
        /// <param name="guidCommon"></param>
        /// <returns></returns>
        private async Task<ProfileTypeDefinition> InsertMockProfileAndExtendEntity(Guid guidCommon)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repoProfile = scope.ServiceProvider.GetService<IRepository<Profile>>();
                var repo = scope.ServiceProvider.GetService<IRepository<ProfileTypeDefinition>>();
                var repoUser = scope.ServiceProvider.GetService<IRepository<User>>();
                var user = GetTestUser(repoUser);

                //create a parent profile
                var profile = CreateProfileEntity(guidCommon, user);
                await repoProfile.AddAsync(profile);

                //create a parent type definition
                var result = CreateEntity(0, profile.ID, null, guidCommon, Guid.NewGuid(), user);
                await repo.AddAsync(result);

                //assign profile to type def in case caller needs it.
                result.Profile = profile;
                
                return result;
            }
        }

        private async Task InsertMockEntitiesForSearchTests(int upperBound, string query)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repoProfile = scope.ServiceProvider.GetService<IRepository<Profile>>();
                var repo = scope.ServiceProvider.GetService<IRepository<ProfileTypeDefinition>>();
                var repoUser = scope.ServiceProvider.GetService<IRepository<User>>();
                var user = GetTestUser(repoUser);

                //create a parent profile
                var profile = CreateProfileEntity(_guidCommon, user);
                await repoProfile.AddAsync(profile);

                //create a parent type definition
                var parent = CreateEntity(0, profile.ID, null, _guidCommon, Guid.NewGuid(), user);
                await repo.AddAsync(parent);

                //assign profile to type def in case caller needs it.
                parent.Profile = profile;

                //get items, loop over and add
                for (int i = 1; i <= upperBound; i++)
                {
                    var uuid = Guid.NewGuid();
                    var entity = CreateEntity(i, profile.ID, parent, _guidCommon, uuid, user);
                    var desc = i % 2 == 0 ? " " + query : "";
                    entity.Description += desc;
                    await repo.AddAsync(entity);
                }
            }
        }

        private static ProfileTypeDefinitionModel CreateItemModel(int i, int? profileId, ProfileTypeDefinition parent, Guid guidCommon, Guid uuid, string cloudLibraryId = null)
        {
            var entity = CreateEntity(i, profileId, parent, guidCommon, uuid, null);
            return MapToModel(entity);
        }

        private static ProfileTypeDefinitionModel MapToModel(ProfileTypeDefinition entity)
        {
            var tags = string.IsNullOrEmpty(entity.MetaTags) ? new List<string>() :
                    Newtonsoft.Json.JsonConvert.DeserializeObject<List<MetaTag>>(entity.MetaTags).Select(s => s.Name.Trim()).ToList();

            return new ProfileTypeDefinitionModel()
            {
                ID = entity.ID,
                OpcNodeId = entity.OpcNodeId,
                Name = entity.Name,
                ProfileId = entity.ProfileId != 0 ? entity.ProfileId : null,
                //Profile = MapToModelProfile(entity.Profile),
                BrowseName = entity.BrowseName,
                SymbolicName = entity.SymbolicName,
                Description = entity.Description,
                TypeId = entity.ProfileTypeId,
                Type = entity.ProfileType != null ?
                        new LookupItemModel { ID = entity.ProfileType.ID, Name = entity.ProfileType.Name, TypeId = entity.ProfileType.ID }
                        : null,
                AuthorId = entity.AuthorId ?? null,
                //Author = MapToModelSimpleUser(entity.Author),
                ExternalAuthor = entity.ExternalAuthor,
                DocumentUrl = entity.DocumentUrl,
                IsAbstract = entity.IsAbstract,
                Created = entity.Created,
                Updated = entity.Updated,
                MetaTags = tags,
                MetaTagsConcatenated = string.IsNullOrEmpty(entity.MetaTags) ? "" :
                    string.Join(", ", tags),
                IsActive = entity.IsActive,
                IsFavorite = entity.Favorite != null,
                //calculated value which gives more emphasis on extending an item
                //PopularityIndex = MapToModelPopularityIndex(entity)
            };
        }

        private static ProfileTypeDefinitionSimpleModel MapToModelSimple(ProfileTypeDefinition entity)
        {
            return new ProfileTypeDefinitionSimpleModel()
            {
                ID = entity.ID,
                OpcNodeId = entity.OpcNodeId,
                Name = entity.Name,
                ProfileId = entity.ProfileId != 0 ? entity.ProfileId : null,
                //Profile = MapToModelProfile(entity.Profile),
                BrowseName = entity.BrowseName,
                SymbolicName = entity.SymbolicName,
                Description = entity.Description,
                Type = entity.ProfileType != null ?
                        new LookupItemModel { ID = entity.ProfileType.ID, Name = entity.ProfileType.Name, TypeId = entity.ProfileType.ID }
                        : null,
                Author = new UserSimpleModel() {ID = entity.AuthorId } ,
                IsAbstract = entity.IsAbstract,
                MetaTags = string.IsNullOrEmpty(entity.MetaTags) ? new List<string>() :
                    Newtonsoft.Json.JsonConvert.DeserializeObject<List<MetaTag>>(entity.MetaTags).Select(s => s.Name.Trim()).ToList(),
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
        private static ProfileTypeDefinition CreateEntity(int i, int? profileId, ProfileTypeDefinition parent, Guid guidCommon, Guid uuid, User user)
        {
            var parentName = parent == null ? "TypeDef" : $"{parent.Name}::Extend";
            var dt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            var tags = new List<MetaTag>() { 
                new MetaTag() { Name = (i % 4 == 0 ? "abcd" : (i % 3 == 0) ? "efgh" : (i % 2 == 0) ? "ijkl" : "mnop") }
            };
            return new ProfileTypeDefinition()
            {
                OpcNodeId = uuid.ToString(),
                Name = $"{parentName}-{i}",
                ProfileId = profileId,
                ParentId = parent?.ID,  //for some tests, we start with null parent and assign during test
                BrowseName = $"browse-{i}-{guidCommon}-{uuid}",
                SymbolicName = guidCommon.ToString(),
                Description = (i % 3 == 0 ? "Unique description for 3" : (i % 2 == 0) ? "Unique description for 2" : "Common description"),
                ProfileTypeId = parent == null ? TYPE_ID_DEFAULT : parent?.ProfileTypeId,
                IsAbstract = i % 9 == 0,
                Created = dt,
                Updated = dt,
                AuthorId = user?.ID,
                OwnerId = user?.ID,
                CreatedById = user == null ? 0 : user.ID.Value,
                UpdatedById = user == null ? 0 : user.ID.Value,
                MetaTags = Newtonsoft.Json.JsonConvert.SerializeObject(tags),
                IsActive = true,
            };
        }

        /// <summary>
        /// This is used to create a row directly into DB. Bypasses everything except baseRepo
        /// </summary>
        private static Profile CreateProfileEntity(Guid guidCommon, User user)
        {
            var dt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            return new Profile()
            {
                Namespace = $"{PARENT_PROFILE_NAMESPACE}/{guidCommon}",
                Title = TITLE_PATTERN,
                Version = "1.0.0.0",
                CategoryName = "TEST",
                PublishDate = dt,
                AuthorId = user?.ID,
                OwnerId = user != null ? user.ID : null,
                Keywords = new string[] { guidCommon.ToString() }
            };
        }


        /// <summary>
        /// Delete profiles created during each test
        /// User <_guidCommon> as way to find items to delete 
        /// </summary>
        /// <returns></returns>
        private async Task CleanupEntities()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                //type defs
                var repo = scope.ServiceProvider.GetService<IRepository<ProfileTypeDefinition>>();
                //order by to account for some fk delete issues
                var items = repo.FindByCondition(x =>
                    x.SymbolicName != null && x.SymbolicName.ToLower().Contains(_guidCommon.ToString()))
                    //.OrderBy(x => x.ParentId.HasValue)
                    .OrderByDescending(x => !x.ParentId.HasValue ? 0 : x.ParentId.Value)
                    .ToList();

                //type def analytics
                var repoAnalytic = scope.ServiceProvider.GetService<IRepository<ProfileTypeDefinitionAnalytic>>();
                //order to account for some fk delete issues
                var itemsAnalytic = repoAnalytic.FindByCondition(x =>
                    items.Select(y => y.ID.Value).Contains(x.ProfileTypeDefinitionId))
                    .ToList();
                foreach (var a in itemsAnalytic)
                {
                    await repoAnalytic.DeleteAsync(a);
                }
                await repoAnalytic.SaveChangesAsync();

                //type defs
                foreach (var item in items)
                {
                    await repo.DeleteAsync(item);
                }
                await repo.SaveChangesAsync();

                //parent profiles
                var repoProfile = scope.ServiceProvider.GetService<IRepository<Profile>>();
                var itemsProfile = repoProfile.FindByCondition(x =>
                    items.Select(y => y.ProfileId.Value).Contains(x.ID.Value))
                    .ToList();
                foreach (var item in itemsProfile)
                { 
                    await repoProfile.DeleteAsync(item);
                }
                await repoProfile.SaveChangesAsync();
            }
        }
        #endregion

        #region Test Data
        public static IEnumerable<object[]> ProfileTypeDefControllerTestData()
        {
            var result = new List<object[]>();
            for (int i = 1; i <= 10; i++)
            {
                var uuid = Guid.NewGuid();
                result.Add(new object[] { CreateItemModel(i, null, null, uuid, uuid) });
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
            CleanupEntities().Wait();
        }

    }
}
