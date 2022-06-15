//#define NODESETDBTEST
namespace CESMII.ProfileDesigner.OpcUa
{
    using CESMII.ProfileDesigner.Common.Enums;
    using CESMII.ProfileDesigner.DAL;
    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.DAL.Utils;
    using CESMII.ProfileDesigner.Data.Entities;
    using CESMII.OpcUa.NodeSetModel;
    using CESMII.OpcUa.NodeSetModel.Export.Opc;
    using CESMII.OpcUa.NodeSetModel.Factory.Opc;
    using CESMII.OpcUa.NodeSetModel.Opc.Extensions;
    using CESMII.ProfileDesigner.OpcUa.NodeSetModelFactory.Profile;
    using CESMII.ProfileDesigner.OpcUa.NodeSetModelImport.Profile;
#if NODESETDBTEST
    using Microsoft.EntityFrameworkCore;
#endif
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using global::Opc.Ua;
    using global::Opc.Ua.Export;
    using System.Xml.Serialization;
    using System.Xml;
    using System.Collections.Immutable;

    public class OpcUaImporter : IOpcUaContext
    {
#pragma warning disable S1075 // URIs should not be hardcoded - these are not URLs representing endpoints, but OPC model identifiers (URIs) that are static and stable
        public const string strOpcNamespaceUri = "http://opcfoundation.org/UA/"; //NOSONAR
        public const string strOpcDiNamespaceUri = "http://opcfoundation.org/UA/DI/"; //NOSONAR
#pragma warning restore S1075 // URIs should not be hardcoded

        public OpcUaImporter(
            IDal<ProfileTypeDefinition, ProfileTypeDefinitionModel> dal, 
            IDal<LookupDataType, LookupDataTypeModel> dtDal, 
            IDal<Profile, ProfileModel> nsDal,
            IDal<NodeSetFile, NodeSetFileModel> nsFileDal,
            IDal<EngineeringUnit, EngineeringUnitModel> euDal,
            ProfileMapperUtil profileUtils,
            ILogger<OpcUaImporter> logger
            #if NODESETDBTEST
            ,NodeSetModelContext nsDBContext
#endif
            )
        {
            _dal = dal;
            _dtDal = dtDal;
            _euDal = euDal;
            this.Logger = new LoggerCapture(logger);
#if NODESETDBTEST
            this.nsDBContext = nsDBContext;
#endif
            _nsDal = nsDal;
            _nsFileDal = nsFileDal;
            _profileUtils = profileUtils;
            var operationContext = new SystemContext();
            var namespaceTable = new NamespaceTable();
            namespaceTable.GetIndexOrAppend(strOpcNamespaceUri);
            var typeTable = new TypeTable(namespaceTable);
            _systemContext = new SystemContext(operationContext)
            {
                NamespaceUris = namespaceTable,
                TypeTable = typeTable,
            };
        }

        public readonly IDal<ProfileTypeDefinition, ProfileTypeDefinitionModel> _dal;
        public readonly IDal<LookupDataType, LookupDataTypeModel> _dtDal;
        public readonly IDal<EngineeringUnit, EngineeringUnitModel> _euDal;
        public readonly ProfileMapperUtil _profileUtils;
        public readonly LoggerCapture Logger;
#if NODESETDBTEST
        private readonly NodeSetModelContext nsDBContext;
#endif
        internal readonly IDal<Data.Entities.Profile, DAL.Models.ProfileModel> _nsDal;
        private readonly IDal<NodeSetFile, NodeSetFileModel> _nsFileDal;
        private readonly ISystemContext _systemContext;

        readonly NodeStateCollection _importedNodes = new();


        readonly Dictionary<string, NodeSetModel> NodesetModels = new ();
        readonly Dictionary<string, string> Aliases = new();

