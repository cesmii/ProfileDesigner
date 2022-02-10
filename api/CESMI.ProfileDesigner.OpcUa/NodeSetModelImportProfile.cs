using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.OpcUa.NodeSetModel.Factory.Opc;
using IDALContext = CESMII.ProfileDesigner.OpcUa.NodeSetModel.Factory.Profile.IDALContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using CESMII.ProfileDesigner.OpcUa.NodeSetModel.Opc.Extensions;
using Microsoft.Extensions.Logging;

using System.Text.Json;

namespace CESMII.ProfileDesigner.OpcUa.NodeSetModel.Import.Profile
{
    public class NodeModelImportProfile: NodeModelImportProfile<NodeModel>
    {

    }
    public class NodeModelImportProfile<T> where T : NodeModel, new()
    {
        public T _model;

        public ProfileTypeDefinitionModel ImportProfileItem(IDALContext dalContext)
        {
            if (dalContext.profileItems.TryGetValue(this._model.NodeId, out var existingProfileItem))
            {
                return existingProfileItem;
            }
            var profile = dalContext.GetNodeSetCustomState(this._model.Namespace) as ProfileModel;
            var profileItem = new ProfileTypeDefinitionModel
            {
                Description = this._model.Description?.FirstOrDefault()?.Text,
                Name = this._model.DisplayName?.FirstOrDefault()?.Text,
                ProfileId = profile.ID != 0 ? profile.ID : null,
                Profile = profile,
                BrowseName = this._model.BrowseName,
                SymbolicName = this._model.SymbolicName,
                OpcNodeId = NodeModelUtils.GetNodeIdIdentifier(this._model.NodeId),
                // Parent = ResolveProfileItem(profileItems, dataType.SuperType),
                TypeId = (int)ProfileItemTypeEnum.Class,
                AuthorId = dalContext.authorId?.UserId,
                ExternalAuthor = "<nodeset author tbd>", // TODO fill in nodeset author information
                MetaTags = _model.Categories?.Any() == true ? new List<string>(_model.Categories) : null,
                DocumentUrl = this._model.Documentation,
            };
            existingProfileItem = dalContext.CheckExisting(profileItem);
            if (existingProfileItem != null)
            {
                dalContext.profileItems.Add(this._model.NodeId, existingProfileItem);
                return existingProfileItem;
            }

            // Add incomplete profile item to the lookup list to support recursive types like DictionaryEntryType, which contains an object of type DictionaryEntryType
            dalContext.profileItems.Add(this._model.NodeId, profileItem);

            UpdateProfileItem(profileItem, dalContext);

            var update = (profileItem.ID ?? 0) == 0 || dalContext.UpdateExisting;
            var result = dalContext.UpsertAsync(profileItem, update).Result;
            profileItem.ID = result.Item1;
            bool created = result.Item2;

            if (created)
            {
                if (OnProfileItemCreated(profileItem, dalContext))
                {
                    var result2 = dalContext.UpsertAsync(profileItem, true).Result;
                }
            }
            return profileItem;
        }

        /// <summary>
        /// Called after a new profile item was created: intended to be used for creating additional artifacts like data type mappings
        /// </summary>
        /// <param name="profileItem"></param>
        /// <param name="dalContext"></param>
        protected virtual bool OnProfileItemCreated(ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            return false;
        }

