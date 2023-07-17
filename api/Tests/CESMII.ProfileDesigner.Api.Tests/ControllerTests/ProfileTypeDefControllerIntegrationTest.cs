using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Repositories;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.Api.Shared.Models;

namespace CESMII.ProfileDesigner.Api.Tests.Int.Controllers
{
    [Trait("SmokeTest", "true")] //trait can be applied at test or test class level
    public class ProfileTypeDefControllerIntegrationTest : ProfileTypeDefControllerTestBase
    {
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
        private const string URL_LIBRARY = "/api/profiletypedefinition/library";
        private const string URL_DELETE = "/api/profiletypedefinition/delete";
        private const string URL_DELETE_MANY = "/api/profiletypedefinition/deletemany";
        #endregion

        #region data naming constants
        private const string NAME_PATTERN = "CESMII.TypeDef";
        private const string CATEGORY_PATTERN = "category-test";
        private const string VERSION_PATTERN = "1.0.0.";
        #endregion

        public ProfileTypeDefControllerIntegrationTest(
            CustomWebApplicationFactory<Api.Startup> factory, 
            ITestOutputHelper output):
            base(factory, output)
        {
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
            var itemExtend = await InsertMockProfileAndTypeDefinition(TYPE_ID_DEFAULT, _guidCommon);

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
            var itemExtend = await InsertMockProfileAndTypeDefinition(TYPE_ID_DEFAULT, _guidCommon);
            var resultExtend = await MapModelToExtendedItem(apiClient, _guidCommon, itemExtend, model);

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
            var itemExtend = await InsertMockProfileAndTypeDefinition(TYPE_ID_DEFAULT, _guidCommon);
            var resultExtend = await MapModelToExtendedItem(apiClient, _guidCommon, itemExtend, model);
            var resultAdd = await apiClient.ApiExecuteAsync<ResultMessageWithDataModel>(URL_ADD, resultExtend);
            var modelId = new IdIntModel() { ID = (int)resultAdd.Data };
            //call the get on the new item so we force an analytics tally - testing FK/cascade delete scenario
            await apiClient.ApiExecuteAsync<ResultMessageWithDataModel>(URL_GETBYID, modelId);

            // ACT
            //delete the item
            var result = await apiClient.ApiExecuteAsync<ResultMessageModel>(URL_DELETE, modelId);

            //ASSERT
            Assert.True(result.IsSuccess);
            Assert.Contains("item was deleted", result.Message.ToLower());
            //Try to get the item and should throw bad request
            await Assert.ThrowsAsync<MyNamespace.ApiException>(
                async () => await apiClient.ApiGetItemAsync<ProfileTypeDefinitionModel>(URL_GETBYID, modelId));
        }

        [Theory]
        [MemberData(nameof(ProfileTypeDefControllerTestData))]
        public async Task NoDeleteParent(ProfileTypeDefinitionModel model)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;
            //add an item to a parent item and then try to delete parent.
            //we should get a message indicating can't delete parent due to dependency. Must delete child then you can delete parent.
            var entityParent = await InsertMockProfileAndTypeDefinition(TYPE_ID_DEFAULT, _guidCommon);
            var modelChildNew = await MapModelToExtendedItem(apiClient, _guidCommon, entityParent, model);
            var resultChild = await apiClient.ApiExecuteAsync<ResultMessageWithDataModel>(URL_ADD, modelChildNew);
            var parentId = new IdIntModel() { ID = entityParent.ID.Value };
            //call the get on the new item so we force an analytics tally - testing FK/cascade delete scenario
            await apiClient.ApiExecuteAsync<ResultMessageWithDataModel>(URL_GETBYID, parentId);

            // ACT
            //try to delete the parent - expect error
            var result = await apiClient.ApiExecuteAsync<ResultMessageModel>(URL_DELETE, parentId);

            //ASSERT
            //expecting a false meaning this is an expected exception scenario
            Assert.True(!result.IsSuccess); 
            Assert.Contains("cannot be deleted because other type definitions depend", result.Message.ToLower());
        }

        [Theory]
        [MemberData(nameof(ProfileTypeDefControllerTestData))]
        public async Task DeleteItemIntermediateObject(ProfileTypeDefinitionModel model)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;

