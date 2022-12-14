using Opc.Ua;
using ua = Opc.Ua;

using System;
using System.Collections.Generic;
using System.Linq;

using CESMII.OpcUa.NodeSetModel;
using CESMII.OpcUa.NodeSetModel.Opc.Extensions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Opc.Ua.Export;
using System.Reflection.Emit;

namespace CESMII.OpcUa.NodeSetModel.Factory.ThinkIQ
{

    public class NodeModelFactoryThinkIQ : NodeModelFactoryThinkIQ<NodeModel>
    {
        public static Task<List<NodeSetModel>> LoadNodeSetAsync(IOpcUaContext opcContext, UANodeSet nodeSet, Object customState, Dictionary<string, string> Aliases, bool doNotReimport = false)
        {
            if (!nodeSet.Models.Any())
            {
                var ex = new Exception($"Invalid nodeset: no models specified");
                opcContext.Logger.LogError(ex.Message);
                throw ex;
            }

            // Find all models that are used by another nodeset
            var requiredModels = nodeSet.Models.Where(m => m.RequiredModel != null).SelectMany(m => m.RequiredModel).Distinct().ToList();
            var missingModels = requiredModels.Where(rm => opcContext.GetOrAddNodesetModel(rm) == null).ToList();
            if (missingModels.Any())
            {
                throw new Exception($"Missing dependent node sets: {string.Join(", ", missingModels)}");
            }

            var loadedModels = new List<NodeSetModel>();

            foreach (var model in nodeSet.Models)
            {
                var nodesetModel = opcContext.GetOrAddNodesetModel(model);
                if (nodesetModel == null)
                {
                    throw new NodeSetResolverException($"Unable to create node set: {model.ModelUri}");
                }
                nodesetModel.CustomState = customState;
                if (model.RequiredModel != null)
                {
                    foreach (var requiredModel in model.RequiredModel)
                    {
                        var requiredModelInfo = nodesetModel.RequiredModels.FirstOrDefault(rm => rm.ModelUri == requiredModel.ModelUri);
                        if (requiredModelInfo == null)
                        {
                            throw new Exception("Required model not populated");
                        }
                        if (requiredModelInfo.AvailableModel == null)
                        {
                            var availableModel = opcContext.GetOrAddNodesetModel(requiredModel);
                            if (availableModel != null)
                            {
                                requiredModelInfo.AvailableModel = availableModel;
                            }
                        }
                    }
                }
                if (nodeSet.Aliases?.Length > 0)
                {
                    foreach (var alias in nodeSet.Aliases)
                    {
                        Aliases[alias.Value] = alias.Alias;
                    }
                }
                loadedModels.Add(nodesetModel);
            }
            if (nodeSet.Items == null)
            {
                nodeSet.Items = new UANode[0];
            }

            var importedNodes = opcContext.ImportUANodeSet(nodeSet);

            // TODO Read nodeset poperties like author etc. and expose them in Profile editor

            foreach (var node in importedNodes)
            {
                var nodeModel = NodeModelFactoryThinkIQ.Create(opcContext, node, customState, out var bAdded);
                if (nodeModel != null && !bAdded)
                {
                    var nodesetModel = nodeModel.NodeSet;

                    if (!nodesetModel.AllNodesByNodeId.ContainsKey(nodeModel.NodeId))
                    {
                        nodesetModel.UnknownNodes.Add(nodeModel);
                    }
                }
            }
            return Task.FromResult(loadedModels);
        }
    }
    public class NodeModelFactoryThinkIQ<T> where T : NodeModel, new()
    {

        protected T _model;
        protected ILogger Logger;

        protected virtual void Initialize(IOpcUaContext opcContext, NodeState opcNode)
        {
            Logger.LogTrace($"Creating node model for {opcNode}");
            // TODO capture multiple locales from a nodeset: UA library seems to offer only one locale
            _model.DisplayName = opcNode.DisplayName.ToModel();

            var browseNameNamespace = opcContext.NamespaceUris.GetString(opcNode.BrowseName.NamespaceIndex);
            _model.BrowseName = $"{browseNameNamespace};{opcNode.BrowseName.Name}";
            _model.SymbolicName = opcNode.SymbolicName;
            _model.Description = opcNode.Description.ToModel();
            if (opcNode.Categories != null)
            {
                if (_model.Categories == null)
                {
                    _model.Categories = new List<string>();
                }
                _model.Categories.AddRange(opcNode.Categories);
            }
            _model.Documentation = opcNode.NodeSetDocumentation;

            var references = opcContext.GetHierarchyReferences(opcNode);

            foreach (var reference in references)
            {
                var referenceType = opcContext.GetNode(reference.ReferenceTypeId) as ReferenceTypeState;
                if (referenceType == null)
                {
                    throw new Exception($"Reference Type {reference.ReferenceTypeId} not found for reference from {opcNode} to {reference.TargetId} . Missing required model / node set?");
                }
                var referenceTypes = GetBaseTypes(opcContext, referenceType);

                var referencedNode = opcContext.GetNode(reference.TargetId);
                if (referencedNode == null)
                {
                    throw new Exception($"Referenced node {reference.TargetId} not found for {opcNode}");
                }

                if (reference.IsInverse)
                {
                    // TODO UANodeSet.Import should already handle inverse references: investigate why these are not processed
                    // Workaround for now:
                    AddChildToNodeModel(
                        () => NodeModelFactoryThinkIQ<T>.Create(opcContext, referencedNode, this._model.CustomState, out _),
                        opcContext, referenceTypes, opcNode);
                }
                else
                {
                    AddChildToNodeModel(() => this._model, opcContext, referenceTypes, referencedNode);
                }
            }
            Logger.LogTrace($"Created node model {this._model} for {opcNode}");
        }