        protected virtual void UpdateProfileItem(ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            foreach (var property in _model.Properties)
            {
                property.AddVariableToProfileModel(profileItem, dalContext);
            }

            foreach (var dataVariable in _model.DataVariables)
            {
                dataVariable.AddVariableToProfileModel(profileItem, dalContext);
            }
            foreach (var opcObject in this._model.Objects)
            {
                var objectProfile = opcObject.ImportProfileItem(dalContext);
                if (profileItem.Compositions == null)
                {
                    profileItem.Compositions = new List<ProfileTypeDefinitionRelatedModel>();
                }
                var composition = new ProfileTypeDefinitionRelatedModel
                {
                    ID = profileItem.ID,
                    ProfileTypeDefinition = profileItem,
                    Name = opcObject.DisplayName?.FirstOrDefault()?.Text,
                    BrowseName = opcObject.BrowseName,
                    Description = opcObject.Description?.FirstOrDefault()?.Text,
                    RelatedProfileTypeDefinitionId = objectProfile.ID,
                    RelatedProfileTypeDefinition = objectProfile,
                    RelatedIsRequired = ObjectModelImportProfile.GetModelingRuleForProfile(opcObject.ModelingRule),
                    RelatedModelingRule = opcObject.ModelingRule,
                    OpcNodeId = NodeModelUtils.GetNodeIdIdentifier(opcObject.NodeId),
                    //Namespace = opcObject.Namespace,
                    Profile = opcObject.CustomState as ProfileModel,
                };
                profileItem.Compositions.Add(composition);
            }
            foreach (var childRef in this._model.OtherChilden)
            {
                var child = childRef.Child;
                var objectProfile = child.ImportProfileItem(dalContext);
                if (profileItem.Compositions == null)
                {
                    profileItem.Compositions = new List<ProfileTypeDefinitionRelatedModel>();
                }
                var composition = new ProfileTypeDefinitionRelatedModel
                {
                    ID = profileItem.ID,
                    ProfileTypeDefinition = profileItem,
                    Name = child.DisplayName?.FirstOrDefault()?.Text,
                    BrowseName = child.BrowseName,
                    Description = child.Description?.FirstOrDefault()?.Text,
                    RelatedProfileTypeDefinitionId = objectProfile.ID,
                    RelatedProfileTypeDefinition = objectProfile,
                    RelatedIsRequired = ObjectModelImportProfile.GetModelingRuleForProfile((child as InstanceModelBase)?.ModelingRule),
                    RelatedModelingRule = (child as InstanceModelBase)?.ModelingRule,
                    OpcNodeId = NodeModelUtils.GetNodeIdIdentifier(child.NodeId),
                    //Namespace = opcObject.Namespace,
                    RelatedReferenceId = childRef.Reference,
                    Profile = child.CustomState as ProfileModel,
                };
                profileItem.Compositions.Add(composition);
            }
            foreach (var uaInterface in this._model.Interfaces)
            {
                if (profileItem.Interfaces == null)
                {
                    profileItem.Interfaces = new List<ProfileTypeDefinitionModel>();
                }
                var interfaceProfileItem = uaInterface.ImportProfileItem(dalContext);
                profileItem.Interfaces.Add(interfaceProfileItem);
            }
            foreach (var uaMethod in this._model.Methods)
            {
                if (profileItem.Compositions == null)
                {
                    profileItem.Compositions = new List<ProfileTypeDefinitionRelatedModel>();
                }
                var methodProfileItem = uaMethod.ImportProfileItem(dalContext);
                var composition = new ProfileTypeDefinitionRelatedModel
                {
                    ID = profileItem.ID,
                    ProfileTypeDefinition = profileItem,
                    Name = uaMethod.DisplayName?.FirstOrDefault()?.Text,
                    BrowseName = uaMethod.BrowseName,
                    Description = uaMethod.Description?.FirstOrDefault()?.Text,
                    RelatedProfileTypeDefinitionId = methodProfileItem.ID,
                    RelatedProfileTypeDefinition = methodProfileItem,
                    RelatedIsRequired = ObjectModelImportProfile.GetModelingRuleForProfile(uaMethod.ModelingRule),
                    RelatedModelingRule = uaMethod.ModelingRule,
                    OpcNodeId = NodeModelUtils.GetNodeIdIdentifier(uaMethod.NodeId),
                    //Namespace = opcObject.Namespace,
                    Profile = uaMethod.CustomState as ProfileModel,
                };
                profileItem.Compositions.Add(composition); // TODO Confirm that this results in correct nodeset on export
            }
            foreach (var uaEvent in this._model.Events)
            {
                if (profileItem.Compositions == null)
                {
                    profileItem.Compositions = new List<ProfileTypeDefinitionRelatedModel>();
                }
                var eventProfileItem = uaEvent.ImportProfileItem(dalContext);
                var composition = new ProfileTypeDefinitionRelatedModel
                {
                    ID = profileItem.ID,
                    ProfileTypeDefinition = profileItem,
                    Name = uaEvent.DisplayName?.FirstOrDefault()?.Text,
                    BrowseName = uaEvent.BrowseName,
                    Description = uaEvent.Description?.FirstOrDefault()?.Text,
                    RelatedProfileTypeDefinitionId = eventProfileItem.ID,
                    RelatedProfileTypeDefinition = eventProfileItem,
                    RelatedIsEvent = true,
                    //RelatedIsRequired = ObjectModelImportProfile.GetModelingRuleForProfile(uaEvent.ModelingRule),
                    //RelatedModelingRule = uaEvent.ModelingRule,
                    OpcNodeId = NodeModelUtils.GetNodeIdIdentifier(uaEvent.NodeId),
                    //Namespace = opcObject.Namespace,
                    Profile = uaEvent.CustomState as ProfileModel,
                };
                profileItem.Compositions.Add(composition); // TODO Confirm that this results in correct nodeset on export
            }
        }
    }

