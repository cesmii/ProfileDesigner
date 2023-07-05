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
    public class TypeDefControllerAttributeIntegrationTest : ProfileTypeDefControllerTestBase
    {
        //get some lookup data that will be needed when we start adding addtribute tests
        private AppLookupModel _lookupData = null;
        private ProfileLookupModel _lookupRelated = null;

        #region API constants
        private const string URL_LOOKUP_ALL = "/api/lookup/all";
        private const string URL_LOOKUP_RELATED = "/api/profiletypedefinition/lookup/profilerelated/extend";
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
            //get related lookup data which depends on existence of type def
            _lookupRelated = apiClient.ApiGetItemGenericAsync<ProfileLookupModel>(URL_LOOKUP_RELATED).Result;

        }

#pragma warning disable xUnit1026  // Stop warnings related to parameters not used in test cases. 

        /// <summary>
        /// Extend from an item, then Add many different attributes and save. Confirm attributes added properly
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Theory]
        [InlineData(AttributeTypeIdEnum.DataVariable, "BaseDataVariableType", "Boolean", null, 5)]
        [InlineData(AttributeTypeIdEnum.DataVariable, "TransitionVariableType", "Float", null, 4)]
        [InlineData(AttributeTypeIdEnum.DataVariable, "AnalogItemType", "Double", null, 3)]
        //[InlineData(AttributeTypeIdEnum.Property, null, "Int64", null, 5)]
        //[InlineData(AttributeTypeIdEnum.Property, null, "Counter", null, 4)]
        //[InlineData(AttributeTypeIdEnum.Property, null, "String", null, 3)]
        //[InlineData(AttributeTypeIdEnum.StructureField, "BaseDataVariableType", "", null, 4)]
        //[InlineData(AttributeTypeIdEnum.EnumField, "BaseDataVariableType", "", null, 3)]
        public async Task AddItem_AddAttributes(AttributeTypeIdEnum attrType, string variableTypeName
            , string dataTypeName, string engUnitName, int numItems)
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
                resultExtend.ProfileAttributes.Add(CreateAttribute($"Attribute-{i}", attrType, variableTypeName, dataTypeName, engUnitName, _guidCommon));
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
            Assert.Equal(numItems, resultGet.ProfileAttributes.Count(x => x.VariableTypeDefinition.Name.Equals(variableTypeName)));
            Assert.Equal(numItems, resultGet.ProfileAttributes.Count(x => x.DataType.Name.Equals(dataTypeName)));
        }

        /// <summary>
        /// Extend from an item, then Add many different composition attributes and save. Confirm attributes added properly
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Theory]
        [InlineData("AlarmMetricsType", 1)]
        [InlineData("AliasNameType", 1)]
        [InlineData("PubSubConfigurationType", 1)]
        [InlineData("ServerType", 1)]
        public async Task AddItem_AddAttributeComposition(string compositionName, int numItems)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;

            //create parent profile and entity to extend
            var itemExtend = await InsertMockProfileAndExtendEntity(_guidCommon);
            var model = CreateItemModel(1, itemExtend.ProfileId, itemExtend, _guidCommon, Guid.NewGuid());
            var resultExtend = await MapModelToExtendedItem(apiClient, _guidCommon, itemExtend, model);

            //add attributes
            resultExtend.ProfileAttributes = new List<ProfileAttributeModel>();
            for (int i = 1; i <= numItems; i++)
            {
                resultExtend.ProfileAttributes.Add(CreateAttributeComposition($"Attribute-Comp-{i}", compositionName, _guidCommon));
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
            Assert.Equal(numItems, resultGet.ProfileAttributes.Count(x => x.Composition != null && x.Composition.Name.StartsWith("Attribute-Comp-")));
        }

        #region Helper Methods
        /// <summary>
        /// Create an attribute
        /// </summary>
        private ProfileAttributeModel CreateAttribute(
            string name, AttributeTypeIdEnum attrTypeId, string variableTypeName, string dataTypeName, 
            string engUnitName, Guid guidCommon)
        {
            var dt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            var attrType = _lookupData.AttributeTypes.Find(x => x.ID.Value.Equals((int)attrTypeId));
            var dataType = _lookupData.DataTypes.Find(x => x.Name.ToLower().Equals(dataTypeName.ToLower()));
            var varType = string.IsNullOrEmpty(variableTypeName) ? null : _lookupRelated.VariableTypes.Find(x => x.Name.ToLower().Equals(variableTypeName.ToLower()));
            var engUnit = string.IsNullOrEmpty(engUnitName) ? null : _lookupData.EngUnits.Find(x => x.DisplayName.ToLower().Equals(engUnitName.ToLower()));

            return new ProfileAttributeModel()
            {
                //TypeDefinition = typeDef,
                //TypeDefinitionId = typeDef.ID,
                AttributeType = attrType,
                BrowseName = guidCommon.ToString(),
                DataType = dataType,
                DataTypeId = dataType.ID,
                VariableTypeDefinition = string.IsNullOrEmpty(variableTypeName) ? null :
                        new ProfileTypeDefinitionModel() { ID = varType.ID, Name = varType.Name, BrowseName = varType.BrowseName },
                VariableTypeDefinitionId = string.IsNullOrEmpty(variableTypeName) ? null : varType.ID,
                EngUnit = engUnit,
                Name = name
            };
        }

        /// <summary>
        /// Create an attribute
        /// </summary>
        private ProfileAttributeModel CreateAttributeComposition(string name, string compositionName, Guid guidCommon)
        {
            var dt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            var comp = _lookupRelated.Compositions.Find(x => x.Name.ToLower().Equals(compositionName.ToLower()));
            var attrType = _lookupData.AttributeTypes.Find(x => x.Name.ToLower().Equals("composition"));
            //var dataType = _lookupData.DataTypes.Find(x => x.Name.ToLower().Equals("composition"));

            return new ProfileAttributeModel()
            {
                CompositionId = comp.ID,
                Composition = new ProfileTypeDefinitionRelatedModel()
                {
                    ID = comp.ID,
                    SymbolicName = _guidCommon.ToString(),
                    RelatedProfileTypeDefinitionId = comp.ID,
                    Name = comp.Name
                },
                AttributeType = attrType,
                BrowseName = guidCommon.ToString(),
                SymbolicName = guidCommon.ToString(),
                //DataType = dataType,
                //DataTypeId = dataType.ID,
                Name = name
            };
        }
        #endregion

        //dispose happens in base class.
        //public override void Dispose()
        //{
        //    CleanupEntities().Wait();
        //}

    }
}
