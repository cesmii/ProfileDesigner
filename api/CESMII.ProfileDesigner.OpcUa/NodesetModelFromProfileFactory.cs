﻿using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.OpcUa.NodeSetModel;
using CESMII.OpcUa.NodeSetModel.Factory.Opc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using System.Text.Json;
using CESMII.ProfileDesigner.OpcUa.NodeSetModelImport.Profile;

namespace CESMII.ProfileDesigner.OpcUa.NodeSetModelFactory.Profile
{
    public interface IDALContext
    {
        Dictionary<string, ProfileTypeDefinitionModel> profileItems { get; }
        UserToken authorId { get; }

        ProfileTypeDefinitionModel GetProfileItemById(int? propertyTypeDefinitionId);

        bool UpdateExisting { get; }
        Task<(int?, bool)> UpsertAsync(ProfileTypeDefinitionModel profileItem, bool updateExisting);

        LookupDataTypeModel GetDataType(string text);
        Task<int?> CreateCustomDataTypeAsync(LookupDataTypeModel customDataTypeLookup);
        Task<LookupDataTypeModel> GetCustomDataTypeAsync(ProfileTypeDefinitionModel customDataTypeProfile);
        object GetNodeSetCustomState(string uaNamespace);
        EngineeringUnitModel GetOrCreateEngineeringUnitAsync(EngineeringUnitModel engUnitLookup);
        ILogger Logger { get; }

        ProfileTypeDefinitionSimpleModel MapToModelProfileSimple(ProfileTypeDefinitionModel profileTypeDef);
        ProfileModel GetProfileForNamespace(string uaNamespace);
        ProfileTypeDefinitionModel CheckExisting(ProfileTypeDefinitionModel profileItem);
    }

    public class NodeModelFromProfileFactory : NodeModelFromProfileFactory<NodeModel>
    {

    }
    public class NodeModelFromProfileFactory<T> where T : NodeModel, new()
    {
        public T _model;
        protected ILogger Logger;
        public static NodeModel Create(ProfileTypeDefinitionModel profileItem, IOpcUaContext opcContext, IDALContext dalContext)
        {
            NodeModel nodeModel;
            if (string.IsNullOrEmpty(profileItem.OpcNodeId))
            {
                opcContext.Logger.LogInformation($"{profileItem}: no NodeId specified - generated {profileItem.OpcNodeId}");
                profileItem.OpcNodeId = $"g={Guid.NewGuid()}";// new ExpandedNodeId(Guid.NewGuid(), profileItem.Namespace).ToString();
            }
            switch ((ProfileItemTypeEnum)profileItem.TypeId)
            {
                case ProfileItemTypeEnum.Class:
                    nodeModel = Create<ObjectTypeModelFromProfileFactory, ObjectTypeModel>(profileItem, opcContext, dalContext);
                    break;
                case ProfileItemTypeEnum.Interface:
                    nodeModel = Create<InterfaceModelFromProfileFactory, InterfaceModel>(profileItem, opcContext, dalContext);
                    break;
                case ProfileItemTypeEnum.VariableType:
                    nodeModel = Create<VariableTypeModelFromProfileFactory, VariableTypeModel>(profileItem, opcContext, dalContext);
                    break;
                case ProfileItemTypeEnum.CustomDataType:
                case ProfileItemTypeEnum.Structure:
                case ProfileItemTypeEnum.Enumeration:
                    // OPC Custom Data Type
                    nodeModel = Create<DataTypeModelFromProfileFactory, DataTypeModel>(profileItem, opcContext, dalContext);
                    break;
                case ProfileItemTypeEnum.Object:
                    nodeModel = Create<ObjectModelFromProfileFactory, ObjectModel>(profileItem, opcContext, dalContext);
                    break;
                case ProfileItemTypeEnum.Method:
                    nodeModel = Create<MethodModelFromProfileFactory, MethodModel>(profileItem, opcContext, dalContext);
                    break;
                default:
                    throw new Exception($"Unknown profile type {profileItem.TypeId}");
            }
            return nodeModel;
        }