    public class InstanceModelImportProfile<TInstanceModel, TBaseTypeModel, TBaseTypeModelImportProfile> : NodeModelImportProfile<TInstanceModel>
        where TInstanceModel : InstanceModel<TBaseTypeModel>, new()
        where TBaseTypeModel : BaseTypeModel, new()
        where TBaseTypeModelImportProfile: NodeModelImportProfile<TBaseTypeModel>, new()
    {
        protected override void UpdateProfileItem(ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            base.UpdateProfileItem(profileItem, dalContext);

            if (_model.TypeDefinition != null)
            {
                var objectType = _model.TypeDefinition?.ImportProfileItem(dalContext);
                if (objectType == null)
                {
                    throw new Exception($"Undefined object type {_model.TypeDefinition.DisplayName?.FirstOrDefault()?.Text} {_model.TypeDefinition.NodeId}");
                }
                profileItem.Parent = dalContext.MapToModelProfileSimple(objectType);
            }
        }

        protected override bool OnProfileItemCreated(ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            bool bUpdated = base.OnProfileItemCreated(profileItem, dalContext);
            if (_model.Parent != null)
            {
                var instanceParent = _model.Parent.ImportProfileItem(dalContext);
                if (instanceParent != null)
                {
                    if (profileItem.InstanceParent != instanceParent)
                    {
                        profileItem.InstanceParent = instanceParent;
                        dalContext.Logger.LogTrace($"Using previously imported parent {_model.Parent} for {_model}.");
                        bUpdated = true;
                    }
                    else
                    {
                        dalContext.Logger.LogInformation($"Previously imported parent {_model.Parent} for {_model} already set.");
                    }
                }
                else
                {
                    dalContext.Logger.LogError($"Parent {_model.Parent} not yet imported for {_model}.");
                }
            }
            return bUpdated;
        }

        // TODO add more modeling rule flavors to the profile compositions / attributes
        public static bool? GetModelingRuleForProfile(string modelingRule)
        {
            if (string.IsNullOrEmpty(modelingRule))
            {
                return null;
            }
            return modelingRule.Contains("Optional") == false;
        }
    }

    public class ObjectModelImportProfile : InstanceModelImportProfile<ObjectModel, ObjectTypeModel, ObjectTypeModelImportProfile>
    {
        protected override void UpdateProfileItem(ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            profileItem.TypeId = (int)ProfileItemTypeEnum.Object;
            base.UpdateProfileItem(profileItem, dalContext);
            if (profileItem.TypeId != (int)ProfileItemTypeEnum.Object)
            {
                throw new Exception();
            }
        }
    }

