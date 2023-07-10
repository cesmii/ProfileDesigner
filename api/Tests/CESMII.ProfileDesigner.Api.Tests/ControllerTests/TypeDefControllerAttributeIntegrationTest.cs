using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using Xunit;
using Xunit.Abstractions;

using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Api.Shared.Models;

namespace CESMII.ProfileDesigner.Api.Tests.Int
{
    public class TypeDefControllerAttributeIntegrationTest : ProfileTypeDefControllerTestBase
    {
        #region API constants
        #endregion

        public TypeDefControllerAttributeIntegrationTest(
            CustomWebApplicationFactory<Api.Startup> factory,
            ITestOutputHelper output) :
            base(factory, output)
        {
        }

#pragma warning disable xUnit1026  // Stop warnings related to parameters not used in test cases. 

        /// <summary>
        /// Extend from an item, then Add many different attributes and save. Confirm attributes added properly
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Theory]
        [InlineData(AttributeTypeIdEnum.DataVariable, "_BaseDataVariableType", "_Boolean", 5)]
        [InlineData(AttributeTypeIdEnum.DataVariable, "_BaseDataVariableType", "_Float", 4)]
        [InlineData(AttributeTypeIdEnum.DataVariable, "_BaseDataVariableType", "_Double", 3)]
        //[InlineData(AttributeTypeIdEnum.Property, null, "Int64", 5)]
        //[InlineData(AttributeTypeIdEnum.Property, null, "Counter", 4)]
        //[InlineData(AttributeTypeIdEnum.Property, null, "String", 3)]
        //[InlineData(AttributeTypeIdEnum.StructureField, "BaseDataVariableType", "", 4)]
        //[InlineData(AttributeTypeIdEnum.EnumField, "BaseDataVariableType", "", 3)]
        public async Task AddItem_AddAttributes(AttributeTypeIdEnum attrType, string variableTypeName
            , string dataTypeName, int numItems)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;

            //note - base.ApiClient - this will force creation of test user if not yet added, this is needed downstream
            //if we run this from a pristine db
            base.PrepareMockData(base.ApiClient);

            //create parent profile and entity to extend
            var itemExtend = await InsertMockProfileAndTypeDefinition(TYPE_ID_DEFAULT, _guidCommon);
            var model = CreateItemModel(1, itemExtend.ProfileId, itemExtend, _guidCommon, Guid.NewGuid());
            var resultExtend = await MapModelToExtendedItem(apiClient, _guidCommon, itemExtend, model);

            //add attributes
            resultExtend.ProfileAttributes = new List<ProfileAttributeModel>();
            for (int i = 1; i <= numItems; i++)
            {
                var attr = CreateAttribute($"Attribute-{i}", attrType, variableTypeName, dataTypeName, _guidCommon);
                resultExtend.ProfileAttributes.Add(attr);
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
        /// <remarks>To pass in multiple strings in each test, separate with |</remarks>
        /// <param name="model"></param>
        /// <returns></returns>
        [Theory]
        [InlineData(5)]
        [InlineData(4)]
        [InlineData(3)]
        [InlineData(2)]
        [InlineData(1)]
        public async Task AddItem_AddAttributeComposition(int numItems)
        {
            const string COMPOSITION_NAME_ROOT = "_BaseComposition_";

            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;

            //note - base.ApiClient - this will force creation of test user if not yet added, this is needed downstream
            //if we run this from a pristine db
            base.PrepareMockData(base.ApiClient);

            //create parent profile and entity to extend
            var itemExtend = await InsertMockProfileAndTypeDefinition(TYPE_ID_DEFAULT, _guidCommon);
            var model = CreateItemModel(1, itemExtend.ProfileId, itemExtend, _guidCommon, Guid.NewGuid());
            var resultExtend = await MapModelToExtendedItem(apiClient, _guidCommon, itemExtend, model);

            //add attributes
            resultExtend.ProfileAttributes = new List<ProfileAttributeModel>();
            for (int i = 1; i <= numItems; i++)
            {
                var n = $"{COMPOSITION_NAME_ROOT}{i}";
                resultExtend.ProfileAttributes.Add(CreateAttributeComposition($"Attribute-Comp-{i}", n, _guidCommon,
                    _lookupRelated, _lookupData));
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
            string name, AttributeTypeIdEnum attrTypeId, string variableTypeName, string dataTypeName, Guid guidCommon)
        {
            var dt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            var attrType = _lookupData.AttributeTypes.Find(x => x.ID.Value.Equals((int)attrTypeId));
            var dataType = _lookupData.DataTypes.Find(x => x.Name.ToLower().Equals(dataTypeName.ToLower()));
            var varType = string.IsNullOrEmpty(variableTypeName) ? null : _lookupRelated.VariableTypes.Find(x => x.Name.ToLower().Equals(variableTypeName.ToLower()));
            //var engUnit = string.IsNullOrEmpty(engUnitName) ? null : _lookupData.EngUnits.Find(x => x.DisplayName.ToLower().Equals(engUnitName.ToLower()));

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
                //EngUnit = engUnit,
                Name = name
            };
        }

        #endregion

        //dispose happens in base class.

    }
}