        private static TNodeModel Create<TNodeModelFactory, TNodeModel>(ProfileTypeDefinitionModel profileItem, IOpcUaContext opcContext, IDALContext dalContext) 
            where TNodeModel : NodeModel, new()
            where TNodeModelFactory : NodeModelFromProfileFactory<TNodeModel>, new()
        {
            var nodeModel = NodeModelFactoryOpc<TNodeModel>.Create<TNodeModel>(opcContext, GetProfileItemNodeId(profileItem), profileItem.Profile.Namespace, profileItem.Profile, out var created);
            var nodeModelFactory = new TNodeModelFactory { _model = nodeModel, Logger = opcContext.Logger };
            if (created)
            {
                if (profileItem.Attributes == null && profileItem.ID != null)
                {
                    // TODO Figure out why these are not getting loaded even with Verbose flag
                    profileItem = dalContext.GetProfileItemById(profileItem.ID);
                }

                nodeModelFactory.Initialize(profileItem, opcContext, dalContext);
            }
            else
            {
                opcContext.Logger.LogTrace($"Using previously created node model {nodeModel} for profile type definition {profileItem}");
            }
            return nodeModel;
        }

        protected static string GetProfileItemNodeId(ProfileTypeDefinitionModel profileItem)
        {
            return GetProfileItemNodeIdInternal(profileItem.OpcNodeId, profileItem.Profile.Namespace);
        }
        protected static string GetProfileItemNodeId(ProfileTypeDefinitionSimpleModel profileItem)
        {
            return GetProfileItemNodeIdInternal(profileItem.OpcNodeId, profileItem.Profile.Namespace);
        }
        protected static string GetProfileItemNodeId(ProfileTypeDefinitionModel profileType, ProfileAttributeModel profileAttribute, IDALContext dalContext, out string opcNamespace)
        {
            opcNamespace = profileAttribute.Namespace;
            if (string.IsNullOrEmpty(opcNamespace))
            {
                // Default to the parent profile's namespace (profile designer may not expose the attribute)
                opcNamespace = profileType?.Profile?.Namespace;
            }

            return GetProfileItemNodeIdInternal(profileAttribute.OpcNodeId, opcNamespace);
        }

        private static string GetProfileItemNodeIdInternal(string nodeIdIdentifier, string nodeIdNamespace)
        {
            return $"nsu={nodeIdNamespace};{nodeIdIdentifier}";
        }

