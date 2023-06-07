using Opc.Ua;
using ua = Opc.Ua;
using uaExport = Opc.Ua.Export;

using System;
using System.Collections.Generic;
using System.Linq;

using Opc.Ua.Export;
using CESMII.OpcUa.NodeSetModel.Factory.Smip;
using SMIP.JsonIO.Model;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace CESMII.OpcUa.NodeSetModel.Export.Smip
{
    //public class NodeModelExportSmip : NodeModelExportSmip<NodeModel>
    //{

    //}
    public class NodeModelExportSmip<TTiqBase, TNodeModel>
        where TTiqBase : SmipNode, new()
        where TNodeModel : NodeModel, new()
    {

        public TNodeModel _model;
        //public static SmipNodeInfo ExportNode(NodeModel model, SmipLibrary library)
        //{
        //    if (model is InterfaceModel uaInterface)
        //    {
        //        return new InterfaceModelExportSmip { _model = uaInterface }.ExportNode(library);
        //    }
        //    else if (model is ObjectTypeModel objectType)
        //    {
        //        return new ObjectTypeModelExportSmip { _model = objectType, }.ExportNode(library);
        //    }
        //    else if (model is VariableTypeModel variableType)
        //    {
        //        return new VariableTypeModelExportSmip { _model = variableType, }.ExportNode(library);
        //    }
        //    else if (model is DataTypeModel dataType)
        //    {
        //        return new DataTypeModelExportSmip { _model = dataType, }.ExportNode(library);
        //    }
        //    else if (model is DataVariableModel dataVariable)
        //    {
        //        return new DataVariableModelExportSmip { _model = dataVariable, }.ExportNode(library);
        //    }
        //    else if (model is PropertyModel property)
        //    {
        //        return new PropertyModelExportSmip { _model = property, }.ExportNode(library);
        //    }
        //    else if (model is ObjectModel uaObject)
        //    {
        //        return new ObjectModelExportSmip { _model = uaObject, }.ExportNode(library);
        //    }
        //    else if (model is MethodModel uaMethod)
        //    {
        //        return new MethodModelExportSmip { _model = uaMethod, }.ExportNode(library);
        //    }
        //    else if (model is ReferenceTypeModel referenceType)
        //    {
        //        return new ReferenceTypeModelExportSmip { _model = referenceType, }.ExportNode(library);
        //    }
        //    throw new Exception($"Unexpected node model {model.GetType()}");
        //}

        public virtual SmipNodeInfo<TTiqBase> ExportNode(SmipTypeSystem library)
        {
            var relativeName = GetRelativeName(_model);
            var fqn = new List<string> { _model.Namespace?.ToLowerInvariant(), relativeName };

            var et = library.Types?.FirstOrDefault(et => et.Fqn.SequenceEqual(fqn));
            if (et != null)
            {
                if (!(et is TTiqBase))
                {
                    throw new Exception($"Duplicate FQN for different types? {fqn} found in EquipmentTypes for type {typeof(TTiqBase)}");
                }
                return new SmipNodeInfo<TTiqBase>
                {
                    tiqModel = et as TTiqBase,
                    Found = true,
                };
            }
            //var at = library.AttributeTypes?.FirstOrDefault(at => at.Fqn.SequenceEqual(fqn));
            //if (at != null)
            //{
            //    if (!(at is TTiqBase))
            //    {
            //        throw new Exception($"Duplicate FQN for different types? {fqn} found in AttributeTypes for type {typeof(TTiqBase)}");
            //    }
            //    return new SmipNodeInfo<TTiqBase>
            //    {
            //        tiqModel = at as TTiqBase,
            //        Found = true,
            //    };
            //}
            var enumType = library.EnumerationTypes?.FirstOrDefault(at => at.Fqn.SequenceEqual(fqn));
            if (enumType != null)
            {
                if (!(enumType is TTiqBase))
                {
                    throw new Exception($"Duplicate FQN for different types? {fqn} found in EnumerationTypes for type {typeof(TTiqBase)}");
                }
                return new SmipNodeInfo<TTiqBase>
                {
                    tiqModel = enumType as TTiqBase,
                    Found = true,
                };
            }


            var tiqBase = new TTiqBase
            {
                RelativeName = relativeName,
                Fqn = fqn,
                Description = _model.Description?.FirstOrDefault()?.Text,
                DisplayName = _model.DisplayName?.FirstOrDefault()?.Text,
                Document = null, // TODO What is this used for?
            };

            if (!library.Libraries.Any(l => l.RelativeName == fqn[0]))
            {
                var libMeta = new SmipLibrary // LibraryMeta
                {
                    Fqn = new List<string> { fqn[0] },
                    Version = Get3PartVersion(_model.NodeSet.Version),
                    RelativeName = fqn[0],
                };
                library.Libraries.Add(libMeta);
            }

            return new SmipNodeInfo<TTiqBase>
            {
                tiqModel = tiqBase
            };
        }

        public static string GetRelativeName(NodeModel model)
        {
            //Add the nodeid to make it unique, as different types seem to use the same browsename/displayname
            var browseName = $"{model.DisplayName?.FirstOrDefault()?.Text}_{model.NodeId}";
            return EscapeSpecialCharacters(browseName);
        }

        public static string EscapeSpecialCharacters(string name)
        {
            return name.Replace(":", "_").Replace(".", "_").Replace("//", "_").Replace("/", "_").Replace(";", "_").Replace("=", "_").ToLowerInvariant();
        }

        public static string Get3PartVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return "1.0.0"; // TODO is version really required? Is there a better default?
            }
            // TODO validate that the parts are numerics etc.
            var parts = version.Split(".");
            var partsNumber = parts.Select(p =>
            {
                if (int.TryParse(p, out var pAsInt))
                {
                    return pAsInt;
                }
                return 0;
            }).ToArray();
            if (partsNumber.Length >= 3)
            {
                return string.Join(".", partsNumber.Take(3).Select(pn => pn.ToString(CultureInfo.InvariantCulture)));
            }
            if (parts.Length == 2)
            {
                return $"{string.Join(".", partsNumber.Select(pn => pn.ToString(CultureInfo.InvariantCulture)))}.0";
            }
            return $"{partsNumber[0].ToString(CultureInfo.InvariantCulture)}.0.0";
        }

        protected string GetNodeIdForExport(string nodeId, NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            if (nodeId == null) return null;
            var expandedNodeId = ExpandedNodeId.Parse(nodeId, namespaces);
            //_nodeIdsUsed?.Add(expandedNodeId.ToString());
            if (aliases.TryGetValue(expandedNodeId.ToString(), out var alias))
            {
                return alias;
            }
            return ExpandedNodeId.ToNodeId(expandedNodeId, namespaces).ToString();
        }

        protected string GetBrowseNameForExport(NamespaceTable namespaces)
        {
            return GetQualifiedNameForExport(_model.BrowseName, _model.Namespace, _model.DisplayName, namespaces);
        }

        protected static string GetQualifiedNameForExport(string qualifiedName, string fallbackNamespace, List<NodeModel.LocalizedText> displayName, NamespaceTable namespaces)
        {
            string qualifiedNameForExport;
            if (qualifiedName != null)
            {
                var parts = qualifiedName.Split(new[] { ';' }, 2);
                if (parts.Length >= 2)
                {
                    qualifiedNameForExport = new QualifiedName(parts[1], namespaces.GetIndexOrAppend(parts[0])).ToString();
                }
                else if (parts.Length == 1)
                {
                    qualifiedNameForExport = parts[0];
                }
                else
                {
                    qualifiedNameForExport = "";
                }
            }
            else
            {
                qualifiedNameForExport = new QualifiedName(displayName?.FirstOrDefault()?.Text, namespaces.GetIndexOrAppend(fallbackNamespace)).ToString();
            }

            return qualifiedNameForExport;
        }

        protected static List<string> GetSmipMeasurementUnit(VariableModel.EngineeringUnitInfo engineeringUnit)
        {
            // TODO implement measurement unit mapping
            return null;
        }

        protected static List<string> GetSmipAttributeType(VariableTypeModel typeDefinition)
        {
            // TODO Generate Smip model for variable type 
            return new List<string> { typeDefinition.Namespace?.ToLowerInvariant(), typeDefinition.BrowseName?.ToLowerInvariant() };

        }


        protected static bool IsRequired(string modelingRule)
        {
            return modelingRule?.Contains("Mandatory") == true;
        }


        public override string ToString()
        {
            return _model?.ToString();
        }
    }

    public class SmipNodeInfo<TTiqBase> where TTiqBase : SmipNode
    {
        public TTiqBase tiqModel { get; set; }
        public string DataTypeName { get; set; }
        public bool Found { get; set; }
    }

    public abstract class InstanceModelExportSmip<TTiqBase, TInstanceModel, TBaseTypeModel> : NodeModelExportSmip<TTiqBase, TInstanceModel>
        where TTiqBase : SmipNode, new()
        where TInstanceModel : InstanceModel<TBaseTypeModel>, new()
        where TBaseTypeModel : NodeModel, new()
    {

        protected abstract (bool IsChild, NodeId ReferenceTypeId) ReferenceFromParent(NodeModel parent);

        //public override (T ExportedNode, List<UANode> AdditionalNodes) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        //{
        //    var result = base.GetUANode<T>(namespaces, aliases);
        //    var instance = result.ExportedNode as UAInstance;
        //    if (instance == null)
        //    {
        //        throw new Exception("Internal error: wrong generic type requested");
        //    }
        //    var references = instance.References?.ToList() ?? new List<Reference>();

        //    if (!string.IsNullOrEmpty(_model.Parent?.NodeId))
        //    {
        //        instance.ParentNodeId = GetNodeIdForExport(_model.Parent.NodeId, namespaces, aliases);
        //    }

        //    string typeDefinitionNodeIdForExport;
        //    if (_model.TypeDefinition != null)
        //    {
        //        namespaces.GetIndexOrAppend(_model.TypeDefinition.Namespace);
        //        typeDefinitionNodeIdForExport = GetNodeIdForExport(_model.TypeDefinition.NodeId, namespaces, aliases);
        //    }
        //    else
        //    {
        //        NodeId typeDefinitionNodeId = null;
        //        if (_model is PropertyModel)
        //        {
        //            typeDefinitionNodeId = VariableTypeIds.PropertyType;
        //        }
        //        else if (_model is DataVariableModel)
        //        {
        //            typeDefinitionNodeId = VariableTypeIds.BaseDataVariableType;
        //        }
        //        else if (_model is VariableModel)
        //        {
        //            typeDefinitionNodeId = VariableTypeIds.BaseVariableType;
        //        }
        //        else if (_model is ObjectModel)
        //        {
        //            typeDefinitionNodeId = ObjectTypeIds.BaseObjectType;
        //        }

        //        typeDefinitionNodeIdForExport = GetNodeIdForExport(typeDefinitionNodeId?.ToString(), namespaces, aliases);
        //    }
        //    if (typeDefinitionNodeIdForExport != null && !(_model.TypeDefinition is MethodModel))
        //    {
        //        var reference = new Reference
        //        {
        //            ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasTypeDefinition.ToString(), namespaces, aliases),
        //            Value = typeDefinitionNodeIdForExport,
        //        };
        //        references.Add(reference);
        //    }

        //    AddModelingRuleReference(_model.ModelingRule, references, namespaces, aliases);

        //    if (references.Any())
        //    {
        //        instance.References = references.Distinct(new ReferenceComparer()).ToArray();
        //    }

        //    return (instance as T, result.AdditionalNodes);
        //}

        protected List<Reference> AddModelingRuleReference(string modelingRule, List<Reference> references, NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            if (modelingRule != null)
            {
                var modelingRuleId = modelingRule switch
                {
                    "Optional" => ObjectIds.ModellingRule_Optional,
                    "Mandatory" => ObjectIds.ModellingRule_Mandatory,
                    "MandatoryPlaceholder" => ObjectIds.ModellingRule_MandatoryPlaceholder,
                    "OptionalPlaceholder" => ObjectIds.ModellingRule_OptionalPlaceholder,
                    "ExposesItsArray" => ObjectIds.ModellingRule_ExposesItsArray,
                    _ => null,
                };
                if (modelingRuleId != null)
                {
                    references.Add(new Reference
                    {
                        ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasModellingRule.ToString(), namespaces, aliases),
                        Value = GetNodeIdForExport(modelingRuleId.ToString(), namespaces, aliases),
                    });
                }
            }
            return references;
        }

        protected void AddOtherReferences(List<Reference> references, string parentNodeId, NodeId referenceTypeId, bool bIsChild, NamespaceTable namespaces, Dictionary<string, string> aliases)
        {
            if (!string.IsNullOrEmpty(_model.Parent?.NodeId))
            {
                bool bAdded = false;
                foreach (var referencingNode in _model.Parent.OtherReferencedNodes.Where(cr => cr.Node == _model))
                {
                    var referenceType = GetNodeIdForExport(referencingNode.ReferenceType?.NodeId, namespaces, aliases);
                    if (!references.Any(r => r.IsForward == false && r.Value == parentNodeId && r.ReferenceType != referenceType))
                    {
                        references.Add(new Reference { IsForward = false, ReferenceType = referenceType, Value = parentNodeId });
                    }
                    else
                    {
                        // TODO ensure we pick the most derived reference type
                    }
                    bAdded = true;
                }
                if (bIsChild || !bAdded)//_model.Parent.Objects.Contains(_model))
                {
                    var referenceType = GetNodeIdForExport(referenceTypeId.ToString(), namespaces, aliases);
                    if (!references.Any(r => r.IsForward == false && r.Value == parentNodeId && r.ReferenceType != referenceType))
                    {
                        references.Add(new Reference { IsForward = false, ReferenceType = referenceType, Value = parentNodeId });
                    }
                    else
                    {
                        // TODO ensure we pick the most derived reference type
                    }
                }
            }
        }



    }

    public class ObjectModelExportSmip : InstanceModelExportSmip<SmipTypeComposition, ObjectModel, ObjectTypeModel>
    {
        public override SmipNodeInfo<SmipTypeComposition> ExportNode(SmipTypeSystem library)
        {
            var childEquipmentTypeInfo = base.ExportNode(library);
            if (childEquipmentTypeInfo.Found)
            {
                return childEquipmentTypeInfo;
            }
            var childTypeDefinitionInfo = new ObjectTypeModelExportSmip { _model = _model.TypeDefinition }.ExportNode(library);
            childEquipmentTypeInfo.tiqModel.ChildTypeFqn = childTypeDefinitionInfo.tiqModel?.Fqn.ToArray();
            childEquipmentTypeInfo.tiqModel.IsRequired = IsRequired(_model.ModellingRule);
            // document
            // max_number
            // min_number

            // TODO Data Variables, Properties
            return childEquipmentTypeInfo;
        }
        //public override (T ExportedNode, List<UANode> AdditionalNodes) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        //{
        //    var result = base.GetUANode<UAObject>(namespaces, aliases);
        //    var uaObject = result.ExportedNode;

        //    var references = uaObject.References?.ToList() ?? new List<Reference>();

        //    if (uaObject.ParentNodeId != null)
        //    {
        //        AddOtherReferences(references, uaObject.ParentNodeId, ReferenceTypeIds.HasComponent, _model.Parent.Objects.Contains(_model), namespaces, aliases);
        //    }
        //    if (references.Any())
        //    {
        //        uaObject.References = references.Distinct(new ReferenceComparer()).ToArray();
        //    }

        //    return (uaObject as T, result.AdditionalNodes);
        //}

        protected override (bool IsChild, NodeId ReferenceTypeId) ReferenceFromParent(NodeModel parent)
        {
            return (parent.Objects.Contains(_model), ReferenceTypeIds.HasComponent);
        }
    }

    public class BaseTypeModelExportSmip<TTiqBase, TBaseTypeModel> : NodeModelExportSmip<TTiqBase, TBaseTypeModel>
        where TTiqBase : SmipNode, new()
        where TBaseTypeModel : BaseTypeModel, new()
    {
        //public override (T ExportedNode, List<UANode> AdditionalNodes) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        //{
        //    var result = base.GetUANode<T>(namespaces, aliases);
        //    var objectType = result.ExportedNode;
        //    foreach (var subType in this._model.SubTypes)
        //    {
        //        namespaces.GetIndexOrAppend(subType.Namespace);
        //    }
        //    if (_model.SuperType != null)
        //    {
        //        namespaces.GetIndexOrAppend(_model.SuperType.Namespace);
        //        var superTypeReference = new Reference
        //        {
        //            ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasSubtype.ToString(), namespaces, aliases),
        //            IsForward = false,
        //            Value = GetNodeIdForExport(_model.SuperType.NodeId, namespaces, aliases),
        //        };
        //        if (objectType.References == null)
        //        {
        //            objectType.References = new Reference[] { superTypeReference };
        //        }
        //        else
        //        {
        //            var referenceList = new List<Reference>(objectType.References);
        //            referenceList.Add(superTypeReference);
        //            objectType.References = referenceList.ToArray();
        //        }
        //    }
        //    if (objectType is UAType uaType)
        //    {
        //        uaType.IsAbstract = _model.IsAbstract;
        //    }
        //    else
        //    {
        //        throw new Exception("Must be UAType or derived");
        //    }
        //    return (objectType, result.AdditionalNodes);
        //}
    }

    public class ObjectTypeModelExportSmip<TTypeModel> : BaseTypeModelExportSmip<SmipType, TTypeModel> where TTypeModel : BaseTypeModel, new()
    {
        public override SmipNodeInfo<SmipType> ExportNode(SmipTypeSystem library)
        {
            if (library.Types == null)
            {
                library.Types = new List<SmipType>();
            }
            if (library.EnumerationTypes == null)
            {
                library.EnumerationTypes = new List<SmipEnumerationType>();
            }

            if (_model == null || _model.Namespace?.ToLowerInvariant() != library.Meta.ExportLibraryFqn?.FirstOrDefault())
            {
                // TODO short circuit more types to built-in Smip types
                if (_model == null || _model.NodeId == "nsu=http://opcfoundation.org/UA/;i=58") // BaseObjectType
                {
                    var baseTypeEquipmentTypeInfo = new SmipNodeInfo<SmipType>
                    {
                        tiqModel = new SmipType
                        {
                            Fqn = new List<string> { "thinkiq_base_library", "object" },
                        },
                    };
                    return baseTypeEquipmentTypeInfo;
                }
                // Assume (for now) that the other library has already been generated/imported
                //return equipmentTypeInfo;
            }
            var equipmentTypeInfo = base.ExportNode(library);
            var equipmentType = equipmentTypeInfo.tiqModel;

            if (equipmentTypeInfo.Found)
            {
                return equipmentTypeInfo;
            }

            if (!_model.IsAbstract)
            {
                // TODO better way of determining which OPC object types are equipment
                equipmentType.Classification = "equipment";
            }
            library.Types.Add(equipmentType);

            var superTypeInfo = new ObjectTypeModelExportSmip<TTypeModel> { _model = _model.SuperType as TTypeModel }.ExportNode(library);
            if (superTypeInfo != null && superTypeInfo.tiqModel != null)
            {
                equipmentType.SubTypeOfFqn = superTypeInfo.tiqModel.Fqn.ToList();
            }
            //UpdatedTimestamp = _model.NodeSet.PublicationDate,
            //UnlinkRelativeName = false,
            equipmentType.ChildEquipment = new List<SmipTypeComposition>();
            equipmentType.Attributes = new List<SmipTypeAttribute>();

            foreach (var childObject in _model.Objects)
            {
                var childEquipmentTypeInfo = new ObjectModelExportSmip { _model = childObject }.ExportNode(library);
                if (childEquipmentTypeInfo.tiqModel == null)
                {
                    throw new NotImplementedException();
                }
                equipmentType.ChildEquipment.Add(childEquipmentTypeInfo.tiqModel);
            }

            // For now: flatten any properties in sub-folders (Organized - i=35)
            var organizesReferenceId = new ExpandedNodeId(ReferenceTypeIds.Organizes, Namespaces.OpcUa).ToString();
            var organizedProperties = _model.OtherReferencedNodes.Where(rn => rn.ReferenceType?.NodeId == organizesReferenceId).SelectMany(rn => rn.Node.Properties).ToList();
            var organizedVariables = _model.OtherReferencedNodes.Where(rn => rn.ReferenceType?.NodeId == organizesReferenceId).SelectMany(rn => rn.Node.DataVariables).ToList();

            var dataVariableTypeInfos = _model.DataVariables.Concat(organizedVariables).Select(dv => new DataVariableModelExportSmip { _model = dv }.ExportNode(library)).ToList();

            var propertyTypeInfos = _model.Properties.Concat(organizedProperties).Select(prop => new PropertyModelExportSmip { _model = prop as PropertyModel }.ExportNode(library)).ToList();
            var variableTypeInfos = dataVariableTypeInfos.Concat(propertyTypeInfos).ToList();

            foreach (var variableTypeInfo in variableTypeInfos)
            {
                if (variableTypeInfo.tiqModel is SmipTypeAttribute attributeType)
                {
                    equipmentType.Attributes.Add(attributeType);
                }
                else if (variableTypeInfo.tiqModel is SmipTypeComposition childEquipmentType)
                {
                    equipmentType.ChildEquipment.Add(childEquipmentType);
                }
                //else if (variableTypeInfo.tiqModel is TiqEquipmentType equipmentType2)
                //{
                //    var childEquipmentType2 = new ChildEquipmentType
                //    {
                //        Name = equipmentType2.Name,
                //        ChildTypeFqn = equipmentType2.Fqn,

                //    };
                //    equipmentType.ChildEquipmentTypes.Add(childEquipmentType2);
                //}
                else
                {
                    throw new Exception($"Unexpected variable type {variableTypeInfo.tiqModel.GetType().Name} returned for {variableTypeInfo.tiqModel?.RelativeName}");
                }
            }

            foreach (var referencedNode in _model.OtherReferencedNodes)
            {
                // TODO Folder (Organizes) references
            }

            return equipmentTypeInfo;
        }
    }

    public class ObjectTypeModelExportSmip : ObjectTypeModelExportSmip<ObjectTypeModel>
    {
    }

    public class InterfaceModelExportSmip : ObjectTypeModelExportSmip<InterfaceModel>
    {
    }

    public abstract class VariableModelExportSmip<TVariableModel> : InstanceModelExportSmip<SmipNode, TVariableModel, VariableTypeModel>
        where TVariableModel : VariableModel, new()
    {

        public override SmipNodeInfo<SmipNode> ExportNode(SmipTypeSystem library)
        {
            var variableInfo = base.ExportNode(library);
            if (variableInfo.Found)
            {
                return variableInfo;
            }
            // TODO Should dataVariable.DataType have been of type DataTypeModel?
            var dataTypeInfo = new DataTypeModelExportSmip { _model = (DataTypeModel)_model.DataType }.ExportNode(library);

            if (dataTypeInfo.tiqModel is SmipType structTypeInfo)
            {
                // OPC Structure: Model as child equipment
                var childEquipmentType = new SmipTypeComposition
                {
                    RelativeName = variableInfo.tiqModel.RelativeName,
                    Description = variableInfo.tiqModel.Description,
                    DisplayName = variableInfo.tiqModel.DisplayName,
                    Document = variableInfo.tiqModel.Document,
                    Fqn = variableInfo.tiqModel.Fqn,
                    ChildTypeFqn = dataTypeInfo.tiqModel.Fqn.ToArray(),
                    IsRequired = IsRequired(_model.ModellingRule),
                    // max_number
                    // min_number
                };
                variableInfo.tiqModel = childEquipmentType;
                return variableInfo;
            }

            // Scalar or Enumeration: model as attribute
            var attributeType = new SmipTypeAttribute
            {
                RelativeName = variableInfo.tiqModel.RelativeName,
                Description = variableInfo.tiqModel.Description,
                DisplayName = variableInfo.tiqModel.DisplayName,
                Document = variableInfo.tiqModel.Document,
                Fqn = variableInfo.tiqModel.Fqn,

                DataType = dataTypeInfo.DataTypeName,
                IsRequired = IsRequired(_model.ModellingRule),
                // TODO AttributeTypeFqn = GetSmipAttributeType(dataVariable.TypeDefinition),
                //DefaultEnumerationValues =,
                MeasurementUnitFqn = GetSmipMeasurementUnit(_model.EngineeringUnit),
                //SourceCategory = ,
                //state_type_fqn
                DefaultValue = _model.Value,
                // decimal_places = ,
                // attribute_limits
                // default_state_values
                // interpolation_method
                // unlink_relative_name
                // TODO Is there a distinction between properties and variables in Smip?
                //SourceCategory = "dynamic", // TODO properties: config?
            };
            if (dataTypeInfo.tiqModel is SmipEnumerationType)
            {
                attributeType.EnumerationTypeFqn = dataTypeInfo.tiqModel.Fqn;
            }
            variableInfo.tiqModel = attributeType;
            return variableInfo;
        }

        //public override (T ExportedNode, List<UANode> AdditionalNodes) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        //{
        //    if (_model.DataType?.Namespace != null)
        //    {
        //        namespaces.GetIndexOrAppend(_model.DataType.Namespace);
        //    }
        //    else
        //    {
        //        // TODO: should not happen - remove once coded
        //    }
        //    var result = base.GetUANode<UAVariable>(namespaces, aliases);
        //    var dataVariable = result.ExportedNode;

        //    var references = dataVariable.References?.ToList() ?? new List<Reference>();

        //    if (!_model.Properties.Concat(_model.DataVariables).Any(p => p.NodeId == _model.EngUnitNodeId) && (_model.EngineeringUnit != null || !string.IsNullOrEmpty(_model.EngUnitNodeId)))
        //    {
        //        // Add engineering unit property
        //        if (result.AdditionalNodes == null)
        //        {
        //            result.AdditionalNodes = new List<UANode>();
        //        }

        //        var engUnitProp = new UAVariable
        //        {
        //            NodeId = GetNodeIdForExport(!String.IsNullOrEmpty(_model.EngUnitNodeId) ? _model.EngUnitNodeId : $"nsu={_model.Namespace};g={Guid.NewGuid()}", namespaces, aliases),
        //            BrowseName = BrowseNames.EngineeringUnits, // TODO preserve non-standard browsenames (detected based on data type)
        //            DisplayName = new uaExport.LocalizedText[] { new uaExport.LocalizedText { Value = BrowseNames.EngineeringUnits } },
        //            ParentNodeId = dataVariable.NodeId,
        //            DataType = DataTypeIds.EUInformation.ToString(),
        //            References = new Reference[]
        //            {
        //                 new Reference {
        //                     ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasTypeDefinition.ToString(), namespaces, aliases),
        //                     Value = GetNodeIdForExport(VariableTypeIds.PropertyType.ToString(), namespaces, aliases)
        //                 },
        //                 new Reference {
        //                     ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasProperty.ToString(), namespaces, aliases),
        //                     IsForward = false,
        //                     Value = GetNodeIdForExport(dataVariable.NodeId, namespaces, aliases),
        //                 },
        //            },
        //            AccessLevel = _model.EngUnitAccessLevel ?? 1,
        //            // UserAccessLevel: deprecated: never emit
        //        };
        //        if (_model.EngUnitModelingRule != null)
        //        {
        //            engUnitProp.References = AddModelingRuleReference(_model.EngUnitModelingRule, engUnitProp.References.ToList(), namespaces, aliases).ToArray();
        //        }
        //        if (_model.EngineeringUnit != null)
        //        {
        //            EUInformation engUnits = NodeModelOpcExtensions.GetEUInformation(_model.EngineeringUnit);
        //            var euXmlElement = GetExtensionObjectAsXML(engUnits);
        //            engUnitProp.Value = euXmlElement;
        //        }
        //        result.AdditionalNodes.Add(engUnitProp);
        //        references.Add(new Reference
        //        {
        //            ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasProperty.ToString(), namespaces, aliases),
        //            Value = engUnitProp.NodeId,
        //        });
        //    }
        //    if (!_model.Properties.Concat(_model.DataVariables).Any(p => p.NodeId == _model.EURangeNodeId) && (!string.IsNullOrEmpty(_model.EURangeNodeId) || (_model.MinValue.HasValue && _model.MaxValue.HasValue && _model.MinValue != _model.MaxValue)))
        //    {
        //        // Add EURange property
        //        if (result.AdditionalNodes == null)
        //        {
        //            result.AdditionalNodes = new List<UANode>();
        //        }

        //        System.Xml.XmlElement xmlElem = null;

        //        if (_model.MinValue.HasValue && _model.MaxValue.HasValue)
        //        {
        //            var range = new ua.Range
        //            {
        //                Low = _model.MinValue.Value,
        //                High = _model.MaxValue.Value,
        //            };
        //            xmlElem = GetExtensionObjectAsXML(range);
        //        }
        //        var euRangeProp = new UAVariable
        //        {
        //            NodeId = GetNodeIdForExport(!String.IsNullOrEmpty(_model.EURangeNodeId) ? _model.EURangeNodeId : $"nsu={_model.Namespace};g={Guid.NewGuid()}", namespaces, aliases),
        //            BrowseName = BrowseNames.EURange,
        //            DisplayName = new uaExport.LocalizedText[] { new uaExport.LocalizedText { Value = BrowseNames.EURange } },
        //            ParentNodeId = dataVariable.NodeId,
        //            DataType = GetNodeIdForExport(DataTypeIds.Range.ToString(), namespaces, aliases),
        //            References = new[] {
        //                new Reference {
        //                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasTypeDefinition.ToString(), namespaces, aliases),
        //                    Value = GetNodeIdForExport(VariableTypeIds.PropertyType.ToString(), namespaces, aliases),
        //                },
        //                new Reference
        //                {
        //                    ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasProperty.ToString(), namespaces, aliases),
        //                    IsForward = false,
        //                    Value = GetNodeIdForExport(dataVariable.NodeId, namespaces, aliases),
        //                },
        //            },
        //            Value = xmlElem,
        //            AccessLevel = _model.EURangeAccessLevel ?? 1,
        //            // deprecated: UserAccessLevel = _model.EURangeUserAccessLevel ?? 1,
        //        };

        //        if (_model.EURangeModelingRule != null)
        //        {
        //            euRangeProp.References = AddModelingRuleReference(_model.EURangeModelingRule, euRangeProp.References?.ToList() ?? new List<Reference>(), namespaces, aliases).ToArray();
        //        }

        //        result.AdditionalNodes.Add(euRangeProp);
        //        references.Add(new Reference
        //        {
        //            ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasProperty.ToString(), namespaces, aliases),
        //            Value = GetNodeIdForExport(euRangeProp.NodeId, namespaces, aliases),
        //        });
        //    }
        //    if (_model.DataType != null)
        //    {
        //        dataVariable.DataType = GetNodeIdForExport(_model.DataType.NodeId, namespaces, aliases);
        //    }
        //    dataVariable.ValueRank = _model.ValueRank ?? -1;
        //    dataVariable.ArrayDimensions = _model.ArrayDimensions;

        //    if (!string.IsNullOrEmpty(_model.Parent?.NodeId))
        //    {
        //        dataVariable.ParentNodeId = GetNodeIdForExport(_model.Parent.NodeId, namespaces, aliases);
        //        if (!references.Any(r => r.Value == dataVariable.ParentNodeId && r.IsForward == false))
        //        {
        //            var reference = new Reference
        //            {
        //                IsForward = false,
        //                ReferenceType = GetNodeIdForExport((_model.Parent.Properties.Contains(_model) ? ReferenceTypeIds.HasProperty : ReferenceTypeIds.HasComponent).ToString(), namespaces, aliases),
        //                Value = dataVariable.ParentNodeId
        //            };
        //            references.Add(reference);
        //        }
        //        else
        //        {
        //            // TODO ensure we pick the most derived reference type
        //        }
        //    }
        //    if (_model.Value != null)
        //    {
        //        using (var decoder = new JsonDecoder(_model.Value, ServiceMessageContext.GlobalContext))
        //        {
        //            var value = decoder.ReadVariant("Value");
        //            var xml = GetVariantAsXML(value);
        //            dataVariable.Value = xml;
        //        }
        //    }

        //    dataVariable.AccessLevel = _model.AccessLevel ?? 1;
        //    // deprecated: dataVariable.UserAccessLevel = _model.UserAccessLevel ?? 1;
        //    dataVariable.AccessRestrictions = (byte)(_model.AccessRestrictions ?? 0);
        //    dataVariable.UserWriteMask = _model.UserWriteMask ?? 0;
        //    dataVariable.WriteMask = _model.WriteMask ?? 0;
        //    dataVariable.MinimumSamplingInterval = _model.MinimumSamplingInterval ?? 0;

        //    if (references?.Any() == true)
        //    {
        //        dataVariable.References = references.ToArray();
        //    }
        //    return (dataVariable as T, result.AdditionalNodes);
        //}

        private static System.Xml.XmlElement GetExtensionObjectAsXML(object extensionBody)
        {
            var extension = new ExtensionObject(extensionBody);
            var context = new ServiceMessageContext();
            var ms = new System.IO.MemoryStream();
            using (var xmlWriter = new System.Xml.XmlTextWriter(ms, System.Text.Encoding.UTF8))
            {
                xmlWriter.WriteStartDocument();

                using (var encoder = new XmlEncoder(new System.Xml.XmlQualifiedName("uax:ExtensionObject", null), xmlWriter, context))
                {
                    encoder.WriteExtensionObject(null, extension);
                    xmlWriter.WriteEndDocument();
                    xmlWriter.Flush();
                }
            }
            var xml = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(xml.Substring(1));
            var xmlElem = doc.DocumentElement;
            return xmlElem;
        }
        internal static System.Xml.XmlElement GetVariantAsXML(Variant value)
        {
            var context = new ServiceMessageContext();
            var ms = new System.IO.MemoryStream();
            using (var xmlWriter = new System.Xml.XmlTextWriter(ms, System.Text.Encoding.UTF8))
            {
                xmlWriter.WriteStartDocument();
                using (var encoder = new XmlEncoder(new System.Xml.XmlQualifiedName("myRoot"/*, "http://opcfoundation.org/UA/2008/02/Types.xsd"*/), xmlWriter, context))
                {
                    encoder.WriteVariant("value", value);
                    xmlWriter.WriteEndDocument();
                    xmlWriter.Flush();
                }
            }
            var xml = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            var doc = new System.Xml.XmlDocument();

            doc.LoadXml(xml.Substring(1));
            var xmlElem = doc.DocumentElement;
            var xmlValue = xmlElem.FirstChild?.FirstChild?.FirstChild as System.Xml.XmlElement;
            return xmlValue;
        }
    }

    public class DataVariableModelExportSmip : VariableModelExportSmip<DataVariableModel>
    {
        //public override (T ExportedNode, List<UANode> AdditionalNodes) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        //{
        //    var result = base.GetUANode<T>(namespaces, aliases);
        //    var dataVariable = result.ExportedNode;
        //    //var references = dataVariable.References?.ToList() ?? new List<Reference>();
        //    //references.Add(new Reference { ReferenceType = "HasTypeDefinition", Value = GetNodeIdForExport(VariableTypeIds.BaseDataVariableType.ToString(), namespaces, aliases), });
        //    //dataVariable.References = references.ToArray();
        //    return (dataVariable, result.AdditionalNodes);
        //}

        protected override (bool IsChild, NodeId ReferenceTypeId) ReferenceFromParent(NodeModel parent)
        {
            return (parent.DataVariables.Contains(_model), ReferenceTypeIds.HasComponent);
        }
    }

    public class PropertyModelExportSmip : VariableModelExportSmip<PropertyModel>
    {
        //public override (T ExportedNode, List<UANode> AdditionalNodes) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        //{
        //    var result = base.GetUANode<T>(namespaces, aliases);
        //    var property = result.ExportedNode;
        //    var references = property.References?.ToList() ?? new List<Reference>();
        //    var propertyTypeNodeId = GetNodeIdForExport(VariableTypeIds.PropertyType.ToString(), namespaces, aliases);
        //    if (references?.Any(r => r.Value == propertyTypeNodeId) == false)
        //    {
        //        references.Add(new Reference { ReferenceType = GetNodeIdForExport(ReferenceTypeIds.HasTypeDefinition.ToString(), namespaces, aliases), Value = propertyTypeNodeId, });
        //    }
        //    property.References = references.ToArray();
        //    return (property, result.AdditionalNodes);
        //}
        protected override (bool IsChild, NodeId ReferenceTypeId) ReferenceFromParent(NodeModel parent)
        {
            return (false, ReferenceTypeIds.HasProperty);
        }
    }

    public class MethodModelExportSmip : InstanceModelExportSmip<SmipNode, MethodModel, MethodModel>
    {
        //public override (T ExportedNode, List<UANode> AdditionalNodes) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        //{
        //    var result = base.GetUANode<UAMethod>(namespaces, aliases);
        //    var method = result.ExportedNode;
        //    method.MethodDeclarationId = GetNodeIdForExport(_model.TypeDefinition?.NodeId, namespaces, aliases);
        //    // method.ArgumentDescription = null; // TODO - not commonly used
        //    if (method.ParentNodeId != null)
        //    {
        //        var references = method.References?.ToList() ?? new List<Reference>();
        //        AddOtherReferences(references, method.ParentNodeId, ReferenceTypeIds.HasComponent, _model.Parent.Methods.Contains(_model), namespaces, aliases);
        //        method.References = references.Distinct(new ReferenceComparer()).ToArray();
        //    }
        //    return (method as T, result.AdditionalNodes);
        //}
        protected override (bool IsChild, NodeId ReferenceTypeId) ReferenceFromParent(NodeModel parent)
        {
            return (parent.Methods.Contains(_model), ReferenceTypeIds.HasComponent);
        }
    }

    public class VariableTypeModelExportSmip : BaseTypeModelExportSmip<SmipType, VariableTypeModel>
    {
        //public override (T ExportedNode, List<UANode> AdditionalNodes) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        //{
        //    var result = base.GetUANode<UAVariableType>(namespaces, aliases);
        //    var variableType = result.ExportedNode;
        //    variableType.IsAbstract = _model.IsAbstract;
        //    if (_model.DataType != null)
        //    {
        //        variableType.DataType = GetNodeIdForExport(_model.DataType.NodeId, namespaces, aliases);
        //    }
        //    if (_model.ValueRank != null)
        //    {
        //        variableType.ValueRank = _model.ValueRank.Value;
        //    }
        //    variableType.ArrayDimensions = _model.ArrayDimensions;
        //    if (_model.Value != null)
        //    {
        //        using (var decoder = new JsonDecoder(_model.Value, ServiceMessageContext.GlobalContext))
        //        {
        //            var value = decoder.ReadVariant("Value");
        //            var xml = VariableModelExportSmip<VariableModel>.GetVariantAsXML(value);
        //            variableType.Value = xml;
        //        }
        //    }
        //    return (variableType as T, result.AdditionalNodes);
        //}
    }
    public class DataTypeModelExportSmip : BaseTypeModelExportSmip<SmipNode, DataTypeModel>
    {
        /// <summary>
        /// Returns EquipmentType for Structures, TiqEnumerationType for Enumerations, TiqBase + DataTypeName name for other data types
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        public override SmipNodeInfo<SmipNode> ExportNode(SmipTypeSystem library)
        {
            var tiqNodeInfo = base.ExportNode(library);
            if (tiqNodeInfo.Found)
            {
                return tiqNodeInfo;
            }
            var dataType = _model;

            var dataTypeText = dataType.DisplayName?.FirstOrDefault()?.Text;
            if (SmipDataTypes.TryGetValue(dataTypeText?.ToLowerInvariant(), out var SmipDataType))
            {
                tiqNodeInfo.DataTypeName = SmipDataType;
            }
            else
            {
                if (dataType.StructureFields?.Any() == true)
                {
                    var attributeTypes = dataType.StructureFields.Select(f =>
                    {
                        var fieldDataTypeInfo = new DataTypeModelExportSmip { _model = (DataTypeModel)f.DataType }.ExportNode(library);
                        // TODO handle arrays (ValueRank, ArrayDimensions)
                        var attributeType = new SmipTypeAttribute
                        {
                            RelativeName = f.Name.ToLowerInvariant(),
                            Description = f.Description?.FirstOrDefault()?.Text,
                            DisplayName = f.Name,
                            IsRequired = !f.IsOptional,
                        };
                        if (fieldDataTypeInfo.DataTypeName != null)
                        {
                            attributeType.DataType = fieldDataTypeInfo.DataTypeName;
                        }
                        else
                        {
                            // TODO handle embedded structure/enum data types
                            attributeType.DataType = "object";
                        }
                        return attributeType as SmipTypeAttribute;
                    }).ToList();
                    var equipmentType = new SmipType
                    {
                        Fqn = tiqNodeInfo.tiqModel.Fqn,
                        Description = tiqNodeInfo.tiqModel.Description,
                        DisplayName = tiqNodeInfo.tiqModel.DisplayName,
                        RelativeName = tiqNodeInfo.tiqModel.RelativeName,
                        Document = tiqNodeInfo.tiqModel.Document,
                        Attributes = attributeTypes,
                    };
                    library.Types.Add(equipmentType);
                    tiqNodeInfo.tiqModel = equipmentType;
                }
                else if (dataType.EnumFields?.Any() == true)
                {
                    var enumerationType = new SmipEnumerationType
                    {
                        Fqn = tiqNodeInfo.tiqModel.Fqn,
                        Description = tiqNodeInfo.tiqModel.Description,
                        DisplayName = tiqNodeInfo.tiqModel.DisplayName,
                        RelativeName = tiqNodeInfo.tiqModel.RelativeName,
                        Document = tiqNodeInfo.tiqModel.Document,
                        EnumerationNames = dataType.EnumFields.Select(f => f.Name).ToList(),
                        DefaultEnumerationValues = dataType.EnumFields.Select(f => f.Value.ToString(CultureInfo.InvariantCulture)).ToList(),
                    };
                    library.EnumerationTypes.Add(enumerationType);
                    tiqNodeInfo.tiqModel = enumerationType;
                    tiqNodeInfo.DataTypeName = "enumeration";
                    //tiqNodeInfo.DataTypeName = "string"; // For Smip instances that don't support enumeration types
                }
                else
                {
                    // TODO custom scalar types -> AttributeType?
                    tiqNodeInfo.DataTypeName = "string";
                }
            }
            return tiqNodeInfo;
        }

        //public override (T ExportedNode, List<UANode> AdditionalNodes) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        //{
        //    var result = base.GetUANode<UADataType>(namespaces, aliases);
        //    var dataType = result.ExportedNode;
        //    if (_model.StructureFields?.Any() == true)
        //    {
        //        var fields = new List<DataTypeField>();
        //        foreach (var field in _model.StructureFields.OrderBy(f => f.FieldOrder))
        //        {
        //            var uaField = new DataTypeField
        //            {
        //                Name = field.Name,
        //                DataType = GetNodeIdForExport(field.DataType.NodeId, namespaces, aliases),
        //                Description = field.Description.ToExport().ToArray(),
        //                ArrayDimensions = field.ArrayDimensions,
        //                IsOptional = field.IsOptional,
        //            };
        //            if (field.ValueRank != null)
        //            {
        //                uaField.ValueRank = field.ValueRank.Value;
        //            }
        //            if (field.MaxStringLength != null)
        //            {
        //                uaField.MaxStringLength = field.MaxStringLength.Value;
        //            }
        //            fields.Add(uaField);
        //        }
        //        dataType.Definition = new uaExport.DataTypeDefinition
        //        {
        //            Name = GetBrowseNameForExport(namespaces),
        //            SymbolicName = _model.SymbolicName,
        //            Field = fields.ToArray(),
        //        };
        //    }
        //    if (_model.EnumFields?.Any() == true)
        //    {
        //        var fields = new List<DataTypeField>();
        //        foreach (var field in _model.EnumFields)
        //        {
        //            fields.Add(new DataTypeField
        //            {
        //                Name = field.Name,
        //                DisplayName = field.DisplayName?.ToExport().ToArray(),
        //                Description = field.Description?.ToExport().ToArray(),
        //                Value = (int)field.Value,
        //                // TODO: 
        //                //SymbolicName = field.SymbolicName,
        //                //DataType = field.DataType,                         
        //            });
        //        }
        //        dataType.Definition = new uaExport.DataTypeDefinition
        //        {
        //            Name = GetBrowseNameForExport(namespaces),
        //            Field = fields.ToArray(),
        //        };
        //    }
        //    if (_model.IsOptionSet != null)
        //    {
        //        if (dataType.Definition == null)
        //        {
        //            dataType.Definition = new uaExport.DataTypeDefinition { };
        //        }
        //        dataType.Definition.IsOptionSet = _model.IsOptionSet.Value;
        //    }
        //    return (dataType as T, result.AdditionalNodes);
        //}

        static Dictionary<string, string> SmipDataTypes = new Dictionary<string, string>
        {
            { "string", "string" },
            { "basedatatype", "string" },
            { "qualifiedname", "string" },
            { "localizedtext", "string" },
            { "double", "float" },
            { "bool", "bool" },
            { "boolean", "bool" },
            { "datetime", "datetime" },
            { "duration", "interval" },
            { "interval", "interval" },
            { "state", "state" },
            { "int", "int" },
            { "int32", "int" },
            { "geopoint", "geopoint" },
        };

    }

    public class ReferenceTypeModelExportSmip : BaseTypeModelExportSmip<SmipNode, ReferenceTypeModel>
    {
        //public override (T ExportedNode, List<UANode> AdditionalNodes) GetUANode<T>(NamespaceTable namespaces, Dictionary<string, string> aliases)
        //{
        //    var result = base.GetUANode<UAReferenceType>(namespaces, aliases);
        //    result.ExportedNode.IsAbstract = _model.IsAbstract;
        //    result.ExportedNode.InverseName = _model.InverseName?.ToExport().ToArray();
        //    result.ExportedNode.Symmetric = _model.Symmetric;
        //    return (result.ExportedNode as T, result.AdditionalNodes);
        //}
    }

    public static class LocalizedTextExtension
    {
        public static uaExport.LocalizedText ToExport(this NodeModel.LocalizedText localizedText) => localizedText?.Text != null || localizedText?.Locale != null ? new uaExport.LocalizedText { Locale = localizedText.Locale, Value = localizedText.Text } : null;
        public static IEnumerable<uaExport.LocalizedText> ToExport(this IEnumerable<NodeModel.LocalizedText> localizedTexts) => localizedTexts?.Select(d => d.Text != null || d.Locale != null ? new uaExport.LocalizedText { Locale = d.Locale, Value = d.Text } : null).ToArray();
    }

}