    public class BaseTypeModelImportProfile<TBaseTypeModel> : NodeModelImportProfile<TBaseTypeModel> where TBaseTypeModel : BaseTypeModel, new()
    {
        protected override void UpdateProfileItem(ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            if (_model.SuperType != null)
            {
                var superTypeProfile = _model.SuperType.ImportProfileItem(dalContext);
                profileItem.Parent = dalContext.MapToModelProfileSimple(superTypeProfile);
            }
            profileItem.IsAbstract = _model.IsAbstract;
            base.UpdateProfileItem(profileItem, dalContext);
        }

        public LookupDataTypeModel GetAttributeDataType(ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            var attributeDataType = dalContext.GetDataType(_model.DisplayName?.FirstOrDefault()?.Text);
            if (attributeDataType == null)
            {
                //TODO: SC - Question - MH - Please review this.
                //At this point, can we add the custom data type record (the profile item and the data type record)
                //Note we first do need to insert the custom data type as a profile item.
                //Possibly wrap ImportProfileItemAsync and a ImportCustomDataTypeAsync into one call.
                // MH: DataType.ImportProfileItemAsync now creates the Data Type lookup entry
                var dataTypeProfile = this.ImportProfileItem(dalContext);
                if (dataTypeProfile == null)
                {
                    throw new Exception($"Undefined data type {_model.DisplayName?.FirstOrDefault()?.Text} {_model.NodeId} in profile {profileItem.Name} ({profileItem.ID})");
                }

                attributeDataType = dalContext.GetCustomDataTypeAsync(dataTypeProfile).Result;
                if (attributeDataType == null)
                {
                    // TODO expand LookupDataTypeModel with object reference
                    attributeDataType = new LookupDataTypeModel { Name = dataTypeProfile.Name, Code = "custom", CustomType = dataTypeProfile };
                }
            }
            return attributeDataType;
        }
    }

    public class ObjectTypeModelImportProfile<TTypeModel> : BaseTypeModelImportProfile<TTypeModel> where TTypeModel : ObjectTypeModel, new()
    {
    }

    public class ObjectTypeModelImportProfile : ObjectTypeModelImportProfile<ObjectTypeModel>
    {
    }

    public class InterfaceModelImportProfile : ObjectTypeModelImportProfile<InterfaceModel>
    {
        protected override void UpdateProfileItem(ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            profileItem.TypeId = (int)ProfileItemTypeEnum.Interface;
            base.UpdateProfileItem(profileItem, dalContext);
            if (profileItem.TypeId != (int)ProfileItemTypeEnum.Interface)
            {
                throw new Exception();
            }
        }
    }