        public virtual void Initialize(ProfileTypeDefinitionModel profileItem, IOpcUaContext opcContext, IDALContext dalContext)
        {
            // TODO capture locales in profile type definitions
            _model.Description = NodeModel.LocalizedText.ListFromText(profileItem.Description);
            _model.DisplayName = NodeModel.LocalizedText.ListFromText(profileItem.Name);
            _model.Namespace = profileItem.Profile.Namespace;

            _model.BrowseName = profileItem.BrowseName;
            _model.SymbolicName = profileItem.SymbolicName;
            _model.NodeId = GetProfileItemNodeId(profileItem);
            // Parent = ResolveProfileItem(profileItems, dataType.SuperType),
            //AuthorId = dalContext.authorId,
            //ExternalAuthor = "<nodeset author tbd>", // TODO fill in nodeset author information
            _model.Categories = profileItem.MetaTags?.Any() == true ? new List<string>(profileItem.MetaTags) : null;
            _model.Documentation = profileItem.DocumentUrl;

            Logger.LogTrace($"Creating node model {_model} from profile type definition {profileItem}");

            if (profileItem.Compositions != null)
            {
                foreach (var composition in profileItem.Compositions)
                {
                    if (composition.Type.ID == (int)ProfileItemTypeEnum.Method)
                    {
                        var uaMethod = MethodModelFromProfileFactory.Create(composition, profileItem, opcContext, dalContext);
                        _model.Methods.Add(uaMethod);
                    }
                    else if (composition.RelatedIsEvent == true)
                    {
                        var eventProfileType = composition.RelatedProfileTypeDefinition;
                        if (composition.RelatedProfileTypeDefinition == null)
                        {
                            eventProfileType = dalContext.GetProfileItemById(composition.RelatedProfileTypeDefinitionId);
                        }

                        var uaEventType = ObjectTypeModelFromProfileFactory.Create(eventProfileType, opcContext, dalContext) as ObjectTypeModel;
                        if (uaEventType != null)
                        {
                            _model.Events.Add(uaEventType);
                        }
                        else
                        {
                            throw new Exception($"Event Type {eventProfileType} not found.");
                        }
                    }
                    else
                    {
                        var uaObject = ObjectModelFromProfileFactory.Create(composition, profileItem, opcContext, dalContext);
                        uaObject.Parent = this._model;
                        if (string.IsNullOrEmpty(composition.RelatedReferenceId))
                        {
                            _model.Objects.Add(uaObject);
                        }
                        else
                        {
                            _model.OtherChilden.Add(new NodeModel.ChildAndReference { Child = uaObject, Reference = composition.RelatedReferenceId });
                        }
                    }
                }
            }
            if (profileItem.Attributes != null)
            {
                foreach (var attribute in profileItem.Attributes)
                {
                    if (string.IsNullOrEmpty(attribute.OpcNodeId))
                    {
                        attribute.OpcNodeId = $"g={Guid.NewGuid()}";
                    }
                    if (attribute.TypeDefinition == null && attribute.TypeDefinitionId == profileItem.ID)
                    {
                        attribute.TypeDefinition = profileItem;
                    }
                    if (attribute.AttributeType?.ID == (int)AttributeTypeIdEnum.Property/* || attribute.Description?.StartsWith(strPropertyPrefix) == true*/)
                    {
                        var propertyModel = VariableModelFromProfileFactory<PropertyModel>.Create<PropertyModel>(profileItem, attribute, opcContext, dalContext);
                        propertyModel.Parent = _model;
                        _model.Properties.Add(propertyModel);
                    }
                    else if (attribute.AttributeType?.ID == (int)AttributeTypeIdEnum.DataVariable)
                    {
                        var dataVariableModel = VariableModelFromProfileFactory<DataVariableModel>.Create<DataVariableModel>(profileItem, attribute, opcContext, dalContext);
                        dataVariableModel.Parent = _model;
                        _model.DataVariables.Add(dataVariableModel);
                    }
                    else if (attribute.AttributeType?.ID == (int)AttributeTypeIdEnum.StructureField)
                    {
                        var structure = this._model as DataTypeModel;
                        var fieldDataType = DataTypeModelFromProfileFactory.GetDataTypeModel(attribute, opcContext, dalContext);
                        if (fieldDataType == null)
                        {
                            throw new Exception($"Unable to resolve data type for field {attribute.DisplayName}");
                        }
                        var field = new DataTypeModel.StructureField
                        {
                            Name = attribute.Name,
                            DataType = fieldDataType,
                            Description = NodeModel.LocalizedText.ListFromText(attribute.Description),
                            IsOptional = !attribute.IsRequired ?? false,
                        };
                        if (structure.StructureFields == null)
                        {
                            structure.StructureFields = new List<DataTypeModel.StructureField>();
                        }
                        structure.StructureFields.Add(field);
                    }
                    else if (attribute.AttributeType?.ID == (int)AttributeTypeIdEnum.EnumField)
                    {
                        var enumeration = this._model as DataTypeModel;
                        var field = new DataTypeModel.UaEnumField
                        {
                            Name = attribute.Name,
                            DisplayName = NodeModel.LocalizedText.ListFromText(attribute.DisplayName),
                            Description = NodeModel.LocalizedText.ListFromText(attribute.Description),
                            Value = attribute.EnumValue.Value,
                        };
                        if (enumeration.EnumFields == null)
                        {
                            enumeration.EnumFields = new List<DataTypeModel.UaEnumField>();
                        }
                        enumeration.EnumFields.Add(field);
                    }
                    else
                    {
                        throw new Exception($"Unexpected attribute type {attribute?.ID} on {attribute.DisplayName}");
                    }
                }
            }
            if (profileItem.Interfaces?.Any() == true)
            {
                foreach (var profileInterface in profileItem.Interfaces)
                {
                    var fullInterface = dalContext.GetProfileItemById(profileInterface.ID); // Some properties don't seem to get loaded: 
                    var interfaceModel = InterfaceModelFromProfileFactory.Create(fullInterface, opcContext, dalContext) as InterfaceModel;
                    _model.Interfaces.Add(interfaceModel);
                }
            }
            Logger.LogTrace($"Created node model {_model} from profile type definition {profileItem}");
        }

    }