        public System.Threading.Tasks.Task<List<NodeSetModel>> LoadNodeSetAsync(UANodeSet nodeSet, ProfileModel profile, bool doNotReimport = false)
        {
            if (!nodeSet.Models.Any())
            {
                var ex = new Exception($"Invalid nodeset: no models specified");
                Logger.LogError(ex.Message);
                throw ex;
            }

            var firstModel = nodeSet.Models.FirstOrDefault();
            if (
                firstModel.ModelUri != profile.Namespace
                || firstModel.Version != profile.Version
                || firstModel.PublicationDate.Date != profile.PublishDate?.Date
                )
            {
                throw new Exception($"Mismatching primary model meta data and meta data from cache");
            }

            _importedNodesByNodeId = null;

            return NodeModelFactoryOpc.LoadNodeSetAsync(this, nodeSet, profile, this.NodesetModels, _systemContext, this._importedNodes, out _, this.Aliases, doNotReimport);
        }

        public static readonly ImmutableList<string> _coreNodeSetUris = ImmutableList<string>.Empty.AddRange(new[] { strOpcNamespaceUri, strOpcDiNamespaceUri });

        public async System.Threading.Tasks.Task<Dictionary<string, ProfileTypeDefinitionModel>> ImportNodeSetModelAsync(NodeSetModel nodeSetModel, UserToken userToken)
        {
#if NODESETDBTEST
            {
                var sw2 = Stopwatch.StartNew();
                Logger.LogTrace($"Saving NodeSetModel");
                foreach(var nodeSet in NodesetModels.Where(ns => ns.Key != nodeSetModel.ModelUri))
                {
                    nsDBContext.NodeSets.Attach(nodeSet.Value);
                }
                nsDBContext.NodeSets.Add(nodeSetModel);
                nsDBContext.SaveChanges();
                Logger.LogTrace($"Saved NodeSetModel after {sw2.Elapsed}");

                var savedModel = nsDBContext.NodeSets
                    .Where(m => m.ModelUri == nodeSetModel.ModelUri && m.PublicationDate == nodeSetModel.PublicationDate)
                    .FirstOrDefault();
                //.ToList();
                //var savedModel2 = nsDBContext.NodeSets.Find(nodeSetModel.ModelUri, nodeSetModel.PublicationDate);
            }
#endif

            ProfileModel profile = (ProfileModel) nodeSetModel.CustomState;

            var authorToken = userToken;
            if (_coreNodeSetUris.Contains(profile.Namespace))
            {
                userToken = UserToken.GetGlobalUser(userToken);
                authorToken = null;
            }
            _dal.StartTransaction();
            foreach(var nsFile in profile.NodeSetFiles)
            {
                await _nsFileDal.UpsertAsync(nsFile, userToken, true);
            }
            await _nsDal.UpsertAsync(profile, userToken, true);
            var dalContext = new DALContext(this, userToken, authorToken, false);
            var profileItems = ImportProfileItems(nodeSetModel, dalContext);
            var sw = Stopwatch.StartNew();
            Logger.LogTrace($"Commiting transaction");
            await _dal.CommitTransactionAsync();
            Logger.LogTrace($"Committed transaction after {sw.Elapsed}");


            if ((profile.ID??0) == 0)
            { 
                // Ensure that the Profile has an ID, as it is referenced by the imported NodeModels.
                var writtenProfile = await _nsDal.GetExistingAsync(profile, userToken);
                profile.ID = writtenProfile?.ID;
            }
            return profileItems;
        }

        public bool ExportNodeSet(CESMII.ProfileDesigner.DAL.Models.ProfileModel nodeSetModel, Stream xmlNodeSet, UserToken userToken, UserToken authorId)
        {
            return ExportInternal(nodeSetModel, null, xmlNodeSet, userToken, authorId);
        }
        public bool ExportProfileItem(ProfileTypeDefinitionModel profileItem, Stream xmlNodeSet, UserToken userToken, UserToken authorId)
        {
            return ExportInternal(null, profileItem, xmlNodeSet, userToken, authorId);
        }