    public class VariableModelImportProfile<TVariableModel> : InstanceModelImportProfile<TVariableModel, VariableTypeModel, VariableTypeModelImportProfile>
        where TVariableModel : VariableModel, new()
    {
        public void AddVariableToProfileModel(ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            string description = _model.Description?.FirstOrDefault()?.Text;
            var typeDefinitionModel = _model.TypeDefinition?.ImportProfileItem(dalContext);

            // TODO Capture the DataVariable TypeDefinition somewhere in the ProfileItem
            var attributeDataType = _model.DataType.GetAttributeDataType(profileItem, dalContext);

            if (attributeDataType != null)
            {
                int attributeTypeId = 0;
                if (this._model is PropertyModel)
                {
                    attributeTypeId = (int)AttributeTypeIdEnum.Property;
                }
                else if (this._model is DataVariableModel)
                {
                    attributeTypeId = (int)AttributeTypeIdEnum.DataVariable;
                }
                else
                {
                    throw new Exception($"Unexpected child item {_model?.DisplayName} of type {this.GetType().Name} on item {profileItem.Name} ({profileItem.ID})");
                }


                string dataVariableNodeIds = null;
                if (_model.DataVariables?.Any() == true && _model?.TypeDefinition?.DataVariables?.Any() == true)
                {
                    var map = GetDataVariableNodeIds(_model.DataVariables, _model.TypeDefinition.DataVariables);
                    dataVariableNodeIds = DataVariableNodeIdMap.GetMapAsString(map);
                }
                var attribute = new ProfileAttributeModel
                {
                    IsActive = true,
                    Name = _model.DisplayName?.FirstOrDefault()?.Text,
                    SymbolicName = _model.SymbolicName,
                    BrowseName = _model.BrowseName,
                    Namespace = NodeModelUtils.GetNamespaceFromNodeId(_model.NodeId),
                    IsRequired = ObjectModelImportProfile.GetModelingRuleForProfile(_model.ModelingRule),
                    ModelingRule = _model.ModelingRule,
                    IsArray = (_model.ValueRank ?? -1) != -1,
                    ValueRank = _model.ValueRank,
                    ArrayDimensions = _model.ArrayDimensions,
                    Description = description,
                    AttributeType = new LookupItemModel { ID = attributeTypeId },
                    DataType = attributeDataType,
                    DataVariableNodeIds = dataVariableNodeIds,
                    TypeDefinitionId = profileItem.ID,
                    TypeDefinition = profileItem,
                    VariableTypeDefinitionId = typeDefinitionModel?.ID,
                    VariableTypeDefinition = typeDefinitionModel,
                    OpcNodeId = NodeModelUtils.GetNodeIdIdentifier(_model.NodeId),
                    AdditionalData = _model.Value,
                    AccessLevel = _model.AccessLevel,
                    UserAccessLevel = _model.UserAccessLevel,
                    AccessRestrictions = _model.AccessRestrictions,
                    WriteMask = _model.WriteMask,
                    UserWriteMask = _model.UserWriteMask,
               };

                var euInfo = NodeModelOpcExtensions.GetEUInformation(_model.EngineeringUnits);
                if (euInfo != null)
                {
                    var engUnit = new EngineeringUnitModel
                    {
                        DisplayName = euInfo.DisplayName.Text,
                        Description = euInfo.Description.Text,
                        UnitId = euInfo.UnitId,
                        NamespaceUri = euInfo.NamespaceUri,
                    };
                    engUnit = dalContext.GetOrCreateEngineeringUnitAsync(engUnit);
                    attribute.EngUnit = engUnit;
                }
                attribute.EngUnitOpcNodeId = _model.EngUnitNodeId;
                attribute.MinValue = (decimal?)_model.MinValue;
                attribute.MaxValue = (decimal?)_model.MaxValue;
                attribute.InstrumentMinValue = (decimal?)_model.InstrumentMinValue;
                attribute.InstrumentMaxValue = (decimal?)_model.InstrumentMaxValue;

                if (profileItem.Attributes == null)
                {
                    profileItem.Attributes = new List<ProfileAttributeModel>();
                }
                profileItem.Attributes.Add(attribute);
            }
            else
            {
                throw new Exception($"Data type {_model.DataType} not resolved.");
            }
        }

        private static DataVariableNodeIdMap GetDataVariableNodeIds(List<DataVariableModel> dataVariables, List<DataVariableModel> typeDataVariables)
        {
            if (dataVariables?.Any() != true || typeDataVariables?.Any() != true)
                return null;
            var dataVariableNodeIdMap = new DataVariableNodeIdMap();
            foreach (var typeDataVariable in typeDataVariables)
            {
                var dataVariable = dataVariables.FirstOrDefault(dv => dv.BrowseName.EndsWith(typeDataVariable.BrowseName));
                var map = new DataVariableNodeIdMapEntry
                {
                    NodeId = dataVariable.NodeId,
                    Map = GetDataVariableNodeIds(dataVariable.DataVariables, typeDataVariable.DataVariables)
                };
                dataVariableNodeIdMap.DataVariableNodeIdsByBrowseName.Add(dataVariable.BrowseName, map);
            }
            return dataVariableNodeIdMap;
        }
    }