        private static void AddChildToNodeModel(Func<NodeModel> parentFactory, IOpcUaContext opcContext, List<BaseTypeState> referenceTypes, NodeState referencedNode)
        {
            if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.HasComponent))
            {
                if (referencedNode is BaseObjectState objectState)
                {
                    var parent = parentFactory();
                    var uaChildObject = Create<ObjectModelFactoryOpc, ObjectModel>(opcContext, objectState, parent?.CustomState);
                    if (uaChildObject != null)
                    {
                        if (parent?.Namespace != uaChildObject.Namespace)
                        {
                            // Add the reverse reference to the referencing node (parent)
                            var referencingNodeAndReference = new NodeModel.NodeAndReference { Node = parent, Reference = opcContext.GetNodeIdWithUri(referenceTypes[0].NodeId, out _), };
                            AddChildIfNotExists(uaChildObject, uaChildObject.OtherReferencingNodes, referencingNodeAndReference, opcContext.Logger, false);
                        }
                        AddChildIfNotExists(parent, parent?.Objects, uaChildObject, opcContext.Logger);
                        if (referenceTypes[0].NodeId != ReferenceTypeIds.HasComponent)
                        {
                            // Preserve the more specific reference type as well
                            var nodeAndReference = new NodeModel.NodeAndReference { Node = uaChildObject, Reference = opcContext.GetNodeIdWithUri(referenceTypes[0].NodeId, out _) };
                            AddChildIfNotExists(parent, parent?.OtherReferencedNodes, nodeAndReference, opcContext.Logger, false);
                        }
                    }
                }
                else if (referencedNode is BaseObjectTypeState objectTypeState)
                {
                    // TODO
                }
                else if (referencedNode is BaseDataVariableState variableState)
                {
                    if (ProcessEUInfoAndRanges(opcContext, referencedNode, parentFactory))
                    {
                        // EU Information was captured in the parent model
                        return;
                    }
                    var parent = parentFactory();
                    var variable = Create<DataVariableModelFactoryOpc, DataVariableModel>(opcContext, variableState, parent?.CustomState);
                    AddChildIfNotExists(parent, parent?.DataVariables, variable, opcContext.Logger);
                }
                else if (referencedNode is MethodState methodState)
                {
                    var parent = parentFactory();
                    var method = Create<MethodModelFactoryOpc, MethodModel>(opcContext, methodState, parent?.CustomState);
                    AddChildIfNotExists(parent, parent?.Methods, method, opcContext.Logger);
                }
                else
                {
                    opcContext.Logger.LogWarning($"Ignoring component {referencedNode} with unexpected node type {referencedNode.GetType()}");
                }
            }
            else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.HasProperty))
            {
                if (ProcessEUInfoAndRanges(opcContext, referencedNode, parentFactory))
                {
                    // EU Information was captured in the parent model
                    return;
                }
                // OptionSetValues are not commonly used and if they are they don't differ from the enum definitiones except for reserved bits: just preserve as regular properties/values for now so we can round trip without designer support
                //else if (referencedNode.BrowseName?.Name == BrowseNames.OptionSetValues)
                //{
                //    var parent = parentFactory();
                //    if (parent is DataTypeModel dataType && dataType != null)
                //    {
                //        var optionSetValues = ((referencedNode as BaseVariableState)?.Value as LocalizedText[]);
                //        if (optionSetValues != null)
                //        {
                //            dataType.SetOptionSetValues(optionSetValues.ToModel());
                //            return;
                //        }
                //        else
                //        {
                //            opcContext.Logger.LogInformation($"No or invalid OptionSetValues in {parent} for {referencedNode}");
                //        }
                //    }
                //    else
                //    {
                //        opcContext.Logger.LogInformation($"Unexpected parent {parent} of type {parent.GetType()} for OptionSetValues property {referencedNode}");
                //    }
                //}
                if (referencedNode is PropertyState propertyState)
                {
                    var parent = parentFactory();
                    var property = Create<PropertyModelFactoryOpc, PropertyModel>(opcContext, propertyState, parent?.CustomState);
                    AddChildIfNotExists(parent, parent?.Properties, property, opcContext.Logger);
                }
                else if (referencedNode is BaseDataVariableState variableState)
                {
                    // Surprisingly, properties can also be of type DataVariable
                    var parent = parentFactory();
                    var variable = Create<DataVariableModelFactoryOpc, DataVariableModel>(opcContext, variableState, parent?.CustomState);
                    AddChildIfNotExists(parent, parent?.Properties, variable, opcContext.Logger);
                }
                else
                {
                    var parent = parentFactory();
                    opcContext.Logger.LogWarning($"Ignoring property reference {referencedNode} with unexpected type {referencedNode.GetType()} in {parent}");
                }
            }

            else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.HasInterface))
            {
                if (referencedNode is BaseObjectTypeState interfaceTypeState)
                {
                    var parent = parentFactory();
                    var uaInterface = Create<InterfaceModelFactoryOpc, InterfaceModel>(opcContext, interfaceTypeState, parent?.CustomState);
                    if (uaInterface != null)
                    {
                        AddChildIfNotExists(parent, parent?.Interfaces, uaInterface, opcContext.Logger);
                        if (referenceTypes[0].NodeId != ReferenceTypeIds.HasInterface)
                        {
                            // Preserve the more specific reference type as well
                            var nodeAndReference = new NodeModel.NodeAndReference { Node = uaInterface, Reference = opcContext.GetNodeIdWithUri(referenceTypes[0].NodeId, out _) };
                            AddChildIfNotExists(parent, parent?.OtherReferencedNodes, nodeAndReference, opcContext.Logger);
                        }
                    }
                }
                else
                {
                    var parent = parentFactory();
                    opcContext.Logger.LogWarning($"Ignoring interface {referencedNode} with unexpected type {referencedNode.GetType()} in {parent}");
                }
            }
            //else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.Organizes))
            //{
            //    if (referencedNode is BaseObjectState)
            //    {
            //        var parent = parentFactory();
            //        AddChildIfNotExists(parent, parent?.Objects, Create<ObjectModelFactoryOpc, ObjectModel>(opcContext, referencedNode, parent.CustomState));
            //    }
            //}
            //else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.FromState))
            //{ }
            //else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.ToState))
            //{ }
            //else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.HasEffect))
            //{ }
            //else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.HasCause))
            //{ }
            else if (referenceTypes.Any(n => n.NodeId == ReferenceTypeIds.GeneratesEvent))
            {
                if (referencedNode is BaseObjectTypeState eventTypeState)
                {
                    var parent = parentFactory();
                    var uaEvent = Create<ObjectTypeModelFactoryOpc, ObjectTypeModel>(opcContext, eventTypeState, parent?.CustomState);
                    if (uaEvent != null)
                    {
                        AddChildIfNotExists(parent, parent?.Events, uaEvent, opcContext.Logger);
                        if (referenceTypes[0].NodeId != ReferenceTypeIds.GeneratesEvent)
                        {
                            // Preserve the more specific reference type as well
                            var nodeAndReference = new NodeModel.NodeAndReference { Node = uaEvent, Reference = opcContext.GetNodeIdWithUri(referenceTypes[0].NodeId, out _) };
                            AddChildIfNotExists(parent, parent?.OtherReferencedNodes, nodeAndReference, opcContext.Logger);
                        }
                    }
                }
                else
                {
                    throw new Exception($"Unexpected event type {referencedNode}");
                }
            }
            else
            {
                var parent = parentFactory();
                var referencedModel = Create(opcContext, referencedNode, parent?.CustomState, out _);
                if (referencedModel != null)
                {
                    var nodeAndReference = new NodeModel.NodeAndReference { Node = referencedModel, Reference = opcContext.GetNodeIdWithUri(referenceTypes.FirstOrDefault().NodeId, out _) };
                    AddChildIfNotExists(parent, parent?.OtherReferencedNodes, nodeAndReference, opcContext.Logger, false);

                    // Add the reverse reference to the referencing node (parent)
                    var referencingNodeAndReference = new NodeModel.NodeAndReference { Node = parent, Reference = nodeAndReference.Reference, };
                    AddChildIfNotExists(referencedModel, referencedModel.OtherReferencingNodes, referencingNodeAndReference, opcContext.Logger, false);
                }
                else
                {
                    opcContext.Logger.LogWarning($"Ignoring reference {referenceTypes.FirstOrDefault()} from {parent} to {referencedNode}: unable to resolve node.");
                }
                // Potential candidates for first class representation in the model:
                // {ns=1;i=6030} - ConnectsTo / Hierarchical
                // {ns=2;i=18179} - Requires / Hierarchical
                // {ns=2;i=18178} - Moves / Hierarchical
                // {ns=2;i=18183} - HasSlave / Hierachical
                // {ns=2;i=18180} - IsDrivenBy / Hierarchical
                // {ns=2;i=18182} - HasSafetyStates - Hierarchical
                // {ns=2;i=4002}  - Controls / Hierarchical
            }

        }
        static void AddChildIfNotExists<TColl>(NodeModel parent, IList<TColl> collection, TColl uaChildObject, ILogger logger, bool setParent = true)
        {
            if (uaChildObject == null)
            {
                return;
            }
            if (uaChildObject is InstanceModelBase uaInstance)
            {
                uaInstance.Parent = parent;
                if (uaInstance.Parent != parent)
                {
                    logger.LogWarning($"{uaInstance} has more than one parent. Ignored parent: {parent}, using {uaInstance.Parent}");
                }
            }
            if (collection?.Contains(uaChildObject) == false)
            {
                collection.Add(uaChildObject);
            }
        }

        static bool ProcessEUInfoAndRangesWithoutParent(IOpcUaContext opcContext, NodeState potentialEUNode, object customState)
        {
            if (potentialEUNode.BrowseName?.Name == BrowseNames.EngineeringUnits || (potentialEUNode as BaseVariableState)?.DataType == DataTypeIds.EUInformation
                || potentialEUNode.BrowseName?.Name == BrowseNames.EURange || potentialEUNode.BrowseName?.Name == BrowseNames.InstrumentRange)
            {
                foreach (var referenceToNode in opcContext.GetHierarchyReferences(potentialEUNode).Where(r => r.IsInverse))
                {
                    var referencingNodeState = opcContext.GetNode(referenceToNode.TargetId);
                    var referencingNode = Create(opcContext, referencingNodeState, customState, out _);
                    if (ProcessEUInfoAndRanges(opcContext, potentialEUNode, () => referencingNode))
                    {
                        // captured in the referencing node
                        return true;
                    }
                }
            }
            return false;
        }
        static bool ProcessEUInfoAndRanges(IOpcUaContext opcContext, NodeState referencedNode, Func<NodeModel> parentFactory)
        {
            if (referencedNode.BrowseName?.Name == BrowseNames.EngineeringUnits || (referencedNode as BaseVariableState).DataType == DataTypeIds.EUInformation)
            {
                var parent = parentFactory();
                if (parent is VariableModel parentVariable && parentVariable != null)
                {
                    parentVariable.EngUnitNodeId = opcContext.GetNodeIdWithUri(referencedNode.NodeId, out _);

                    var modelingRuleId = (referencedNode as BaseInstanceState)?.ModellingRuleId;
                    if (modelingRuleId != null)
                    {
                        var modelingRule = opcContext.GetNode(modelingRuleId);
                        if (modelingRule == null)
                        {
                            throw new Exception($"Unable to resolve modeling rule {modelingRuleId}: dependency on UA nodeset not declared?");
                        }
                        parentVariable.EngUnitModelingRule = modelingRule.DisplayName.Text;
                    }
                    if (referencedNode is BaseVariableState euInfoVariable)
                    {
                        parentVariable.EngUnitAccessLevel = euInfoVariable.AccessLevelEx != 1 ? euInfoVariable.AccessLevelEx : null;
                        // deprecated: parentVariable.EngUnitUserAccessLevel = euInfoVariable.UserAccessLevel != 1 ? euInfoVariable.UserAccessLevel : null;
                    }

                    var euInfo = ((referencedNode as BaseVariableState)?.Value as ExtensionObject)?.Body as EUInformation;
                    if (euInfo != null)
                    {
                        parentVariable.SetEngineeringUnits(euInfo);
                    }
                    else
                    {
                        // Nodesets commonly indicate that EUs are required on instances by specifying an empty EU in the class
                    }
                    return true;
                }
            }
            else if (referencedNode.BrowseName?.Name == BrowseNames.EURange)
            {
                var parent = parentFactory();
                if (parent is VariableModel parentVariable && parentVariable != null)
                {
                    parentVariable.EURangeNodeId = opcContext.GetNodeIdWithUri(referencedNode.NodeId, out _);
                    var modelingRuleId = (referencedNode as BaseInstanceState)?.ModellingRuleId;
                    if (modelingRuleId != null)
                    {
                        var modelingRule = opcContext.GetNode(modelingRuleId);
                        if (modelingRule == null)
                        {
                            throw new Exception($"Unable to resolve modeling rule {modelingRuleId}: dependency on UA nodeset not declared?");
                        }
                        parentVariable.EURangeModelingRule = modelingRule.DisplayName.Text;
                    }
                    if (referencedNode is BaseVariableState euRangeVariable)
                    {
                        parentVariable.EURangeAccessLevel = euRangeVariable.AccessLevelEx != 1 ? euRangeVariable.AccessLevelEx : null;
                        // deprecated: parentVariable.EURangeUserAccessLevel = euRangeVariable.UserAccessLevel != 1 ? euRangeVariable.UserAccessLevel : null;
                    }
                    var euRange = ((referencedNode as BaseVariableState)?.Value as ExtensionObject)?.Body as ua.Range;
                    if (euRange != null)
                    {
                        parentVariable.SetRange(euRange);
                    }
                    else
                    {
                        // Nodesets commonly indicate that EURange are required on instances by specifying an enpty EURange in the class
                    }
                    return true;
                }
            }
            else if (referencedNode.BrowseName?.Name == BrowseNames.InstrumentRange)
            {
                var parent = parentFactory();
                if (parent is VariableModel parentVariable && parentVariable != null)
                {
                    var euRange = ((referencedNode as BaseVariableState)?.Value as ExtensionObject)?.Body as ua.Range;
                    if (euRange != null)
                    {
                        parentVariable.SetInstrumentRange(euRange);
                    }
                    else
                    {
                        // Nodesets commonly indicate that an Instrument Range is required on instances by specifying an enpty Instrument Range in the class
                    }
                    return true;
                }
            }
            return false;
        }


        public static NodeModel Create(IOpcUaContext opcContext, NodeState node, object customState, out bool added)
        {
            NodeModel nodeModel;
            added = true;
            if (node is DataTypeState dataType)
            {
                nodeModel = Create<DataTypeModelFactoryOpc, DataTypeModel>(opcContext, dataType, customState);
            }
            else if (node is BaseVariableTypeState variableType)
            {
                nodeModel = Create<VariableTypeModelFactoryOpc, VariableTypeModel>(opcContext, variableType, customState);
            }
            else if (node is BaseObjectTypeState objectType)
            {
                if (objectType.IsAbstract && GetBaseTypes(opcContext, objectType).Any(n => n.NodeId == ObjectTypeIds.BaseInterfaceType))
                {
                    nodeModel = Create<InterfaceModelFactoryOpc, InterfaceModel>(opcContext, objectType, customState);
                }
                else
                {
                    nodeModel = Create<ObjectTypeModelFactoryOpc, ObjectTypeModel>(opcContext, objectType, customState);
                }
            }
            else if (node is BaseObjectState uaObject)
            {
                nodeModel = Create<ObjectModelFactoryOpc, ObjectModel>(opcContext, uaObject, customState);
            }
            else if (node is PropertyState property)
            {
                nodeModel = Create<PropertyModelFactoryOpc, PropertyModel>(opcContext, property, customState);
            }
            else if (node is BaseDataVariableState dataVariable)
            {
                nodeModel = Create<DataVariableModelFactoryOpc, DataVariableModel>(opcContext, dataVariable, customState);
            }
            else if (node is MethodState methodState)
            {
                nodeModel = Create<MethodModelFactoryOpc, MethodModel>(opcContext, methodState, customState);
            }
            else if (node is ReferenceTypeState referenceState)
            {
                nodeModel = Create<ReferenceTypeModelFactoryOpc, ReferenceTypeModel>(opcContext, referenceState, customState);
            }
            else
            {
                if (!(node is ViewState))
                {
                    nodeModel = Create<NodeModelFactoryThinkIQ<T>, T>(opcContext, node, customState);
                }
                else
                {
                    // TODO support Views
                    nodeModel = null;
                }
                added = false;
            }
            return nodeModel;

        }

        public static List<BaseTypeState> GetBaseTypes(IOpcUaContext opcContext, BaseTypeState objectType)
        {
            var baseTypes = new List<BaseTypeState>();
            if (objectType != null)
            {
                baseTypes.Add(objectType);
            }
            var currentObjectType = objectType;
            while (currentObjectType?.SuperTypeId != null)
            {
                var objectSuperType = opcContext.GetNode(currentObjectType.SuperTypeId);
                if (objectSuperType is BaseTypeState)
                {
                    baseTypes.Add(objectSuperType as BaseTypeState);
                }
                else
                {
                    baseTypes.Add(new BaseObjectTypeState { NodeId = objectType.SuperTypeId, Description = "Unknown type: more base types may exist" });
                }
                currentObjectType = objectSuperType as BaseTypeState;
            }
            return baseTypes;
        }


        protected static TNodeModel Create<TNodeModelOpc, TNodeModel>(IOpcUaContext opcContext, NodeState opcNode, object customState) where TNodeModelOpc : NodeModelFactoryThinkIQ<TNodeModel>, new() where TNodeModel : NodeModel, new()
        {
            var nodeId = opcContext.GetNodeIdWithUri(opcNode.NodeId, out var namespaceUri);

            // EngineeringUnits are captured in the datavariable to which they belong in order to simplify the model for consuming applications
            // Need to make sure that the nodes with engineering units get properly captured even if they are processed before the containing node
            if (ProcessEUInfoAndRangesWithoutParent(opcContext, opcNode, customState))
            {
                // Node was captured into a parent: don't create separate model for it
                return null;
            }

            var nodeModel = Create<TNodeModel>(opcContext, nodeId, namespaceUri, customState, out var created);
            var nodeModelOpc = new TNodeModelOpc { _model = nodeModel, Logger = opcContext.Logger };
            if (created)
            {
                nodeModelOpc.Initialize(opcContext, opcNode);
            }
            else
            {
                opcContext.Logger.LogTrace($"Using previously created node model {nodeModel} for {opcNode}");
            }
            return nodeModel;
        }

        public static TNodeModel Create<TNodeModel>(IOpcUaContext opcContext, string nodeId, string opcNamespace, object customState, out bool created) where TNodeModel : NodeModel, new()
        {
            created = false;
            opcContext.NamespaceUris.GetIndexOrAppend(opcNamespace); // Ensure the namespace is in the namespace table
            var nodeModelBase = opcContext.GetModelForNode<TNodeModel>(nodeId);
            var nodeModel = nodeModelBase as TNodeModel;
            if (nodeModel == null)
            {
                if (nodeModelBase != null)
                {
                    throw new Exception($"Internal error - Type mismatch for node {nodeId}: NodeModel of type {typeof(TNodeModel)} was previously created with type {nodeModelBase.GetType()}.");
                }
                nodeModel = new TNodeModel();
                nodeModel.NodeId = nodeId;
                nodeModel.CustomState = customState;
                created = true;

                var nodesetModel = opcContext.GetOrAddNodesetModel(new ModelTableEntry { ModelUri = opcNamespace });
                nodeModel.NodeSet = nodesetModel;
                if (!nodesetModel.AllNodesByNodeId.ContainsKey(nodeModel.NodeId))
                {
                    nodesetModel.AllNodesByNodeId.Add(nodeModel.NodeId, nodeModel);
                    if (nodeModel is InterfaceModel uaInterface)
                    {
                        nodesetModel.Interfaces.Add(uaInterface);
                    }
                    else if (nodeModel is ObjectTypeModel objectType)
                    {
                        nodesetModel.ObjectTypes.Add(objectType);
                    }
                    else if (nodeModel is DataTypeModel dataType)
                    {
                        nodesetModel.DataTypes.Add(dataType);
                    }
                    else if (nodeModel is DataVariableModel dataVariable)
                    {
                        nodesetModel.DataVariables.Add(dataVariable);
                    }
                    else if (nodeModel is VariableTypeModel variableType)
                    {
                        nodesetModel.VariableTypes.Add(variableType);
                    }
                    else if (nodeModel is ObjectModel uaObject)
                    {
                        nodesetModel.Objects.Add(uaObject);
                    }
                    else if (nodeModel is PropertyModel property)
                    {
                        nodesetModel.Properties.Add(property);
                    }
                    else if (nodeModel is MethodModel method)
                    {
                        // TODO nodesetModel.Methods.Add(method);
                    }
                    else if (nodeModel is ReferenceTypeModel referenceType)
                    {
                        nodesetModel.ReferenceTypes.Add(referenceType);
                    }
                    else
                    {
                        throw new Exception($"Unexpected node model type {nodeModel.GetType().FullName} for node {nodeModel}");
                    }
                }
                else
                {
                    // Node already processed
                    opcContext.Logger.LogWarning($"Node {nodeModel} was already in the nodeset model.");
                }

            }
            if (customState != null && nodeModel != null && nodeModel.CustomState == null)
            {
                nodeModel.CustomState = customState;
            }
            return nodeModel;
        }

    }

    public class InstanceModelFactoryOpc<TInstanceModel, TBaseTypeModel, TBaseTypeModelFactoryOpc> : NodeModelFactoryThinkIQ<TInstanceModel>
        where TInstanceModel : InstanceModel<TBaseTypeModel>, new()
        where TBaseTypeModel : NodeModel, new()
        where TBaseTypeModelFactoryOpc : NodeModelFactoryThinkIQ<TBaseTypeModel>, new()
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode)
        {
            base.Initialize(opcContext, opcNode);
            var uaInstance = opcNode as BaseInstanceState;
            var variableTypeDefinition = opcContext.GetNode(uaInstance.TypeDefinitionId);
            if (variableTypeDefinition != null) //is BaseTypeState)
            {
                var typeDefModel = NodeModelFactoryThinkIQ.Create(opcContext, variableTypeDefinition, _model.CustomState, out _); // Create<TBaseTypeModelFactoryOpc, TBaseTypeModel>(opcContext, variableTypeDefinition, null);
                _model.TypeDefinition = typeDefModel as TBaseTypeModel;
                if (_model.TypeDefinition == null)
                {
                    throw new Exception($"Unexpected type definition {variableTypeDefinition}");
                }
            }

            if (uaInstance.ModellingRuleId != null)
            {
                var modelingRuleId = uaInstance.ModellingRuleId;
                var modelingRule = opcContext.GetNode(modelingRuleId);
                if (modelingRule == null)
                {
                    throw new Exception($"Unable to resolve modeling rule {modelingRuleId}: dependency on UA nodeset not declared?");
                }
                _model.ModelingRule = modelingRule.DisplayName.Text;
            }
            if (uaInstance.Parent != null)
            {
                var instanceParent = NodeModelFactoryThinkIQ.Create(opcContext, uaInstance.Parent, null, out _);
                _model.Parent = instanceParent;
                if (_model.Parent != instanceParent)
                {
                    opcContext.Logger.LogWarning($"{_model} has more than one parent. Ignored parent: {instanceParent}, using {_model.Parent}.");
                }
            }
        }

    }

    public class ObjectModelFactoryOpc : InstanceModelFactoryOpc<ObjectModel, ObjectTypeModel, ObjectTypeModelFactoryOpc>
    {
    }

    public class BaseTypeModelFactoryOpc<TBaseTypeModel> : NodeModelFactoryThinkIQ<TBaseTypeModel> where TBaseTypeModel : BaseTypeModel, new()
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode)
        {
            base.Initialize(opcContext, opcNode);
            var uaType = opcNode as BaseTypeState;

            if (uaType.SuperTypeId != null)
            {
                var superTypeNodeId = new ExpandedNodeId(uaType.SuperTypeId, opcContext.NamespaceUris.GetString(uaType.SuperTypeId.NamespaceIndex)).ToString();
                var superTypeModel = opcContext.GetModelForNode<TBaseTypeModel>(superTypeNodeId) as BaseTypeModel;
                if (superTypeModel == null)
                {
                    var superTypeState = opcContext.GetNode(uaType.SuperTypeId) as BaseTypeState;
                    if (superTypeState != null)
                    {
                        superTypeModel = NodeModelFactoryThinkIQ.Create(opcContext, superTypeState, this._model.CustomState, out _) as BaseTypeModel;
                        if (superTypeModel == null)
                        {
                            throw new Exception($"Invalid node {superTypeState} is not a Base Type");
                        }
                    }
                }
                _model.SuperType = superTypeModel;
                _model.RemoveInheritedAttributes(_model.SuperType);
                foreach (var uaInterface in _model.Interfaces)
                {
                    _model.RemoveInheritedAttributes(uaInterface);
                }
            }
            else
            {
                _model.SuperType = null;
            }
            _model.IsAbstract = uaType.IsAbstract;
        }

    }

    public class ObjectTypeModelFactoryOpc<TTypeModel> : BaseTypeModelFactoryOpc<TTypeModel> where TTypeModel : BaseTypeModel, new()
    {
    }

    public class ObjectTypeModelFactoryOpc : ObjectTypeModelFactoryOpc<ObjectTypeModel>
    {
    }

    public class InterfaceModelFactoryOpc : ObjectTypeModelFactoryOpc<InterfaceModel>
    {
    }

    public class VariableModelFactoryOpc<TVariableModel> : InstanceModelFactoryOpc<TVariableModel, VariableTypeModel, VariableTypeModelFactoryOpc>
        where TVariableModel : VariableModel, new()
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode)
        {
            base.Initialize(opcContext, opcNode);
            var variableNode = opcNode as BaseVariableState;

            InitializeDataTypeInfo(_model, opcContext, variableNode);
            if (variableNode.AccessLevelEx != 1) _model.AccessLevel = variableNode.AccessLevelEx;
            // deprecated if (variableNode.UserAccessLevel != 1) _model.UserAccessLevel = variableNode.UserAccessLevel;
            if (variableNode.AccessRestrictions != 0) _model.AccessRestrictions = (ushort)variableNode.AccessRestrictions;
            if (variableNode.WriteMask != 0) _model.WriteMask = (uint)variableNode.WriteMask;
            if (variableNode.UserWriteMask != 0) _model.UserWriteMask = (uint)variableNode.UserWriteMask;
            if (variableNode.MinimumSamplingInterval != 0)
            {
                _model.MinimumSamplingInterval = variableNode.MinimumSamplingInterval;
            }
        }

        internal static void InitializeDataTypeInfo(VariableModel _model, IOpcUaContext opcContext, BaseVariableState variableNode)
        {
            VariableTypeModelFactoryOpc.InitializeDataTypeInfo(_model, opcContext, variableNode, variableNode.DataType, variableNode.ValueRank, variableNode.ArrayDimensions, variableNode.WrappedValue);
        }
    }

    public class DataVariableModelFactoryOpc : VariableModelFactoryOpc<DataVariableModel>
    {
    }

    public class PropertyModelFactoryOpc : VariableModelFactoryOpc<PropertyModel>
    {
    }

    public class MethodModelFactoryOpc : InstanceModelFactoryOpc<MethodModel, MethodModel, MethodModelFactoryOpc> // TODO determine if intermediate base classes of MethodState are worth exposing in the model
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode)
        {
            base.Initialize(opcContext, opcNode);
            if (opcNode is MethodState methodState)
            {
                // Already captured in NodeModel as properties: only need to parse out if we want to provide designer experience for methods
                //_model.MethodDeclarationId = opcContext.GetNodeIdWithUri(methodState.MethodDeclarationId, out var _);
                //_model.InputArguments = _model.Properties.Select(p => p as PropertyModel).ToList();
            }
            else
            {
                throw new Exception($"Unexpected node type for method {opcNode}");
            }
        }
    }

    public class VariableTypeModelFactoryOpc : BaseTypeModelFactoryOpc<VariableTypeModel>
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode)
        {
            base.Initialize(opcContext, opcNode);
            var variableTypeState = opcNode as BaseVariableTypeState;
            InitializeDataTypeInfo(_model, opcContext, variableTypeState);
            //variableTypeState.ValueRank
            //variableTypeState.Value
            //variableTypeState.ArrayDimensions
            //_model.
        }

        internal static void InitializeDataTypeInfo(VariableTypeModel model, IOpcUaContext opcContext, BaseVariableTypeState variableTypeNode)
        {
            VariableTypeModelFactoryOpc.InitializeDataTypeInfo(model, opcContext, variableTypeNode, variableTypeNode.DataType, variableTypeNode.ValueRank, variableTypeNode.ArrayDimensions, variableTypeNode.WrappedValue);
        }

        internal static void InitializeDataTypeInfo(IVariableDataTypeInfo model, IOpcUaContext opcContext, NodeState variableNode, NodeId dataTypeNodeId, int valueRank, ReadOnlyList<uint> arrayDimensions, Variant wrappedValue)
        {
            var dataType = opcContext.GetNode(dataTypeNodeId);
            if (dataType is DataTypeState)
            {
                model.DataType = Create<DataTypeModelFactoryOpc, DataTypeModel>(opcContext, dataType as DataTypeState, null);
            }
            else
            {
                if (dataType == null)
                {
                    throw new Exception($"{variableNode.GetType()} {variableNode}: did not find data type {dataTypeNodeId} (Namespace {opcContext.NamespaceUris.GetString(dataTypeNodeId.NamespaceIndex)}).");
                }
                else
                {
                    throw new Exception($"{variableNode.GetType()} {variableNode}: Unexpected node state {dataTypeNodeId}/{dataType?.GetType().FullName}.");
                }
            }
            if (valueRank != -1)
            {
                model.ValueRank = valueRank;
                if (arrayDimensions != null && arrayDimensions.Any())
                {
                    model.ArrayDimensions = String.Join(",", arrayDimensions);
                }
            }
            if (wrappedValue.Value != null)
            {
                var encodedValue = opcContext.JsonEncodeVariant(wrappedValue);
                model.Value = encodedValue;
            }
        }

    }
    public class DataTypeModelFactoryOpc : BaseTypeModelFactoryOpc<DataTypeModel>
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode)
        {
            base.Initialize(opcContext, opcNode);

            var dataTypeState = opcNode as DataTypeState;
            if (dataTypeState.DataTypeDefinition?.Body != null)
            {
                var sd = dataTypeState.DataTypeDefinition.Body as StructureDefinition;
                if (sd != null)
                {
                    _model.StructureFields = new List<DataTypeModel.StructureField>();
                    int order = 0;
                    foreach (var field in sd.Fields)
                    {
                        var dataType = opcContext.GetNode(field.DataType);
                        if (dataType is DataTypeState)
                        {
                            var dataTypeModel = Create<DataTypeModelFactoryOpc, DataTypeModel>(opcContext, dataType as DataTypeState, null);
                            if (dataTypeModel == null)
                            {
                                throw new Exception($"Unable to resolve data type {dataType.DisplayName}");
                            }
                            var structureField = new DataTypeModel.StructureField
                            {
                                Name = field.Name,
                                DataType = dataTypeModel,
                                ValueRank = field.ValueRank != -1 ? field.ValueRank : null,
                                ArrayDimensions = field.ArrayDimensions != null && field.ArrayDimensions.Any() ? String.Join(",", field.ArrayDimensions) : null,
                                MaxStringLength = field.MaxStringLength != 0 ? field.MaxStringLength : null,
                                Description = field.Description.ToModel(),
                                IsOptional = field.IsOptional,
                                FieldOrder = order++,
                            };
                            _model.StructureFields.Add(structureField);
                        }
                        else
                        {
                            if (dataType == null)
                            {
                                throw new Exception($"Unable to find node state for data type {field.DataType} in {opcNode}");
                            }
                            throw new Exception($"Unexpected node state {dataType?.GetType()?.FullName} for data type {field.DataType} in {opcNode}");
                        }
                    }
                }
                else
                {
                    var enumFields = dataTypeState.DataTypeDefinition.Body as EnumDefinition;
                    if (enumFields != null)
                    {
                        _model.IsOptionSet = enumFields.IsOptionSet;
                        _model.EnumFields = new List<DataTypeModel.UaEnumField>();
                        foreach (var field in enumFields.Fields)
                        {
                            var enumField = new DataTypeModel.UaEnumField
                            {
                                Name = field.Name,
                                DisplayName = field.DisplayName.ToModel(),
                                Value = field.Value,
                                Description = field.Description.ToModel(),
                            };
                            _model.EnumFields.Add(enumField);
                        }
                    }
                    else
                    {
                        throw new Exception($"Unknown data type definition in {dataTypeState}");
                    }
                }
            }
        }
    }

    public class ReferenceTypeModelFactoryOpc : BaseTypeModelFactoryOpc<ReferenceTypeModel>
    {
        protected override void Initialize(IOpcUaContext opcContext, NodeState opcNode)
        {
            base.Initialize(opcContext, opcNode);
            var referenceTypeState = opcNode as ReferenceTypeState;

            _model.InverseName = referenceTypeState.InverseName?.ToModel();
            _model.Symmetric = referenceTypeState.Symmetric;
        }
    }
}

namespace CESMII.OpcUa.NodeSetModel
{

    public static class LocalizedTextExtension
    {
        public static NodeModel.LocalizedText ToModelSingle(this ua.LocalizedText text) => text != null ? new NodeModel.LocalizedText { Text = text.Text, Locale = text.Locale } : null;
        public static List<NodeModel.LocalizedText> ToModel(this ua.LocalizedText text) => text != null ? new List<NodeModel.LocalizedText> { text.ToModelSingle() } : new List<NodeModel.LocalizedText>();
        public static List<NodeModel.LocalizedText> ToModel(this IEnumerable<ua.LocalizedText> texts) => texts?.Select(text => text.ToModelSingle()).Where(lt => lt != null).ToList();
    }
}