        public bool ExportInternal(CESMII.ProfileDesigner.DAL.Models.ProfileModel profileModel, ProfileTypeDefinitionModel profileItem, Stream xmlNodeSet, UserToken userToken, UserToken authorId)
        {
            if (profileItem?.ProfileId != null)
            {
                profileModel = _nsDal.GetById(profileItem.ProfileId.Value, userToken);
            }
            var dalContext = new DALContext(this, userToken, authorId, false);
            _lastDalContext = dalContext;
            if (!NodesetModels.ContainsKey(strOpcNamespaceUri))
            {
                try
                {
                    // TODO find the right OPC version references in the nodeSet?
                    var opcNodeSetModel = _nsDal.Where(ns => ns.Namespace == strOpcNamespaceUri /*&& (ns.AuthorId == null || ns.AuthorId == userId)*/, userToken, null, null, false, true).Data.OrderByDescending(m => m.PublishDate).FirstOrDefault();
                    // workaround for bug https://github.com/dotnet/runtime/issues/67622
                    var fileCachePatched = opcNodeSetModel.NodeSetFiles[0].FileCache.Replace("<Value/>", "<Value xsi:nil='true' />");
                    using (MemoryStream nodeSetStream = new MemoryStream(Encoding.UTF8.GetBytes(fileCachePatched)))
                    {
                        UANodeSet nodeSet = UANodeSet.Read(nodeSetStream);
                        _importedNodesByNodeId = null;
                        // TODO - find a more elegant way to load OPC base data types (needed by DataTypeModel.GetBuiltinDataType)
                        var opcModel = nodeSet.Models[0];
                        var opcProfile = _nsDal.Where(ns => ns.Namespace == opcModel.ModelUri, userToken, null, null).Data?.FirstOrDefault();
                        //TODO - this next line is time consuming.
                        this.LoadNodeSetAsync(nodeSet, opcProfile, true).Wait();
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "Internal error preparing for nodeset export.");
                }
            }

            if (profileItem == null)
            {
                var profileItemsResult = _dal.Where(pi => pi.ProfileId == profileModel.ID /*&& (pi.AuthorId == null || pi.AuthorId == userId)*/, userToken, null, null, false, true);
                if (profileItemsResult.Data != null)
                {
                    foreach (var profile in profileItemsResult.Data)
                    {
                        NodeModelFromProfileFactory.Create(profile, this, dalContext);
                    }
                }
            }
            else
            {
                NodeModelFromProfileFactory.Create(profileItem, this, dalContext);
            }

            // Export the nodesets
            var exportedNodeSet = new UANodeSet();
            foreach (var model in this.NodesetModels.Values.Where(model => 
                ((ProfileModel) model.CustomState).Namespace == profileModel.Namespace 
                && ((ProfileModel) model.CustomState).PublishDate == profileModel.PublishDate))
            {
                model.UpdateIndices();

                ExportNodeSet(exportedNodeSet, model, this.NodesetModels, this.Aliases);
            }
            // .Net6 changed the default to no-identation: https://github.com/dotnet/runtime/issues/64885
            using (StreamWriter writer = new StreamWriter(xmlNodeSet, Encoding.UTF8))
            {
                try
                {
                    var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true, });
                    XmlSerializer serializer = new XmlSerializer(typeof(UANodeSet));
                    serializer.Serialize(xmlWriter, exportedNodeSet);
                }
                finally
                {
                    writer.Flush();
                }
            }
            return true;
        }

        /// <summary>
        /// Creates Profile Items in the backend store based for all OPC nodes in the nodeset model
        /// </summary>
        /// <param name="nodesetModel">nodeset model to import</param>
        /// <param name="updateExisting">Indicates if existing profile items should be updated/overwritten or kept unchanged.</param>
        /// <returns></returns>
        private static Dictionary<string, ProfileTypeDefinitionModel> ImportProfileItems(NodeSetModel nodesetModel, DALContext dalContext)
        {
            nodesetModel.UpdateIndices();
            foreach (var dataType in nodesetModel.DataTypes)
            {
                 dataType.ImportProfileItem(dalContext);
            }
            foreach (var customType in nodesetModel.VariableTypes)
            {
                customType.ImportProfileItem(dalContext);
            }
            foreach (var uaInterface in nodesetModel.Interfaces)
            {
                uaInterface.ImportProfileItem(dalContext);
            }
            foreach (var objectType in nodesetModel.ObjectTypes)
            {
                objectType.ImportProfileItem(dalContext);
            }

            foreach(var uaObject in nodesetModel.Objects)
            {
                uaObject.ImportProfileItem(dalContext);
            }

            foreach (var uaVariable in nodesetModel.DataVariables)
            {
                if (uaVariable.Parent.Namespace != uaVariable.Namespace)
                {
                    dalContext.Logger.LogWarning($"UAVariable {uaVariable} ignored because it's parent {uaVariable.Parent} is in a different namespace {uaVariable.Parent.Namespace}.");
                    continue;
                }
                if (uaVariable.Parent is DataVariableModel)
                {
                    if (dalContext.profileItems.TryGetValue(uaVariable.Parent.NodeId, out var parent))
                    {
                        var nodeIdParts = uaVariable.NodeId.Split(';');
                        if (parent.Attributes.FirstOrDefault(a => a.OpcNodeId == nodeIdParts[1] && nodeIdParts[0].EndsWith(a.Namespace)) != null)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (uaVariable.Parent is DataVariableModel dvParentModel &&  dalContext.profileItems.TryGetValue(dvParentModel.Parent.NodeId, out var dvGrandParent))
                        {

                            var nodeIdParts = uaVariable.Parent.NodeId.Split(';');
                            var attribute = dvGrandParent.Attributes?.FirstOrDefault(a => a.OpcNodeId == nodeIdParts[1] && nodeIdParts[0].EndsWith(a.Namespace));
                            if (attribute != null && !string.IsNullOrEmpty(attribute.DataVariableNodeIds))
                            {
                                var map = DataVariableNodeIdMap.GetMap(attribute.DataVariableNodeIds);
                                if (map.DataVariableNodeIdsByBrowseName.ContainsKey(uaVariable.BrowseName))
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
                if (uaVariable.Parent is ObjectTypeModel || uaVariable.Parent is ObjectModel || uaVariable.Parent is VariableTypeModel)
                {
                    if (dalContext.profileItems.TryGetValue(uaVariable.Parent.NodeId, out var parent))
                    {
                        var nodeIdParts = uaVariable.NodeId.Split(';');
                        if (parent.Attributes?.FirstOrDefault(a => a.OpcNodeId == nodeIdParts[1] && nodeIdParts[0].EndsWith(a.Namespace)) != null)
                        {
                            continue;
                        }
                    }
                }
                dalContext.Logger.LogWarning($"UAVariable {uaVariable} ignored.");
            }

            return dalContext.profileItems;
        }

        static void ExportNodeSet(UANodeSet nodeSet, NodeSetModel nodesetModel, Dictionary<string, NodeSetModel> nodesetModels, Dictionary<string, string> aliases)
        {
            nodesetModel.UpdateIndices();
            var namespaceUris = nodesetModel.AllNodesByNodeId.Values.Select(v => v.Namespace).Distinct().ToList();

            var requiredModels = new List<ModelTableEntry>();

            NamespaceTable namespaces;
            // Ensure OPC UA model is the first one
            if (nodeSet.NamespaceUris?.Any() == true)
            {
                namespaces = new NamespaceTable(nodeSet.NamespaceUris);
            }
            else
            {
                // Ensure OPC UA model is the first one
                namespaces = new NamespaceTable(new[] { strOpcNamespaceUri });
            }
            foreach (var nsUri in namespaceUris)
            {
                namespaces.GetIndexOrAppend(nsUri);
            }
            var nodeIdsUsed = new HashSet<string>();
            var items = ExportAllNodes(nodesetModel, aliases, namespaces, nodeIdsUsed);

            // remove unused aliases
            var usedAliases = aliases.Where(pk => nodeIdsUsed.Contains(pk.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);

            // Add aliases for all nodeids from other namespaces
            var currentNodeSetNamespaceIndex = namespaces.GetIndex(nodesetModel.ModelUri);
            bool bAliasesAdded = false;
            foreach (var nodeId in nodeIdsUsed)
            {
                var parsedNodeId = NodeId.Parse(nodeId);
                if (parsedNodeId.NamespaceIndex != currentNodeSetNamespaceIndex
                    && !usedAliases.ContainsKey(nodeId))
                {
                    var namespaceUri = namespaces.GetString(parsedNodeId.NamespaceIndex);
                    var nodeIdWithUri = new ExpandedNodeId(parsedNodeId, namespaceUri).ToString();
                    var nodeModel = nodesetModels.Select(nm => nm.Value.AllNodesByNodeId.TryGetValue(nodeIdWithUri, out var model) ? model : null).FirstOrDefault(n => n != null);
                    var displayName = nodeModel.DisplayName?.FirstOrDefault()?.Text;
                    if (displayName != null && !usedAliases.ContainsValue(displayName))
                    {
                        usedAliases.Add(nodeId, displayName);
                        aliases.Add(nodeId, displayName);
                        bAliasesAdded = true;
                    }
                }
            }

            var aliasList = usedAliases
                .Select(alias => new NodeIdAlias { Alias = alias.Value, Value = alias.Key })
                .OrderBy(kv => kv.Value)
                .ToList();
            nodeSet.Aliases = aliasList.ToArray();

            if (bAliasesAdded)
            {
                // Re-export with new aliases
                items = ExportAllNodes(nodesetModel, aliases, namespaces, null);
            }

            var allNamespaces = namespaces.ToArray();
            nodeSet.NamespaceUris = allNamespaces.Where(ns => ns != strOpcNamespaceUri).ToArray();
            foreach (var uaNamespace in allNamespaces.Except(namespaceUris))
            {
                if (!requiredModels.Any(m => m.ModelUri == uaNamespace))
                {
                    if (nodesetModels.TryGetValue(uaNamespace, out var requiredNodeSetModel))
                    {
                        var requiredModel = new ModelTableEntry
                        {
                            ModelUri = uaNamespace,
                            Version = requiredNodeSetModel.Version,
                            PublicationDate = requiredNodeSetModel.PublicationDate != null ? DateTime.SpecifyKind(requiredNodeSetModel.PublicationDate.Value, DateTimeKind.Utc) : default,
                            PublicationDateSpecified = requiredNodeSetModel.PublicationDate != null,
                            RolePermissions = null,
                            AccessRestrictions = 0,
                        };
                        requiredModels.Add(requiredModel);
                    }
                }
            }

            var model = new ModelTableEntry
            {
                ModelUri = nodesetModel.ModelUri,
                RequiredModel = requiredModels.ToArray(),
                AccessRestrictions = 0,
                PublicationDate = nodesetModel.PublicationDate != null ? DateTime.SpecifyKind(nodesetModel.PublicationDate.Value, DateTimeKind.Utc) : default,
                PublicationDateSpecified = nodesetModel.PublicationDate != null,
                RolePermissions = null,
                Version = nodesetModel.Version,
            };
            if (nodeSet.Models != null)
            {
                var models = nodeSet.Models.ToList();
                models.Add(model);
                nodeSet.Models = models.ToArray();
            }
            else
            {
                nodeSet.Models = new ModelTableEntry[] { model };
            }
            if (nodeSet.Items != null)
            {
                var newItems = nodeSet.Items.ToList();
                newItems.AddRange(items);
                nodeSet.Items = newItems.ToArray();
            }
            else
            {
                nodeSet.Items = items.ToArray();
            }
        }

        private static List<UANode> ExportAllNodes(NodeSetModel nodesetModel, Dictionary<string, string> aliases, NamespaceTable namespaces, HashSet<string> nodeIdsUsed)
        {
            var items = new List<UANode>();
            foreach (var node in nodesetModel.AllNodesByNodeId /*.Where(n => n.Value.Namespace == opcNamespace)*/.OrderBy(n => n.Key))
            {
                var result = NodeModelExportOpc.GetUANode(node.Value, namespaces, aliases, nodeIdsUsed);
                items.Add(result.Item1);
                if (result.Item2 != null)
                {
                    items.AddRange(result.Item2);
                }
            }
            return items;
        }

        public async Task ImportEngineeringUnitsAsync(UserToken userToken)
        {
            var units = NodeModelOpcExtensions.GetUNECEEngineeringUnits();
            _euDal.StartTransaction();
            foreach (var unit in units)
            {
                await _euDal.AddAsync(new EngineeringUnitModel
                {
                    DisplayName = unit.DisplayName.Text,
                    Description = unit.Description.Text,
                    NamespaceUri = unit.NamespaceUri,
                    UnitId = unit.UnitId,
                }, userToken);
            }
            await _euDal.CommitTransactionAsync();
        }

        Dictionary<NodeId, NodeState> _importedNodesByNodeId;
        private DALContext _lastDalContext;

        #region IOpcUaContext
        public NamespaceTable NamespaceUris { get => _systemContext.NamespaceUris; }

        ILogger IOpcUaContext.Logger => Logger;

        public string GetNodeIdWithUri(NodeId nodeId, out string namespaceUri)
        {
            namespaceUri = GetNamespaceUri(nodeId.NamespaceIndex);
            var nodeIdWithUri = new ExpandedNodeId(nodeId, namespaceUri).ToString();
            return nodeIdWithUri;
        }
    
        public NodeState GetNode(ExpandedNodeId expandedNodeId)
        {
            var nodeId = ExpandedNodeId.ToNodeId(expandedNodeId, _systemContext.NamespaceUris);
            return GetNode(nodeId);
        }

        public NodeState GetNode(NodeId nodeId)
        {
            if (_importedNodesByNodeId == null)
            {
                _importedNodesByNodeId = new Dictionary<NodeId, NodeState>(_importedNodes.Select(n => new KeyValuePair<NodeId, NodeState>(n.NodeId, n)));
            }
            NodeState nodeStateDict = null;
            if (nodeId != null)
            {
                _importedNodesByNodeId.TryGetValue(nodeId, out nodeStateDict);
            }
            return nodeStateDict;
        }

        public string GetNamespaceUri(ushort namespaceIndex)
        {
            return _systemContext.NamespaceUris.GetString(namespaceIndex);
        }

        public NodeModel GetModelForNode(string nodeId)
        {
            var expandedNodeId = ExpandedNodeId.Parse(nodeId, _systemContext.NamespaceUris);
            var uaNamespace = GetNamespaceUri(expandedNodeId.NamespaceIndex);
            if (!NodesetModels.TryGetValue(uaNamespace, out var nodeSetModel))
            {
                return null;
            }
            if (nodeSetModel.AllNodesByNodeId.TryGetValue(nodeId, out var nodeModel))
            {
                return nodeModel;
            }
            return null;
        }

        public NodeSetModel GetOrAddNodesetModel(NodeModel nodeModel)
        {
            var uaNamespace = nodeModel.Namespace;
            if (!NodesetModels.TryGetValue(uaNamespace, out var nodesetModel))
            {
                var profile = _lastDalContext?.GetProfileForNamespace(uaNamespace);
                nodesetModel = new NodeSetModel();
                if (profile != null)
                {
                    nodesetModel.Version = profile.Version;
                    nodesetModel.PublicationDate = profile.PublishDate;
                    nodesetModel.ModelUri = profile.Namespace;
                    nodesetModel.CustomState = profile;
                }
                else
                {
                    nodesetModel.ModelUri = uaNamespace;
                }
                NodesetModels.Add(uaNamespace, nodesetModel);
            }
            nodeModel.NodeSet = nodesetModel;
            return nodesetModel;
        }

        public List<NodeStateHierarchyReference> GetHierarchyReferences(NodeState nodeState)
        {
            var hierarchy = new Dictionary<NodeId, string>();
            var references = new List<NodeStateHierarchyReference>();
            nodeState.GetHierarchyReferences(_systemContext, null, hierarchy, references);
            return references;
        }

        #endregion
        internal ProfileModel GetNodeSetProfile(string uaNamespace)
        {
            if (NodesetModels.TryGetValue(uaNamespace, out var nodesetModel))
            {
                return (ProfileModel) nodesetModel.CustomState;
            }
            return null;
        }

        string IOpcUaContext.JsonEncodeVariant(Variant wrappedValue)
        {
            return DefaultOpcUaContext.JsonEncodeVariant(_systemContext, wrappedValue);
        }
    }

    public class LoggerCapture : ILogger<OpcUaImporter>
    {
        private readonly ILogger<OpcUaImporter> logger;

        public LoggerCapture(ILogger<OpcUaImporter> logger)
        {
            this.logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel >= LogLevel.Warning)
            {
                LogList?.Add($"{logLevel}: {formatter(state, exception)}");
            }
            logger.Log(logLevel, eventId, state, exception, formatter);
        }
        public List<string> LogList { get; set; }
    }

    public class DALContext : IDALContext
    {
        private readonly OpcUaImporter _importer;
        private readonly UserToken _authorToken;
        private readonly UserToken _userToken;
        public DALContext(OpcUaImporter importer, UserToken userToken, UserToken authorToken, bool UpdateExisting)
        {
            _importer = importer;
            _authorToken = authorToken;
            _userToken = userToken;
        }
        public UserToken authorId => _authorToken;

        public bool UpdateExisting { get; set; }
        public Dictionary<string, ProfileTypeDefinitionModel> profileItems { get; } = new Dictionary<string, ProfileTypeDefinitionModel>();

        public ILogger Logger => _importer.Logger;

        public LookupDataTypeModel GetDataType(string opcNamespace, string opcNodeId)
        {
            return _importer._dtDal.Where(dt => dt.CustomType.Profile.Namespace == opcNamespace && dt.CustomType.OpcNodeId == opcNodeId, _userToken, null, null)?.Data?.FirstOrDefault();
        }

        public ProfileTypeDefinitionModel GetProfileItemById(int? propertyTypeDefinitionId)
        {
            if (propertyTypeDefinitionId == null) return null;
            var item = _importer._dal.GetById(propertyTypeDefinitionId.Value, _userToken);
            return item;
        }

        public Task<(int?, bool)> UpsertAsync(ProfileTypeDefinitionModel profileItem, bool updateExisting)
        {
            return _importer._dal.UpsertAsync(profileItem, _userToken, updateExisting);
        }

        public async Task<int?> CreateCustomDataTypeAsync(LookupDataTypeModel customDataTypeLookup)
        {
            return (await _importer._dtDal.UpsertAsync(customDataTypeLookup, _userToken, false)).Item1;
        }

        public Task<LookupDataTypeModel> GetCustomDataTypeAsync(ProfileTypeDefinitionModel customDataTypeProfile)
        {
            var result = _importer._dtDal.Where(l => 
                (
                    ((customDataTypeProfile.ID??0) != 0 && l.CustomTypeId == customDataTypeProfile.ID)
                    || ((customDataTypeProfile.ID??0) == 0 &&  l.CustomType != null && l.CustomType.OpcNodeId == customDataTypeProfile.OpcNodeId && l.CustomType.Profile.Namespace == customDataTypeProfile.Profile.Namespace)
                )
                /*&& (l.OwnerId == null || l.OwnerId == _userId)*/,
            _userToken, null, 1, false, false);
            return Task.FromResult(result.Data.FirstOrDefault());
        }

        public object GetNodeSetCustomState(string uaNamespace)
        {
            return _importer.GetNodeSetProfile(uaNamespace);
        }

        public EngineeringUnitModel GetOrCreateEngineeringUnitAsync(EngineeringUnitModel engUnit)
        {
            engUnit.ID = _importer._euDal.UpsertAsync(engUnit, _userToken, false).Result.Item1;
            return engUnit;
        }

        public ProfileTypeDefinitionSimpleModel MapToModelProfileSimple(ProfileTypeDefinitionModel profileTypeDef)
        {
            return _importer._profileUtils.MapToModelProfileSimple(profileTypeDef);
        }

        public ProfileModel GetProfileForNamespace(string uaNamespace)
        {
            var result = _importer._nsDal.Where(ns => ns.Namespace == uaNamespace, _userToken, null, null, false, false);
            if (result?.Data?.Any() == true)
            {
                if (result.Data.Count > 1)
                {
                    // TODO handle multiple imported nodeset versions (compute highest etc.)
                    throw new Exception($"Found more than one version of {uaNamespace}");
                }
                var profile = result.Data.FirstOrDefault();
                return profile;
            }
            return null;
        }

        public ProfileTypeDefinitionModel CheckExisting(ProfileTypeDefinitionModel profileItem)
        {
            return _importer._dal.GetExistingAsync(profileItem, _userToken, true).Result;
        }
    }

}