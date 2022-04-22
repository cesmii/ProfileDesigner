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

    public class OpcUaImporter : IOpcUaContext
    {
        const string strOpcNamespaceUri = "http://opcfoundation.org/UA/";
        const string strOpcDiNamespaceUri = "http://opcfoundation.org/UA/DI/";
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

        public IDal<ProfileTypeDefinition, ProfileTypeDefinitionModel> _dal;
        public IDal<LookupDataType, LookupDataTypeModel> _dtDal;
        public IDal<EngineeringUnit, EngineeringUnitModel> _euDal;
        public readonly ProfileMapperUtil _profileUtils;
        public readonly LoggerCapture Logger;
#if NODESETDBTEST
        private readonly NodeSetModelContext nsDBContext;
#endif
        public IDal<Data.Entities.Profile, DAL.Models.ProfileModel> _nsDal;
        private readonly IDal<NodeSetFile, NodeSetFileModel> _nsFileDal;
        public ISystemContext _systemContext;

        NodeStateCollection _importedNodes = new NodeStateCollection();


        Dictionary<string, NodeSetModel> NodesetModels = new Dictionary<string, NodeSetModel>();
        Dictionary<string, string> Aliases = new Dictionary<string, string>();

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

        public static List<string> _coreNodeSetUris = new List<string> { strOpcNamespaceUri, strOpcDiNamespaceUri };

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
                await _nsFileDal.Upsert(nsFile, userToken, true);
            }
            var result = await _nsDal.Upsert(profile, userToken, true);
            var dalContext = new DALContext(this, userToken, authorToken, false);
            var profileItems = ImportProfileItems(nodeSetModel, dalContext);
            var sw = Stopwatch.StartNew();
            Logger.LogTrace($"Commiting transaction");
            await _dal.CommitTransactionAsync();
            Logger.LogTrace($"Committed transaction after {sw.Elapsed}");


            // TODO figure out why the InstanceParent property doesn't get written propertly: this fixup only works for some cases and is very slow
            //foreach (var item in dalContext.profileItems.Values.Where(pi => pi.InstanceParent != null))
            //{
            //    var existingItem = await _dal.GetExistingAsync(item, userId);
            //    if (existingItem.InstanceParent == null)
            //    {
            //        existingItem.InstanceParent = item.InstanceParent;
            //        try
            //        {
            //            await dalContext.UpsertAsync(existingItem, true);
            //        }
            //        catch (Exception ex) 
            //        {
            //            Logger.LogError(ex.InnerException != null ? ex.InnerException : ex , $"Error updating instance parent for {existingItem} to {item.InstanceParent}");
            //        }
            //    }
            //}

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
                //if (!(profileModel.AuthorId == null || profileModel.AuthorId == userId))
                //{
                //    throw new Exception($"User does not have access to profile on profileItem {profileItem}");
                //}
            }
            //var uriToExport = nodeSetModel?.Namespace ?? profileItem.Namespace;
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
                    //var nodeSetFilePath = Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), "Nodesets", "Opc.Ua.NodeSet2.xml");
                    //using (Stream nodeSetStream = new FileStream(nodeSetFilePath, FileMode.Open))
                    {
                        UANodeSet nodeSet = UANodeSet.Read(nodeSetStream);
                        _importedNodesByNodeId = null;
                        // Get aliases from base UA model
                        // TODO remove unused aliases later
                        //foreach (var alias in nodeSet.Aliases)
                        //{
                        //    this.Aliases[alias.Value] = alias.Alias;
                        //}
                        // TODO find a more elegant way to load OPC base data types (needed by DataTypeModel.GetBuiltinDataType)
                        var opcModel = nodeSet.Models[0];
                        var opcProfile = _nsDal.Where(ns => ns.Namespace == opcModel.ModelUri /*&& ns.PublicationDate == opcModel.PublicationDate*/ /*&& (ns.AuthorId == null || ns.AuthorId == userId)*/, userToken, null, null).Data?.FirstOrDefault();
                        //TBD - this next line is time consuming.
                        this.LoadNodeSetAsync(nodeSet, opcProfile
                            //    new OPCUANodeSetHelpers.ModelNameAndVersion 
                            //{ 
                            //    ModelUri = opcModel.ModelUri,
                            //    ModelVersion = opcModel.Version,
                            //    PublicationDate = opcModel.PublicationDate,
                            //    CacheId = opcNodeSetModel.ID,
                            //}
                            , true).Wait();
                    }
                }
                catch { }
            }

            if (profileItem == null)
            {
                var profileItemsResult = _dal.Where(pi => pi.ProfileId == profileModel.ID /*&& (pi.AuthorId == null || pi.AuthorId == userId)*/, userToken, null, null, false, true);
                if (profileItemsResult.Data != null)
                {
                    foreach (var profile in profileItemsResult.Data)
                    {
                        var nodeModel = NodeModelFromProfileFactory.Create(profile, this, dalContext);
                    }
                }
            }
            else
            {
                var nodeModel = NodeModelFromProfileFactory.Create(profileItem, this, dalContext);
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
            exportedNodeSet.Write(xmlNodeSet);
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
                            var attribute = dvGrandParent.Attributes.FirstOrDefault(a => a.OpcNodeId == nodeIdParts[1] && nodeIdParts[0].EndsWith(a.Namespace));
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
                    // These are usually OK as they are generated based on the variable type
                    //continue;
                }
                if (uaVariable.Parent is ObjectTypeModel || uaVariable.Parent is ObjectModel || uaVariable.Parent is VariableTypeModel)
                {
                    if (dalContext.profileItems.TryGetValue(uaVariable.Parent.NodeId, out var parent))
                    {
                        var nodeIdParts = uaVariable.NodeId.Split(';');
                        if (parent.Attributes.FirstOrDefault(a => a.OpcNodeId == nodeIdParts[1] && nodeIdParts[0].EndsWith(a.Namespace)) != null)
                        {
                            continue;
                        }
                    }
                }
                dalContext.Logger.LogWarning($"UAVariable {uaVariable} ignored.");
            }

            return dalContext.profileItems;
        }

        static UANodeSet ExportNodeSet(UANodeSet nodeSet, NodeSetModel nodesetModel, Dictionary<string, NodeSetModel> nodesetModels, Dictionary<string, string> aliases)
        {
            //var nodeSet = new UANodeSet();

            // TODO gather aliases from each nodesetModel
            var aliasList = new List<NodeIdAlias>();
            foreach (var alias in aliases)
            {
                aliasList.Add(new NodeIdAlias { Alias = alias.Value, Value = alias.Key });
            }
            nodeSet.Aliases = aliasList.ToArray();

            nodesetModel.UpdateIndices();
            var namespaceUris = nodesetModel.AllNodes.Values.Select(v => v.Namespace).Distinct().ToList();

            var requiredModels = new List<ModelTableEntry>();

            var items = new List<UANode>();

            NamespaceTable namespaces;
            // Ensure OPC UA model is the first one
            if (nodeSet.NamespaceUris?.Any()== true)
            {
                namespaces = new NamespaceTable(nodeSet.NamespaceUris);
            }
            else
            {
                // Ensure OPC UA model is the first one
                namespaces = new NamespaceTable(new[] { strOpcNamespaceUri });
            }
            foreach(var nsUri in namespaceUris)
            {
                namespaces.GetIndexOrAppend(nsUri);
            }
            foreach (var node in nodesetModel.AllNodes /*.Where(n => n.Value.Namespace == opcNamespace)*/.OrderBy(n => n.Key))
            {
                var result = NodeModelExportOpc.GetUANode(node.Value, namespaces, aliases);//.GetUANode<UANode>(namespaces, aliases);
                items.Add(result.Item1);
                if (result.Item2 != null)
                {
                    items.AddRange(result.Item2);
                }
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
            return nodeSet;
        }

        public async Task ImportEngineeringUnitsAsync(UserToken userToken)
        {
            var units = NodeModelOpcExtensions.GetUNECEEngineeringUnits();
            _euDal.StartTransaction();
            foreach (var unit in units)
            {
                await _euDal.Add(new EngineeringUnitModel
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

        public NodeState GetNode(NodeId expandedNodeId)
        {
            if (_importedNodesByNodeId == null)
            {
                _importedNodesByNodeId = new Dictionary<NodeId, NodeState>(_importedNodes.Select(n => new KeyValuePair<NodeId, NodeState>(n.NodeId, n)));
            }
            //var nodeState = _importedNodes.FirstOrDefault(n => n.NodeId == expandedNodeId);
            NodeState nodeStateDict = null;
            if (expandedNodeId != null)
            {
                _importedNodesByNodeId.TryGetValue(expandedNodeId, out nodeStateDict);
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
            if (nodeSetModel.AllNodes.TryGetValue(nodeId, out var nodeModel))
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
                var profile = _lastDalContext?.GetProfileForNamespace(uaNamespace);// _nsDal.Where(ns => ns.Namespace == uaNamespace && (ns.AuthorId == null || ns.AuthorId == this._lastDalContext._userId) , null, null, false, false);
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
        private ILogger<OpcUaImporter> logger;

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
        private OpcUaImporter _importer;
        private UserToken _authorToken;
        private UserToken _userToken;
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

        public LookupDataTypeModel GetDataType(string dataTypeName)
        {
            return _importer._dtDal.Where(dt => dt.Name == dataTypeName /*&& (dt.OwnerId == null || dt.OwnerId == _userId)*/, _userToken, null, null)?.Data?.FirstOrDefault();
        }

        public ProfileTypeDefinitionModel GetProfileItemById(int? id)
        {
            if (id == null) return null;
            var item = _importer._dal.GetById(id.Value, _userToken);
            //if (item?.AuthorId == null || item?.AuthorId == _userId)
            //{
            //    return item;
            //}
            return item;
        }

        public Task<(int?, bool)> UpsertAsync(ProfileTypeDefinitionModel profileItem, bool updateExisting)
        {
            //if (updateExisting)
            {
                return _importer._dal.Upsert(profileItem, _userToken, updateExisting);
            }
            //else
            //{
            //    return Task.FromResult((_importer._dal.Add(profileItem, _userId).Result, true));
            //}
        }

        public async Task<int?> CreateCustomDataTypeAsync(LookupDataTypeModel customDataTypeLookup)
        {
            return (await _importer._dtDal.Upsert(customDataTypeLookup, _userToken, false)).Item1;
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
            engUnit.ID = _importer._euDal.Upsert(engUnit, _userToken, false).Result.Item1;
            return engUnit;
        }

        public ProfileTypeDefinitionSimpleModel MapToModelProfileSimple(ProfileTypeDefinitionModel profileTypeDef)
        {
            return _importer._profileUtils.MapToModelProfileSimple(profileTypeDef);
        }

        public ProfileModel GetProfileForNamespace(string uaNamespace)
        {
            var result = _importer._nsDal.Where(ns => ns.Namespace == uaNamespace /*&& (ns.AuthorId == null || ns.AuthorId == this._userId)*/, _userToken, null, null, false, false);
            if (result?.Data?.Any() == true)
            {
                var profile = result.Data.FirstOrDefault();
                foreach (var info in result.Data.Skip(1))
                {
                    // TODO handle multiple imported nodeset versions (compute highest etc.)
                    throw new Exception($"Found more than one version of {uaNamespace}");
                }
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