    public class DataVariableNodeIdMapEntry
    {
        public string NodeId { get; set; }
        public DataVariableNodeIdMap Map { get; set; }
    }
    public class DataVariableNodeIdMap
    {
        public Dictionary<string, DataVariableNodeIdMapEntry> DataVariableNodeIdsByBrowseName { get; set; } = new Dictionary<string, DataVariableNodeIdMapEntry>();

        public static DataVariableNodeIdMap GetMap(string dataVariableNodeIdsByBrowseNameJson)
        {
            var map = JsonSerializer.Deserialize<DataVariableNodeIdMap>(dataVariableNodeIdsByBrowseNameJson);
            return map;
        }
        public static string GetMapAsString(DataVariableNodeIdMap dataVariableNodeIdsByBrowseName)
        {
            var map = JsonSerializer.Serialize<DataVariableNodeIdMap>(dataVariableNodeIdsByBrowseName);
            return map;
        }
    }


    public class DataVariableModelImportProfile : VariableModelImportProfile<DataVariableModel>
    {
    }

    public class PropertyModelImportProfile : VariableModelImportProfile<PropertyModel>
    {
    }

    public class MethodModelImportProfile : NodeModelImportProfile<MethodModel>
    {
        protected override void UpdateProfileItem(ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            profileItem.TypeId = (int)ProfileItemTypeEnum.Method;
            base.UpdateProfileItem(profileItem, dalContext);
            if (profileItem.TypeId != (int)ProfileItemTypeEnum.Method)
            {
                throw new Exception();
            }
        }
    }

    public class VariableTypeModelImportProfile : BaseTypeModelImportProfile<VariableTypeModel>
    {
        protected override void UpdateProfileItem(ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            profileItem.TypeId = (int)ProfileItemTypeEnum.VariableType;
            base.UpdateProfileItem(profileItem, dalContext);
            if (profileItem.TypeId != (int)ProfileItemTypeEnum.VariableType)
            {
                throw new Exception();
            }
        }

    }
    public class DataTypeModelImportProfile : BaseTypeModelImportProfile<DataTypeModel>
    {
        protected override void UpdateProfileItem(ProfileTypeDefinitionModel profileItem, IDALContext dalContext )
        {
            profileItem.TypeId = (int)ProfileItemTypeEnum.CustomDataType;
            base.UpdateProfileItem(profileItem, dalContext);
            if (profileItem.TypeId != (int)ProfileItemTypeEnum.CustomDataType)
            {
                throw new Exception();
            }
            var attributeNamespace = NodeModelUtils.GetNamespaceFromNodeId(_model.NodeId);
            if (_model.StructureFields?.Any() == true || _model.HasBaseType("nsu=http://opcfoundation.org/UA/;i=22"))
            {
                profileItem.TypeId = (int) ProfileItemTypeEnum.Structure;

                if (profileItem.Attributes == null)
                {
                    profileItem.Attributes = new List<ProfileAttributeModel>();
                }
                foreach (var field in _model.StructureFields ?? new List<DataTypeModel.StructureField>())
                {
                    var fieldDataType = field.DataType;
                    if (fieldDataType == null)
                    {
                        throw new Exception($"Unable to resolve data type {field.DataType?.DisplayName}");
                    }
                    var attributeDataType = fieldDataType.GetAttributeDataType(profileItem, dalContext);
                    if (attributeDataType == null)
                    {
                        throw new Exception($"{fieldDataType} not resolved");
                    }
                    var attribute = new ProfileAttributeModel
                    {
                        IsActive = true,
                        Name = field.Name,
                        BrowseName = $"{attributeNamespace};{field.Name}",
                        //No SymbolicName for structure fields
                        Namespace = attributeNamespace,
                        IsRequired = !field.IsOptional,
                        Description = field.Description?.FirstOrDefault()?.Text,
                        AttributeType = new LookupItemModel { ID = (int)AttributeTypeIdEnum.StructureField },
                        DataType = attributeDataType,
                        OpcNodeId = NodeModelUtils.GetNodeIdIdentifier(_model.NodeId),
                    };
                    profileItem.Attributes.Add(attribute);
                }
            }
            if (_model.EnumFields?.Any() == true)
            {
                profileItem.TypeId = (int) ProfileItemTypeEnum.Enumeration;
                if (profileItem.Attributes == null)
                {
                    profileItem.Attributes = new List<ProfileAttributeModel>();
                }
                foreach (var field in _model.EnumFields)
                {
                    var int64DataType = dalContext.GetDataType("Int64");
                    if (int64DataType == null/* || int64DataType.ID == 0*/)
                    {
                        throw new Exception($"Unable to resolve Int64 data type.");
                    }
                    var attribute = new ProfileAttributeModel
                    {
                        IsActive = true,
                        Name = field.Name,
                        BrowseName = $"{attributeNamespace};{field.Name}",
                        // No SymbolicName for enum fields
                        DisplayName = field.DisplayName?.FirstOrDefault()?.Text,
                        Description = field.Description?.FirstOrDefault()?.Text,
                        AttributeType = new LookupItemModel { ID = (int)AttributeTypeIdEnum.EnumField },
                        DataType = int64DataType,
                        EnumValue = field.Value,
                        Namespace = attributeNamespace,
                    };
                    profileItem.Attributes.Add(attribute);
                }
            }
        }