    public class InstanceModelFromProfileFactory<TInstanceModel, TBaseTypeModel, TBaseTypeModelFromProfileFactory> : NodeModelFromProfileFactory<TInstanceModel>
        where TInstanceModel : InstanceModel<TBaseTypeModel>, new()
        where TBaseTypeModel : BaseTypeModel, new()
        where TBaseTypeModelFromProfileFactory : NodeModelFromProfileFactory<TBaseTypeModel>, new()
    {

        public override void Initialize(ProfileTypeDefinitionModel profileItem, IOpcUaContext opcContext, IDALContext dalContext)
        {
            base.Initialize(profileItem, opcContext, dalContext);
            if (profileItem.Parent != null)
            {
                var parentProfile = dalContext.GetProfileItemById(profileItem.Parent.ID);
                var parentModel = NodeModelFromProfileFactory<TBaseTypeModel>.Create(parentProfile, opcContext, dalContext);
                if (parentModel is TBaseTypeModel typeDefinition)
                {
                    Logger.LogTrace($"Using type definition {typeDefinition} for profile type {profileItem}");
                    _model.TypeDefinition = typeDefinition;
                }
                else
                {
                    Logger.LogError($"Invalid type definition for Instance {profileItem}");
                    throw new Exception($"Invalid type definition for Instance {profileItem}");
                }
            }
            if (profileItem.InstanceParent != null)
            {
                var instanceParentProfile = dalContext.GetProfileItemById(profileItem.InstanceParent.ID);
                if (instanceParentProfile == null)
                {
                    throw new Exception($"{profileItem.Name}/{profileItem.ID}: Instance parent {profileItem.InstanceParent.Name}/{profileItem.InstanceParent.ID} not found.");
                }
                var instanceParentModel = NodeModelFromProfileFactory<NodeModel>.Create(instanceParentProfile, opcContext, dalContext);
                _model.Parent = instanceParentModel;
            }
        }

        // TODO add more modeling rule flavors to the profile compositions / attributes
        public static string GetModelingRuleFromProfile(bool? isRequired, string modelingRule)
        {
            if (!string.IsNullOrEmpty(modelingRule))
            {
                return modelingRule;
            }
            if (isRequired == null)
            {
                return null;
            }
            return isRequired.Value ? "Mandatory" : "Optional"; // 
        }
    }