            //insert base parent
            var entityParent = await InsertMockProfileAndTypeDefinition(TYPE_ID_DEFAULT, _guidCommon);
            //extend base parent as item 1 - use this as composition for item 2
            var modelChild1New = await MapModelToExtendedItem(apiClient, _guidCommon, entityParent, model);
            var resultChild1Added = await apiClient.ApiExecuteAsync<ResultMessageWithDataModel>(URL_ADD, modelChild1New);
            var modelChild1Id = new IdIntModel() { ID = (int)resultChild1Added.Data };

            //wait to call this until we create the dependent records first
            _compositionRootId = entityParent.ID.Value;  //this will be how we get related compositions in the db
            //get lookup data to be used when adding attributes
            _lookupData = GetLookupData(apiClient, _guidCommon).Result;
            //get related lookup data - variable types list, compositions, interfaces. Using mock data to keep this stateless.
            _lookupRelated = GetRelatedData(entityParent.ProfileId.Value);

            //extend base parent as item 2 - add composition pointing to item 1
            var modelChild2New = await MapModelToExtendedItem(apiClient, _guidCommon, entityParent, model);
            //add composition attribute pointing to child 1
            var attr = CreateAttributeComposition($"Attribute-Comp-1", modelChild1New.Name, _guidCommon, _lookupRelated, _lookupData);
            modelChild2New.ProfileAttributes.Add(attr);
            var resultChild2Added = await apiClient.ApiExecuteAsync<ResultMessageWithDataModel>(URL_ADD, modelChild2New);
            var modelChild2Id = new IdIntModel() { ID = (int)resultChild2Added.Data };

            //visit item 2, visit item 1 (get by id)
            //call the get on the new item so we force an analytics tally - testing FK/cascade delete scenario
            await apiClient.ApiExecuteAsync<ResultMessageWithDataModel>(URL_GETBYID, modelChild1Id);
            await apiClient.ApiExecuteAsync<ResultMessageWithDataModel>(URL_GETBYID, modelChild2Id);

            // ACT
            //delete item 1 - expect fail due to item 2 depending on item 1 
            var result = await apiClient.ApiExecuteAsync<ResultMessageModel>(URL_DELETE, modelChild1Id);
            //delete item 2 - expect pass and testing that intermediate object (associated with composition) is not an issue
            var result2 = await apiClient.ApiExecuteAsync<ResultMessageModel>(URL_DELETE, modelChild2Id);

            //ASSERT
            //expecting a false meaning this is an expected exception scenario
            Assert.True(!result.IsSuccess);
            Assert.Contains("cannot be deleted because this is extended by", result.Message.ToLower());

            //ASSERT
            Assert.True(result2.IsSuccess);
            Assert.Contains("item was deleted", result2.Message.ToLower());
            //Try to get the item and should throw bad request
            await Assert.ThrowsAsync<MyNamespace.ApiException>(
                async () => await apiClient.ApiGetItemAsync<ProfileTypeDefinitionModel>(URL_GETBYID, modelChild2Id));
        }

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
                var parentClass = CreateEntity(0, profileMine.ID, null, TYPE_ID_DEFAULT, _guidCommon, Guid.NewGuid(), user);
                parentClass.AuthorId = null;
                parentClass.OwnerId = null;
                parentClass.ExternalAuthor = _guidCommon.ToString();
                await repo.AddAsync(parentClass);
                var parentInterface = CreateEntity(0, profileMine.ID, null, TYPE_ID_DEFAULT, _guidCommon, Guid.NewGuid(), user);
                parentInterface.ProfileTypeId = (int)ProfileItemTypeEnum.Interface;
                parentInterface.AuthorId = null;
                parentInterface.OwnerId = null;
                parentInterface.ExternalAuthor = _guidCommon.ToString();
                await repo.AddAsync(parentInterface);
                var parentEnum = CreateEntity(0, profileMine.ID, null, TYPE_ID_DEFAULT, _guidCommon, Guid.NewGuid(), user);
                parentEnum.ProfileTypeId = (int)ProfileItemTypeEnum.Enumeration;
                parentEnum.AuthorId = null;
                parentEnum.OwnerId = null;
                parentEnum.ExternalAuthor = _guidCommon.ToString();
                await repo.AddAsync(parentEnum);
                var parentStructure = CreateEntity(0, profileMine.ID, null, TYPE_ID_DEFAULT, _guidCommon, Guid.NewGuid(), user);
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
                    var entity = CreateEntity(i, i % 3 == 0 ? profileCore.ID : profileMine.ID, p, p.ProfileTypeId.Value, _guidCommon, uuid, user);
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