        protected override bool OnProfileItemCreated(ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            var bUpdated = base.OnProfileItemCreated(profileItem, dalContext);


            var isNumeric = _model.HasBaseType("nsu=http://opcfoundation.org/UA/;i=26");

            // Create data type table entry
            var dataTypeLookup = new LookupDataTypeModel
            {
                ID = null,
                Name = profileItem.Name,
                Code = $"{profileItem.Profile.Namespace}.{profileItem.OpcNodeId}", //"custom",
                CustomTypeId = profileItem.ID,
                CustomType = profileItem,
                // TODO verify if OPA UC allows enginering units on non-numeric types
                UseEngUnit = isNumeric,
                UseMinMax = isNumeric,
                IsNumeric = isNumeric,
                //For standard (UA, UA/DI), AuthorId is null.
                //For standard imported by owner (UA/Robotics), AuthorId has value and we use this to keep the separation by owner.
                //For custom (myNodeset), AuthorId has value and we use this to keep the separation by owner.
                OwnerId = profileItem.AuthorId
            };
            //if (GetBuiltinDataTypeNodeId(dataTypeLookup) == null)
            var dataTypeId = dalContext.CreateCustomDataTypeAsync(dataTypeLookup).Result;
            return bUpdated;
        }

        //internal static NodeId GetBuiltinDataTypeNodeId(LookupDataTypeModel profileEditorDataType)
        //{
        //    NodeId typeNodeId = null;
        //    var typeNodeIdField = typeof(DataTypeIds).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Where(f => string.Equals(f.Name, profileEditorDataType.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        //    if (typeNodeIdField == null)
        //    {
        //        var code = profileEditorDataType.Code;
        //        switch (code)
        //        {
        //            case "long":
        //                code = "Int64";
        //                break;
        //        }
        //        typeNodeIdField = typeof(DataTypeIds).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Where(f => string.Equals(f.Name, code, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        //    }
        //    if (typeNodeIdField != null)
        //    {
        //        //typeNodeId = typeNodeIdField.GetValue(null) as NodeId;
        //    }
        //    else
        //    {

        //    }
        //    return typeNodeId;
        //}