    public class ObjectModelFromProfileFactory : InstanceModelFromProfileFactory<ObjectModel, ObjectTypeModel, ObjectTypeModelFromProfileFactory>
    {
        internal static ObjectModel Create(ProfileTypeDefinitionRelatedModel objectTypeRelated, ProfileTypeDefinitionModel composingProfile, IOpcUaContext opcContext, IDALContext dalContext)
        {
            var objectProfile = objectTypeRelated.RelatedProfileTypeDefinition;
            if (objectProfile == null)
            {
                objectProfile = dalContext.GetProfileItemById(objectTypeRelated.RelatedProfileTypeDefinitionId);
            }
            var objectOrTypeModel = Create(objectProfile, opcContext, dalContext);
            var modelingRule = GetModelingRuleFromProfile(objectTypeRelated.RelatedIsRequired, objectTypeRelated.RelatedModelingRule);
            if (objectOrTypeModel is ObjectModel objectModel)
            {
                objectModel.ModelingRule = modelingRule;
                return objectModel;
            }
            else if (objectOrTypeModel is ObjectTypeModel objectTypeModel)
            {
                // In the profile designer we allow composition directly with a class: create the intermediate OPC Object required
                try
                {
                    objectModel = NodeModelFactoryOpc<NodeModel>.Create<ObjectModel>(opcContext, GetProfileItemNodeId(objectTypeRelated), objectTypeRelated.Profile.Namespace ?? composingProfile.Profile.Namespace, objectTypeRelated.Profile, out var created);
                    if (created)
                    {
                        objectModel.DisplayName = NodeModel.LocalizedText.ListFromText(objectTypeRelated.Name);
                        objectModel.SymbolicName = objectTypeRelated.SymbolicName;
                        objectModel.BrowseName = objectTypeRelated.BrowseName;
                        objectModel.TypeDefinition = objectTypeModel;
                        objectModel.ModelingRule = modelingRule;
                    }
                    return objectModel;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("NodeModel was previously created with a different, incompatible type")) // TODO turn this into a dedicated exception or track when a variable is a ObjectType...
                    {
                        opcContext.Logger.LogError($"{ex.Message}");
                        throw;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else
            {
                throw new Exception($"Unexpected profile type {objectOrTypeModel.GetType().FullName} for composition {objectTypeRelated.ID}");
            }
        }
    }

    public class BaseTypeModelFromProfileFactory<TBaseTypeModel> : NodeModelFromProfileFactory<TBaseTypeModel> where TBaseTypeModel : BaseTypeModel, new()
    {
        public override void Initialize(ProfileTypeDefinitionModel profileItem, IOpcUaContext opcContext, IDALContext dalContext)
        {
            base.Initialize(profileItem, opcContext, dalContext);
            if (profileItem.Parent != null)
            {
                var superTypeProfile = dalContext.GetProfileItemById(profileItem.Parent.ID);
                if (superTypeProfile == null)
                {
                    throw new Exception($"{profileItem.Name}/{profileItem.ID}: Super Type {profileItem.Parent.Name}/{profileItem.Parent.ID} not found.");
                }
                var superTypeModel = NodeModelFromProfileFactory<TBaseTypeModel>.Create(superTypeProfile, opcContext, dalContext);
                if (!(superTypeModel is BaseTypeModel))
                {
                    throw new Exception($"{profileItem.Name}/{profileItem.ID}: Super Type {superTypeProfile.Name}/{superTypeProfile.ID} is not an OPC Object Type.");
                }
                _model.SuperType = superTypeModel as BaseTypeModel;
            }
            _model.IsAbstract = profileItem.IsAbstract;
        }

    }


    public class ObjectTypeModelFromProfileFactory : BaseTypeModelFromProfileFactory<ObjectTypeModel>
    {
    }

    public class InterfaceModelFromProfileFactory : BaseTypeModelFromProfileFactory<InterfaceModel>//ObjectTypeModelFromProfileFactory<InterfaceModel>
    {
    }

    public class VariableModelFromProfileFactory<TVariableModel> : InstanceModelFromProfileFactory<TVariableModel, VariableTypeModel, VariableTypeModelFromProfileFactory>
        where TVariableModel : VariableModel, new()
    {
        internal static T Create<T>(ProfileTypeDefinitionModel profileType, ProfileAttributeModel attribute, IOpcUaContext opcContext, IDALContext dalContext) where T : VariableModel, new()
        {
            var variableModel = NodeModelFactoryOpc.Create<T>(opcContext, GetProfileItemNodeId(profileType, attribute, dalContext, out var opcNamespace), opcNamespace, profileType, out var created);
            if (created)
            {
                var dataTypeModel = DataTypeModelFromProfileFactory.GetDataTypeModel(attribute, opcContext, dalContext);
                if (dataTypeModel == null)
                {
                    throw new Exception($"Unable to resolve data type {attribute.Name}");
                }

                VariableTypeModel variableTypeModel = null;
                if (attribute.VariableTypeDefinition != null)
                {
                    variableTypeModel = NodeModelFromProfileFactory.Create(attribute.VariableTypeDefinition, opcContext, dalContext) as VariableTypeModel;
                }
                variableModel.DataType = dataTypeModel;
                variableModel.TypeDefinition = variableTypeModel;
                variableModel.DisplayName = NodeModel.LocalizedText.ListFromText(attribute.Name);
                variableModel.SymbolicName = attribute.SymbolicName;
                variableModel.BrowseName = attribute.BrowseName;
                variableModel.Description = NodeModel.LocalizedText.ListFromText(attribute.Description);
                variableModel.ModelingRule = GetModelingRuleFromProfile(attribute.IsRequired, attribute.ModelingRule);
                if (attribute.ValueRank != null)
                {
                    variableModel.ValueRank = attribute.ValueRank;
                }
                else
                {
                    variableModel.ValueRank = attribute.IsArray ? 1 : -1;
                }
                if (!string.IsNullOrEmpty(attribute.ArrayDimensions))
                {
                    variableModel.ArrayDimensions = attribute.ArrayDimensions;
                }
                else
                {
                    variableModel.ArrayDimensions = null;
                }

                if (!string.IsNullOrEmpty(attribute.DataVariableNodeIds))
                {
                    // Workaround to preserve nodeids for data variables for data type fields
                    var map = DataVariableNodeIdMap.GetMap(attribute.DataVariableNodeIds);
                    ProcessDataVariableNodeIds(map, variableModel, opcContext, profileType);
                }

                if (attribute.EngUnit != null)
                {
                    variableModel.EngineeringUnit = new VariableModel.EngineeringUnitInfo
                    {
                        DisplayName = attribute.EngUnit.DisplayName,
                        Description = attribute.EngUnit.Description,
                        NamespaceUri = attribute.EngUnit.NamespaceUri,
                        UnitId = attribute.EngUnit.UnitId,
                    };
                }
                variableModel.EngUnitNodeId = attribute.EngUnitOpcNodeId;
                variableModel.MinValue = (double?)attribute.MinValue;
                variableModel.MaxValue = (double?)attribute.MaxValue;
                variableModel.InstrumentMinValue = (double?)attribute.InstrumentMinValue;
                variableModel.InstrumentMaxValue = (double?)attribute.InstrumentMaxValue;
                variableModel.Value = attribute.AdditionalData;

                variableModel.AccessLevel = attribute.AccessLevel;
                variableModel.UserAccessLevel = attribute.UserAccessLevel;
                variableModel.AccessRestrictions = attribute.AccessRestrictions;
                variableModel.WriteMask = attribute.WriteMask;
                variableModel.UserWriteMask = attribute.UserWriteMask;
            }
            return variableModel;
        }

        private static void ProcessDataVariableNodeIds(DataVariableNodeIdMap map, VariableModel variableModel
            , IOpcUaContext opcContext, ProfileTypeDefinitionModel profileType)
        {
            foreach (var typeDataVariable in variableModel.TypeDefinition.DataVariables)
            {
                if (map.DataVariableNodeIdsByBrowseName.TryGetValue(typeDataVariable.BrowseName, out var mapEntry))
                {
                    var dataVariable = NodeModelFactoryOpc.Create<DataVariableModel>(opcContext, mapEntry.NodeId, variableModel.Namespace, profileType, out var dvCreated);
                    if (dvCreated)
                    {
                        dataVariable.DataType = typeDataVariable.DataType;
                        dataVariable.BrowseName = typeDataVariable.BrowseName;
                        dataVariable.CustomState = typeDataVariable.CustomState;
                        dataVariable.ArrayDimensions = typeDataVariable.ArrayDimensions;
                        dataVariable.Categories = typeDataVariable.Categories;
                        dataVariable.Description = typeDataVariable.Description;
                        dataVariable.DisplayName = typeDataVariable.DisplayName;
                        dataVariable.Documentation = typeDataVariable.Documentation;
                        //dataVariable.Namespace = typeDataVariable.Namespace;
                        dataVariable.NodeSet = typeDataVariable.NodeSet;
                        //dataVariable.SymbolicName = typeDataVariable.SymbolicName;
                        dataVariable.TypeDefinition = typeDataVariable.TypeDefinition;
                        dataVariable.Value = typeDataVariable.Value;
                        dataVariable.ValueRank = typeDataVariable.ValueRank;
                        dataVariable.Parent = variableModel;
                        dataVariable.ModelingRule = typeDataVariable.ModelingRule;
                        dataVariable.AccessLevel = typeDataVariable.AccessLevel;
                        dataVariable.UserAccessLevel = typeDataVariable.UserAccessLevel;
                        dataVariable.AccessRestrictions = typeDataVariable.AccessRestrictions;
                        dataVariable.WriteMask = typeDataVariable.WriteMask;
                        dataVariable.UserWriteMask = typeDataVariable.UserWriteMask;
                    }
                    variableModel.DataVariables.Add(dataVariable);
                    if (mapEntry.Map != null)
                    {
                        ProcessDataVariableNodeIds(mapEntry.Map, dataVariable, opcContext, profileType);
                    }
                }
            }
        }
    }

    public class DataVariableModelFromProfileFactory : VariableModelFromProfileFactory<DataVariableModel>
    {
    }

    public class PropertyModelFromProfileFactory : VariableModelFromProfileFactory<PropertyModel>
    {
    }

    public class MethodModelFromProfileFactory : NodeModelFromProfileFactory<MethodModel>
    {
        internal static MethodModel Create(ProfileTypeDefinitionRelatedModel methodRelated, ProfileTypeDefinitionModel composingProfile, IOpcUaContext opcContext, IDALContext dalContext)
        {
            var methodProfile = methodRelated.RelatedProfileTypeDefinition;
            if (methodProfile == null)
            {
                methodProfile = dalContext.GetProfileItemById(methodRelated.RelatedProfileTypeDefinitionId);
            }
            var nodeModel = Create(methodProfile, opcContext, dalContext);
            if (nodeModel is MethodModel methodModel)
            {
                return methodModel;
            }
            else
            {
                throw new Exception($"Unexpected profile type {nodeModel.GetType().FullName} for composition {methodRelated.ID}");
            }
        }

    }

    public class VariableTypeModelFromProfileFactory : BaseTypeModelFromProfileFactory<VariableTypeModel>
    {
    }
    public class DataTypeModelFromProfileFactory : BaseTypeModelFromProfileFactory<DataTypeModel>
    {
        public static DataTypeModel GetDataTypeModel(ProfileAttributeModel attribute, IOpcUaContext opcContext, IDALContext dalContext)
        {
            DataTypeModel dataTypeModel = null; // DataTypeModel.GetBuiltinDataType(opcContext, attribute.DataType);
            if (attribute.DataTypeId != 0 || attribute.DataType != null)
            {
                var customDataType = attribute.DataType;
                var customTypeProfile = customDataType?.CustomType;
                if (customTypeProfile == null)
                {
                    customTypeProfile = dalContext.GetProfileItemById(customDataType?.CustomTypeId ?? 0);
                }
                if (customTypeProfile != null)
                {
                    dataTypeModel = NodeModelFromProfileFactory<DataTypeModel>.Create(customTypeProfile, opcContext, dalContext) as DataTypeModel;
                }
                else
                {
                    throw new Exception($"Unable to resolve custom type profile {customDataType.Name}");
                }
            }

            return dataTypeModel;
        }

    }
}