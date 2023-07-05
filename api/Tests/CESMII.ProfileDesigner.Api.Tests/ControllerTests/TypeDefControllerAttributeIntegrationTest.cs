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
    public class TypeDefControllerAttributeIntegrationTest : ProfileTypeDefControllerIntegrationTestBase
    {
        //get some lookup data that will be needed when we start adding addtribute tests
        private AppLookupModel _lookupData = null;

        private const string _attributeBasic = "{'id':-1,'name':'Attr1'," +
                                                     "'dataType':null," +
                                                     "'attributeType':null," +
                                                     "'minValue':null,'maxValue':null,'engUnit':null,'compositionId':-1,'composition':null,'interfaceId':-1,'interface':null,'description':'','displayName':'','typeDefinitionId':null,'isArray':false,'isRequired':false,'enumValue':null," +
                                                     "'variableTypeDefinition':null}";

        private const string _attributeComposition = "{'id':-1,'name':'A Comp Attr','dataType':{'id':1,'name':'Composition','customTypeId':null,'customType':null},'attributeType':{'name':'Composition','code':'Composition','lookupType':2,'typeId':2,'displayOrder':9999,'isActive':false,'id':9}," + 
                                                     "'minValue':null,'maxValue':null,'engUnit':null,'compositionId':-999,'composition':{'id':-999,'name':'Test Comp Add','description':'','browseName':'','relatedProfileTypeDefinitionId':-999,'relatedName':'Test Comp Add'}," + 
                                                     "'interfaceId':-1,'interface':null,'description':'','displayName':'','typeDefinitionId':-888,'isArray':false,'isRequired':false,'enumValue':null}";

        private const string _attributeDataVariable_Bool = "{'id':-1,'name':'A Data Var Attr::Bool'," + 
                                                     "'dataType':{'baseDataTypeId':3,'popularityLevel':3,'popularityIndex':223,'usageCount':223,'manualRank':0,'isCustom':true,'name':'Boolean','code':'http://opcfoundation.org/UA/.i=1','useMinMax':false,'useEngUnit':false,'isNumeric':false,'displayOrder':9999,'isActive':false,'customTypeId':5,'customType':null,'ownerId':null,'id':6}," +
                                                     "'attributeType':{'name':'Data Variable','code':'DataVariable','lookupType':2,'typeId':2,'displayOrder':9999,'isActive':false,'id':6}," +
                                                     "'minValue':null,'maxValue':null,'engUnit':null,'compositionId':-1,'composition':null,'interfaceId':-1,'interface':null,'description':'','displayName':'','typeDefinitionId':null,'isArray':false,'isRequired':false,'enumValue':null," +
                                                     "'variableTypeDefinition':{'id':1347,'name':'BaseDataVariableType','browseName':'http://opcfoundation.org/UA/;BaseDataVariableType'},'variableTypeDefinitionId':1347}";

        private const string _attributeDataVariable_Double = "{'id':-2,'name':'A Data Var Attr::Double'," +
                                                     "'dataType':{'baseDataTypeId':4,'popularityLevel':3,'popularityIndex':44,'usageCount':44,'manualRank':0,'isCustom':true,'name':'Double','code':'http://opcfoundation.org/UA/.i=11','useMinMax':true,'useEngUnit':true,'isNumeric':true,'displayOrder':9999,'isActive':false,'customTypeId':93,'customType':null,'ownerId':null,'id':25}," +
                                                     "'attributeType':{'name':'Data Variable','code':'DataVariable','lookupType':2,'typeId':2,'displayOrder':9999,'isActive':false,'id':6}," +
                                                     "'minValue':null,'maxValue':null,'engUnit':null,'compositionId':-1,'composition':null,'interfaceId':-1,'interface':null,'description':'','displayName':'','typeDefinitionId':null,'isArray':false,'isRequired':false,'enumValue':null," +
                                                     "'variableTypeDefinition':{'id':1347,'name':'BaseDataVariableType','browseName':'http://opcfoundation.org/UA/;BaseDataVariableType'},'variableTypeDefinitionId':1347}";

        #region API constants
        private const string URL_LOOKUP_ALL = "/api/lookup/all";
        //private const string URL_INIT = "/api/profiletypedefinition/init";
        //private const string URL_EXTEND = "/api/profiletypedefinition/extend";
        //private const string URL_ADD = "/api/profiletypedefinition/add";
        //private const string URL_GETBYID = "/api/profiletypedefinition/getbyid";
        //private const string URL_DELETE = "/api/profiletypedefinition/delete";
        //private const string URL_DELETE_MANY = "/api/profiletypedefinition/deletemany";
        #endregion

        #region data naming constants
        /*
        private const string NAME_PATTERN = "CESMII.TypeDef";
        private const string PARENT_PROFILE_NAMESPACE = "https://CESMII.Profile.Mock.org/";
        private const string TITLE_PATTERN = "CESMII.ProfileDesigner.Api.Tests.Integration";
        private const string CATEGORY_PATTERN = "category-test";
        private const string VERSION_PATTERN = "1.0.0.";
        private const int TYPE_ID_DEFAULT = (int)ProfileItemTypeEnum.Class;
        */
        #endregion

        #region Data Type Constants
        #endregion

        public TypeDefControllerAttributeIntegrationTest(
            CustomWebApplicationFactory<Api.Startup> factory, 
            ITestOutputHelper output):
            base(factory, output)
        {
            //load lookup data that will be needed for attribute adds
            //note there is a dependency that the DB contains this look up data prior to the test being run.
            //some of the data is seeded by the create db script, some of the data is seeded by the import of the 
            //2 root nodesets used everywhere - ua and ua/di

            //get lookup data to be used when adding attributes
            var apiClient = base.ApiClient;
            _lookupData = apiClient.ApiGetItemGenericAsync<AppLookupModel>(URL_LOOKUP_ALL, method:"GET").Result;
        }

#pragma warning disable xUnit1026  // Stop warnings related to parameters not used in test cases. 

        /// <summary>
        /// Extend from an item, then Add many different attributes and save. Confirm attributes added properly
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Theory]
        [InlineData(AttributeTypeIdEnum.DataVariable, "BaseDataType", null, 5)]
        public async Task AddItem_AddAttributes(AttributeTypeIdEnum attrType, string dataTypeName, string engUnitName, int numItems)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;

            //create parent profile and entity to extend
            var itemExtend = await InsertMockProfileAndExtendEntity(_guidCommon);
            var model = CreateItemModel(1,itemExtend.ProfileId, itemExtend, _guidCommon, Guid.NewGuid());
            var resultExtend = await MapModelToExtendedItem(apiClient, _guidCommon, itemExtend, model);

            //add attributes
            resultExtend.ProfileAttributes = new List<ProfileAttributeModel>();
            for (int i = 1; i <= numItems; i++)
            {
                resultExtend.ProfileAttributes.Add(CreateAttribute($"Attribute-{i}", attrType, dataTypeName, engUnitName, resultExtend, _guidCommon));
            }

            // ACT
            //save item w/ attrs.
            var resultAdd = await apiClient.ApiExecuteAsync<ResultMessageWithDataModel>(URL_ADD, resultExtend);
            var modelGet = new IdIntModel() { ID = (int)resultAdd.Data };
            var resultGet = await apiClient.ApiGetItemAsync<ProfileTypeDefinitionModel>(URL_GETBYID, modelGet);

            //ASSERT - test the attribute values specifically, the type def tests happen somewhere else so no need
            // to overly scrutinize type def vals
            Assert.NotNull(resultExtend);
            Assert.Equal(numItems, resultGet.ProfileAttributes.Count);
            Assert.Equal(numItems, resultGet.ProfileAttributes.Count(x => x.DataType.Name.Equals(dataTypeName)));
        }


        #region Helper Methods
        /// <summary>
        /// Create an attribute
        /// </summary>
        private ProfileAttributeModel CreateAttribute(
            string name, AttributeTypeIdEnum attrTypeId, string dataTypeName, string engUnitName,
            ProfileTypeDefinitionModel typeDef,
            Guid guidCommon)
        {
            var dt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            var attrType = _lookupData.AttributeTypes.Find(x => x.ID.Value.Equals((int)attrTypeId));
            var dataType = _lookupData.DataTypes.Find(x => x.Name.ToLower().Equals(dataTypeName.ToLower()));
            var engUnit = string.IsNullOrEmpty(engUnitName) ? null : _lookupData.EngUnits.Find(x => x.DisplayName.ToLower().Equals(engUnitName.ToLower()));

            return new ProfileAttributeModel()
            {
                //TypeDefinition = typeDef,
                //TypeDefinitionId = typeDef.ID,
                AttributeType = attrType,
                BrowseName = guidCommon.ToString(),
                DataType = dataType,
                DataTypeId = dataType.ID,
                //VariableTypeDefinition = varType,
                //VariableTypeDefinitionId = varType.ID,
                EngUnit = engUnit,
                Name = name
            };
        }


        /// <summary>
        /// Delete items created during each test
        /// User <_guidCommon> as way to find items to delete 
        /// </summary>
        /// <returns></returns>
        protected override async Task CleanupEntities()
        {
            base.CleanupEntities();
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