        //internal static DataTypeModel GetBuiltinDataType(IOpcUaImporter opcContext, LookupDataTypeModel profileEditorDataType)
        //{
        //    var typeNodeId = GetBuiltinDataTypeNodeId(profileEditorDataType);
        //    if (typeNodeId != null)
        //    {
        //        var typeNodeIdString = opcContext.GetNodeIdWithUri(typeNodeId, out _);
        //        var dataTypeModel = opcContext.GetModelForNode(typeNodeIdString);
        //        return dataTypeModel as DataTypeModel;
        //    }
        //    return null;
        //}
    }
    public static class NodeModelProfileExtensions
    {
        public static ProfileTypeDefinitionModel ImportProfileItem(this NodeModel model, IDALContext dalContext)
        {
            if (model is InterfaceModel uaInterface)
            {
                return new InterfaceModelImportProfile { _model = uaInterface }.ImportProfileItem(dalContext);
            }
            else if (model is ObjectTypeModel objectType)
            {
                return new ObjectTypeModelImportProfile { _model = objectType }.ImportProfileItem(dalContext);
            }
            else if (model is VariableTypeModel variableType)
            {
                return new VariableTypeModelImportProfile { _model = variableType }.ImportProfileItem(dalContext);
            }
            else if (model is DataTypeModel dataType)
            {
                return new DataTypeModelImportProfile { _model = dataType }.ImportProfileItem(dalContext);
            }
            else if (model is DataVariableModel dataVariable)
            {
                return new DataVariableModelImportProfile { _model = dataVariable }.ImportProfileItem(dalContext);
            }
            else if (model is PropertyModel property)
            {
                return new PropertyModelImportProfile { _model = property }.ImportProfileItem(dalContext);
            }
            else if (model is VariableModel variable)
            {
                return new VariableModelImportProfile<VariableModel> { _model = variable }.ImportProfileItem(dalContext);
            }
            else if (model is ObjectModel uaObject)
            {
                return new ObjectModelImportProfile { _model = uaObject }.ImportProfileItem(dalContext);
            }
            else if (model is MethodModel uaMethod)
            {
                return new MethodModelImportProfile { _model = uaMethod }.ImportProfileItem(dalContext);
            }
            throw new Exception($"Unexpected node model {model.GetType()}");
            //var nodeModelFactory = new NodeModelImportProfile<NodeModel> { _model = model };
            //return nodeModelFactory.ImportProfileItem(dalContext);
        }

        public static void AddVariableToProfileModel(this PropertyModel model, ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            var nodeModelFactory = new PropertyModelImportProfile { _model = model };
            nodeModelFactory.AddVariableToProfileModel(profileItem, dalContext);
        }
        public static void AddVariableToProfileModel(this DataVariableModel model, ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            var nodeModelFactory = new DataVariableModelImportProfile { _model = model };
            nodeModelFactory.AddVariableToProfileModel(profileItem, dalContext);
        }
        public static void AddVariableToProfileModel(this VariableModel model, ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            if (model is PropertyModel property)
            {
                property.AddVariableToProfileModel(profileItem, dalContext);
            }
            else if (model is DataVariableModel variable)
            {
                variable.AddVariableToProfileModel(profileItem, dalContext);
            }
            else
            {
                throw new Exception($"Unexpected variable type {model.GetType()}");
            }
        }
        public static LookupDataTypeModel GetAttributeDataType(this BaseTypeModel model, ProfileTypeDefinitionModel profileItem, IDALContext dalContext)
        {
            if (model is ObjectTypeModel objectType)
            {
                return new ObjectTypeModelImportProfile { _model = objectType }.GetAttributeDataType(profileItem, dalContext);
            }
            else if (model is VariableTypeModel variableType)
            {
                return new VariableTypeModelImportProfile { _model = variableType }.GetAttributeDataType(profileItem, dalContext);
            }
            else if (model is DataTypeModel dataType)
            {
                return new DataTypeModelImportProfile { _model = dataType }.GetAttributeDataType(profileItem, dalContext);
            }
            throw new Exception($"Unexpected variable type {model.GetType()}");
            //var nodeModelFactory = new BaseTypeModelImportProfile<BaseTypeModel> { _model = model };
            //return nodeModelFactory.GetAttributeDataType(profileItem, dalContext);
        }

    }

}