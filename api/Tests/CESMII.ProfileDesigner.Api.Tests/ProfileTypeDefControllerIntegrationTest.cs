using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

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

        //note - set user id in authors to 1 which is the test user created in base test code
        private const string _filterPayload = @"{'filters':[{'items':" +
            "[{'selected':false,'visible':true,'name':'My Types','code':null,'lookupType':0,'typeId':null,'displayOrder':0,'isActive':false,'id':1}],'name':'Author','id':1}," +
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
            services.AddSingleton<IConfiguration>(factory.Config);
            services.AddScoped<IRepository<Profile>, BaseRepo<Profile, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<ProfileTypeDefinition>, BaseRepo<ProfileTypeDefinition, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<ProfileTypeDefinitionAnalytic>, BaseRepo<ProfileTypeDefinitionAnalytic, ProfileDesignerPgContext>>();
            //need to get user id of test user when we add profile
            services.AddScoped<IRepository<User>, BaseRepo<User, ProfileDesignerPgContext>>();
            
            _serviceProvider = services.BuildServiceProvider();
        }

        protected ProfileTypeDefFilterModel TypeDefFilter
        {
            get
            {
                //get stock filter
                return JsonConvert.DeserializeObject<ProfileTypeDefFilterModel>(_filterPayload);
            }
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
            Assert.Equal(itemExtend.Profile.XmlSchemaUri, resultGet.Profile.XmlSchemaUri);
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

        /*
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
            var filter = this.TypeDefFilter;
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
        */

        [Theory]
        [InlineData(TITLE_PATTERN, true, false, null, 16)]
        [InlineData(TITLE_PATTERN, false, false, null, 16)]
        [InlineData(NAME_PATTERN, true, false, null, 8)]
        [InlineData(NAME_PATTERN, false, false, null, 8)]
        [InlineData("xxxx-xxxxx", true, false, null, 30)]
        [InlineData("xxxx-xxxxx", false, false, null, 2)]
        [InlineData("yyyy-yyyyy", true, false, null, 10)]
        [InlineData("yyyy-yyyyy", false, false, null, 10)]
        [InlineData("zzzz-zzzzz", true, false, null, 10)]
        [InlineData("zzzz-zzzzz", false, false, null, 10)]
        [InlineData(null, true, false, null, 10)]
        [InlineData(null, false, false, null, 10)]
        [InlineData(null, true, false, ProfileItemTypeEnum.Class , 24)] 
        [InlineData(null, false, false, ProfileItemTypeEnum.Class, 12)]
        [InlineData(null, true, false, ProfileItemTypeEnum.Structure, 21)] 
        [InlineData(null, false, false, ProfileItemTypeEnum.Structure, 21)]
        [InlineData(null, true, false, ProfileItemTypeEnum.Interface, 29)]
        [InlineData(null, false, false, ProfileItemTypeEnum.Interface, 29)]
        [InlineData(null, true, false, ProfileItemTypeEnum.Enumeration, 19)]
        [InlineData(null, false, false, ProfileItemTypeEnum.Enumeration, 19)]
        public async Task GetLibrarySearch(string query, bool isMine, bool isPopular, ProfileItemTypeEnum? typeDefType, int numItemsToAdd)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            //get stock filter
            var filter = this.TypeDefFilter;
            //apply specifics to filter
            filter.Query = query;
            filter.Take = numItemsToAdd + 9999;  //set very high so that we don't have invalid counts because we page out some results.

            //get profiles that are mine only
            if (isMine)
            {
                var f = filter.Filters.Find(x => x.ID.Equals((int)SearchCriteriaCategoryEnum.Author))?.Items
                    .Find(y => y.ID.Equals((int)ProfileSearchCriteriaSourceEnum.Mine));
                f.Selected = true;
            }

            //get profiles that are poular only
            //TODO: not yet tested...may have to add analytics tallies when adding test data first to achieve testable results.
            if (isPopular)
            {
                var f = filter.Filters.Find(x => x.ID.Equals((int)SearchCriteriaCategoryEnum.Popular))?.Items
                    .Find(y => y.ID.Equals(-1));
                f.Selected = true;
            }

            //optional filter - type
            if (typeDefType != null)
            {
                var f = filter.Filters.Find(x => x.ID.Equals((int)SearchCriteriaCategoryEnum.TypeDefinitionType))?.Items
                    .Find(y => y.ID.Equals((int)typeDefType));
                f.Selected = true;
            }

            //add some test rows to search against
            //apply query to desc for 75% of the items,
            //set types for 20% interface, 25% enum, 33% structure, remainder class
            var itemsAdded = await InsertMockEntitiesForSearchTests(numItemsToAdd, query);
            var expectedCount = CalculateExpectedCountSearch(itemsAdded, query, isMine, isPopular, typeDefType);

            // ACT
            //get the list of items
            var result = await apiClient.ApiGetManyAsync<ProfileTypeDefinitionModel>(URL_LIBRARY, filter);
            //always add the extra where clause after the fact of _guidCommon in case another test is adding stuff in parallel. 
            var items = result.Data
                .Where(x => x.SymbolicName != null && x.SymbolicName.ToLower().Contains(_guidCommon.ToString())).ToList();
            //always remove the parent type defs from result items - we denote those by putting external author = guidCommon. 
            items = items
                .Where(x => x.ExternalAuthor == null || !x.ExternalAuthor.ToLower().Contains(_guidCommon.ToString())).ToList();

            //output.WriteLine($"expectedCount: {expectedCount}, expectedCount calc (ceiling): {(int)Math.Ceiling(expectedCount1)}");
            //output.WriteLine($"expectedCount: {expectedCount}, expectedCount calc (round): {(int)Math.Round(expectedCount1)}");
            //lets see the correct outcome 
            if (expectedCount == items.Count)
            {
                output.WriteLine($"Expected: {expectedCount}, Actual: {items.Count}");
            }
            //ASSERT
            Assert.Equal(expectedCount, items.Count);
        }


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

        /// <summary>
        /// Inserts parent profiles, parent type defs and then inserts types defs. 
        /// </summary>
        /// <remarks>Note there is lots of logic to disperse the data. 67% of items assigned to owner, 
        /// 75% of items given query value in description, 
        /// Type def type is assigned 20% interface, 25% enum, 33% structure, remainder class</remarks>
        /// <param name="upperBound"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        private async Task<List<ProfileTypeDefinition>> InsertMockEntitiesForSearchTests(int upperBound, string query)
        {
            var result = new List<ProfileTypeDefinition>();
            using (var scope = _serviceProvider.CreateScope())
            {
                var repoProfile = scope.ServiceProvider.GetService<IRepository<Profile>>();
                var repo = scope.ServiceProvider.GetService<IRepository<ProfileTypeDefinition>>();
                var repoUser = scope.ServiceProvider.GetService<IRepository<User>>();
                var user = GetTestUser(repoUser);

                //create a parent profile - one that is mine, one that is generic
                var profileMine = CreateProfileEntity(_guidCommon, user);
                await repoProfile.AddAsync(profileMine);
                var profileCore = CreateProfileEntity(_guidCommon, null);
                profileCore.AuthorId = null;
                profileCore.OwnerId = null;
                await repoProfile.AddAsync(profileCore);

                //create a parent type definition - make parents null so it doesn't impact the 
                //search calls
                var parentClass = CreateEntity(0, profileMine.ID, null, _guidCommon, Guid.NewGuid(), user);
                parentClass.AuthorId = null;
                parentClass.OwnerId = null;
                parentClass.ExternalAuthor = _guidCommon.ToString();
                await repo.AddAsync(parentClass);
                var parentInterface = CreateEntity(0, profileMine.ID, null, _guidCommon, Guid.NewGuid(), user);
                parentInterface.ProfileTypeId = (int)ProfileItemTypeEnum.Interface;
                parentInterface.AuthorId = null;
                parentInterface.OwnerId = null;
                parentInterface.ExternalAuthor = _guidCommon.ToString();
                await repo.AddAsync(parentInterface);
                var parentEnum = CreateEntity(0, profileMine.ID, null, _guidCommon, Guid.NewGuid(), user);
                parentEnum.ProfileTypeId = (int)ProfileItemTypeEnum.Enumeration;
                parentEnum.AuthorId = null;
                parentEnum.OwnerId = null;
                parentEnum.ExternalAuthor = _guidCommon.ToString();
                await repo.AddAsync(parentEnum);
                var parentStructure = CreateEntity(0, profileMine.ID, null, _guidCommon, Guid.NewGuid(), user);
                parentStructure.ProfileTypeId = (int)ProfileItemTypeEnum.Structure;
                parentStructure.AuthorId = null;
                parentStructure.OwnerId = null;
                parentStructure.ExternalAuthor = _guidCommon.ToString();
                await repo.AddAsync(parentStructure);

                //assign profile to type def in case caller needs it.
                parentClass.Profile = profileCore;
                parentInterface.Profile = profileCore;
                parentEnum.Profile = profileCore;
                parentStructure.Profile = profileCore;

                //add to collection to return
                result.Add(parentClass);
                result.Add(parentInterface);
                result.Add(parentEnum);
                result.Add(parentStructure);

                //get items, loop over and add
                for (int i = 1; i <= upperBound; i++)
                {
                    var uuid = Guid.NewGuid();
                    //distribute the parent type def assignment
                    var p = i % 5 == 0 ? parentInterface : i % 4 == 0 ? parentClass : i % 3 == 0 ? parentStructure : parentEnum;
                    //set owner to 2/3 of the items
                    var entity = CreateEntity(i, i % 3 == 0 ? profileCore.ID : profileMine.ID, p, _guidCommon, uuid, user);
                    int? authorId = i % 3 == 0 ? null : user.ID;
                    entity.AuthorId = authorId;
                    entity.OwnerId = authorId;
                    //customize some entries
                    //add query to description for 75%
                    var desc = i % 4 == 0 ? "" : " " + query;
                    entity.Description += desc;
                    await repo.AddAsync(entity);
                    result.Add(entity);
                }
            }
            return result;
        }

        /// <summary>
        /// Using the items added in the insert mock items, calculate the expected count to compare against actual search count
        /// </summary>
        /// <param name="itemsAdded"></param>
        /// <param name="query"></param>
        /// <param name="isMine"></param>
        /// <param name="isPopular"></param>
        /// <param name="typeDefType"></param>
        /// <returns></returns>
        private int CalculateExpectedCountSearch(List<ProfileTypeDefinition> itemsAdded, string query, bool isMine, bool isPopular, ProfileItemTypeEnum? typeDefType)
        {
            //calculate this value based on the criteria and our knowledge of how we prep the test data
            return itemsAdded
                //always trim out parent type defs
                .Where(x => x.ExternalAuthor == null || !x.ExternalAuthor.ToLower().Contains(_guidCommon.ToString()))
                //trim out mine - if needed 
                .Where(x => !isMine || (isMine && x.AuthorId.HasValue))
                //TODO: popular filter
                //
                //type def filter
                .Where(x => typeDefType == null || x.ProfileTypeId.Equals((int)typeDefType))
                //query
                .Where(x => string.IsNullOrEmpty(query) || x.Description.ToLower().Contains(query.ToLower()))
                .Count();
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
        /// <param name="creator"></param>
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
