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
    using CESMII.ProfileDesigner.Api.Shared.Models;
    using CESMII.ProfileDesigner.Opc.Ua.NodeSetDBCache;
    using CESMII.OpcUa.NodeSetImporter;
    using Microsoft.Extensions.DependencyInjection;

    public class OpcUaImporter :
#if NODESETDBTEST
        DbOpcUaContext
#else
        DefaultOpcUaContext
#endif
    {
#pragma warning disable S1075 // URIs should not be hardcoded - these are not URLs representing endpoints, but OPC model identifiers (URIs) that are static and stable
        public const string strOpcNamespaceUri = "http://opcfoundation.org/UA/"; //NOSONAR
        public const string strOpcDiNamespaceUri = "http://opcfoundation.org/UA/DI/"; //NOSONAR
#pragma warning restore S1075 // URIs should not be hardcoded

        public OpcUaImporter(
            IDal<ProfileTypeDefinition, ProfileTypeDefinitionModel> dal,
            IDal<LookupDataType, LookupDataTypeModel> dtDal,
            IDal<Profile, ProfileModel> profileDal,
            IDal<NodeSetFile, NodeSetFileModel> nodeSetFileDal,
            IDal<StandardNodeSet, StandardNodeSetModel> dalStandardNodeSet,
            IDal<EngineeringUnit, EngineeringUnitModel> euDal,
            IUANodeSetResolverWithProgress cloudLibResolver,
            ProfileMapperUtil profileUtils,
            ILogger<OpcUaImporter> logger
#if NODESETDBTEST
            , NodeSetModelContext nsDBContext
            ) : base(nsDBContext, logger)
#else
            ) : base(logger)
#endif
        {
            _dal = dal;
            _dtDal = dtDal;
            _euDal = euDal;
            this.Logger = new LoggerCapture(logger);
#if NODESETDBTEST
            this.nsDBContext = nsDBContext;
#endif
            _profileDal = profileDal;
            this._nodeSetFileDal = nodeSetFileDal;
            this._dalStandardNodeSet = dalStandardNodeSet;
            _profileUtils = profileUtils;
            _nodeSetResolver = cloudLibResolver;
        }

        public readonly IDal<ProfileTypeDefinition, ProfileTypeDefinitionModel> _dal;
        public readonly IDal<LookupDataType, LookupDataTypeModel> _dtDal;
        public readonly IDal<EngineeringUnit, EngineeringUnitModel> _euDal;
        public readonly ProfileMapperUtil _profileUtils;
        public readonly LoggerCapture Logger;
        private readonly IUANodeSetResolverWithProgress _nodeSetResolver;
#if NODESETDBTEST
        private readonly NodeSetModelContext nsDBContext;
#endif
        internal readonly IDal<Data.Entities.Profile, DAL.Models.ProfileModel> _profileDal;
        private readonly IDal<NodeSetFile, NodeSetFileModel> _nodeSetFileDal;
        private readonly IDal<StandardNodeSet, StandardNodeSetModel> _dalStandardNodeSet;
        readonly Dictionary<string, string> Aliases = new();


        public async Task<List<WarningsByNodeSet>> ImportUaNodeSets(List<ImportOPCModel> nodeSetXmlList, UserToken userToken, Func<string, TaskStatusEnum, Task> logToImportLog, int logId)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogTrace("Starting import");
            var fileNames = string.Join(", ", nodeSetXmlList.Select(f => f.FileName).ToArray<string>());
            var filesImportedMsg = $"Importing File{(nodeSetXmlList.Count.Equals(1) ? "" : "s")}: {fileNames}";

            _logger.LogInformation($"ImportService|ImportOpcUaProfile|{filesImportedMsg}. User Id:{userToken}.");

            if (_euDal.Count(userToken) == 0)
            {
                await logToImportLog($"Importing engineering units...<br/>{filesImportedMsg}", TaskStatusEnum.InProgress);
                await ImportEngineeringUnitsAsync(UserToken.GetGlobalUser(userToken));
            }
            await logToImportLog($"Validating nodeset files and dependencies...<br/>{filesImportedMsg}", TaskStatusEnum.InProgress);

            //init the warnings object outside the try/catch so that saving warnings happens after conclusion of import.
            //We don't want an execption saving warnings to DB to cause a "failed" import message
            //if something goes wrong on the saving of the warnings to the DB, we handle it outside of the import messages.  
            var nodesetWarnings = new List<WarningsByNodeSet>();

            //wrap the importNodesets for total coverage of exceptions
            //we need to inform the front end of an exception and update the import log on 
            //any failure so it can refresh front end accordingly
            //Todo: Revisit this and limit the try/catch blocks
            try
            {
                #region CM new code for importing all NodeSet in correct order and with Dependency Resolution

                var myNodeSetCache = new UANodeSetDBCache(_nodeSetFileDal, _dalStandardNodeSet, userToken); // DB CACHE

                _profileDal.StartTransaction();
                _logger.LogTrace($"Timestamp||ImportId:{logId}||Importing node set files: {sw.Elapsed}");

                var nodeSetXmlStringList = nodeSetXmlList.Select(nodeSetXml => nodeSetXml.Data).ToList();
                UANodeSetImportResult importedNodeSetFiles = ImportAndDownloadNodeSetFiles(myNodeSetCache, userToken, nodeSetXmlStringList, logToImportLog);
                _logger.LogTrace($"Timestamp||ImportId:{logId}||Imported node set files: {sw.Elapsed}");
                if (!string.IsNullOrEmpty(importedNodeSetFiles.ErrorMessage))
                {
                    //The UA Importer encountered a crash/error
                    //failed complete message
                    _profileDal.RollbackTransaction();
                    await logToImportLog(importedNodeSetFiles.ErrorMessage + $"<br/>{filesImportedMsg}", TaskStatusEnum.Failed);
                    return null;
                }
                if (importedNodeSetFiles?.MissingModels?.Count > 0)
                {
                    //The UA Importer tried to resolve already all missing NodeSet either from Cache or CloudLib but could not find all dependencies
                    //failed complete message
                    _profileDal.RollbackTransaction();
                    var missingModelsText = string.Join(", ", importedNodeSetFiles.MissingModels);
                    await logToImportLog($"Missing dependent node sets: {missingModelsText}.", TaskStatusEnum.Failed);
                    return null;
                }


                var profilesAndNodeSets = new List<ProfileModelAndNodeSet>();

                //This area will be put in an interface that can be used by the Importer (after Friday Presentation)
                try
                {
                    //_logger.LogTrace($"Timestamp||ImportId:{logId}||Getting standard nodesets files: {sw.Elapsed}");
                    //var standardNodeSets = _dalStandardNodeSet.GetAll(userToken);

                    //_logger.LogTrace($"Timestamp||ImportId:{logId}||Verifying standard nodeset: {sw.Elapsed}");
                    //importedNodeSetFiles = UANodeSetValidator.VerifyNodeSetStandard(importedNodeSetFiles, standardNodeSets);
                    //if (!string.IsNullOrEmpty(importedNodeSetFiles?.ErrorMessage))
                    //{
                    //    await logToImportLog(importedNodeSetFiles.ErrorMessage.ToLower() + $"<br/>{filesImportedMsg}", TaskStatusEnum.Failed);
                    //    return null;
                    //}

                    if (importedNodeSetFiles != null && importedNodeSetFiles.Models.Any())
                    {
                        foreach (var tmodel in importedNodeSetFiles.Models)
                        {
                            var profile = FindOrCreateProfileForNodeSet(tmodel, _profileDal, userToken, logId, sw);
                            profilesAndNodeSets.Add(new ProfileModelAndNodeSet
                            {
                                Profile = profile, // TODO use the nodesetfile instead
                                NodeSetModel = tmodel,
                            });

                        }
                    }
                    await logToImportLog($"Nodeset files validated.<br/>{filesImportedMsg}", TaskStatusEnum.InProgress);
                }
                catch (Exception e)
                {
                    myNodeSetCache.DeleteNewlyAddedNodeSetsFromCache(importedNodeSetFiles);
                    //log complete message to logger and abbreviated message to user. 
                    _logger.LogCritical(e, $"ImportId:{logId}||ImportService|ImportOpcUaProfile|{e.Message}");
                    //failed complete message
                    _profileDal.RollbackTransaction();
                    await logToImportLog($"Nodeset validation failed: {e.Message}.<br/>{filesImportedMsg}", TaskStatusEnum.Failed);
                    return null;
                }
                //To Here
                #endregion

                _logger.LogTrace($"Timestamp||ImportId:{logId}||Starting node import: {sw.Elapsed}");
                await logToImportLog($"Processing nodeset data...<br/>{filesImportedMsg}", TaskStatusEnum.InProgress);
                //shield the front end from an exception message. Catch it, log it, and return success is false w/ simplified message
                try
                {
                    // Performance: Load the profiles and data types into the EF cache, in a singe database query
                    Task primeEFCacheTask = null;
                    var startEFCache = sw.Elapsed;
                    var profileIds = profilesAndNodeSets.Select(pn => pn.Profile.ID).Where(i => (i ?? 0) != 0);
                    _logger.LogTrace($"Timestamp||ImportId:{logId}||Loading EF cache: {sw.Elapsed}");
                    primeEFCacheTask = _dal.LoadIntoCacheAsync(pt => profileIds.Contains(pt.ProfileId));
                    primeEFCacheTask = primeEFCacheTask.ContinueWith((t) => _dtDal.LoadIntoCacheAsync(dt => profileIds.Contains(dt.CustomType.ProfileId))).Unwrap();

                    var profileItemsByNodeId = new Dictionary<string, ProfileTypeDefinitionModel>();
                    var modelsToImport = new List<NodeSetModel>();
                    foreach (var profileAndNodeSet in profilesAndNodeSets)
                    {
                        //only show message for the items which are newly imported...
                        if (profileAndNodeSet.NodeSetModel.NewInThisImport)
                        {
                            await logToImportLog($"Processing nodeset file: {profileAndNodeSet.NodeSetModel.NameVersion}...", TaskStatusEnum.InProgress);
                        }
                        var logList = new List<string>();
                        (Logger as LoggerCapture).LogList = logList;

                        var nodeSetModels = await LoadNodeSetAsync(profileAndNodeSet.NodeSetModel.NodeSet, profileAndNodeSet.Profile, !profileAndNodeSet.NodeSetModel.NewInThisImport);
                        if (profileAndNodeSet.NodeSetModel.NewInThisImport)
                        {
                            foreach (var model in nodeSetModels)
                            {
                                if (modelsToImport.FirstOrDefault(m => m.ModelUri == model.ModelUri) == null)
                                {
                                    modelsToImport.Add(model);
                                    if (primeEFCacheTask != null)
                                    {
                                        _logger.LogTrace($"Timestamp||ImportId:{logId}||Waiting for EF cache to load");
                                        await primeEFCacheTask;
                                        var endEFCache = sw.Elapsed;
                                        _logger.LogTrace($"Timestamp||ImportId:{logId}||Finished loading EF cache: {endEFCache - startEFCache}");
                                        primeEFCacheTask = null;
                                    }

                                    var itemsByNodeId = await ImportNodeSetModelAsync(model, userToken);
                                    if (itemsByNodeId != null)
                                    {
                                        foreach (var item in itemsByNodeId)
                                        {
                                            profileItemsByNodeId[item.Key] = item.Value;
                                        }
                                    }
                                }
                                (Logger as LoggerCapture).LogList = null;
                                if (logList.Any())
                                {
                                    nodesetWarnings.Add(new WarningsByNodeSet()
                                    { ProfileId = profileAndNodeSet.Profile.ID.Value, Key = profileAndNodeSet.Profile.ToString(), Warnings = logList });
                                }
                            }
                        }
                    }

                    //CodeSmell:Remove:if (profileItems.Any())
                    //CodeSmell:Remove:{
                    //CodeSmell:Remove:    result = profileItems.Last().Value.ID;
                    //CodeSmell:Remove:}

                    //CodeSmell:Remove: result = 1; // TOD: OPC imported profiles don't get a profiletype when being read back for some reason
                    sw.Stop();
                    var elapsed = sw.Elapsed;
                    var elapsedMsg = $"{elapsed.Minutes}:{elapsed.Seconds} (min:sec)";
                    _logger.LogTrace($"Timestamp||ImportId:{logId}||Import time: {elapsedMsg}, Files: {fileNames} "); //use warning so it shows in app log in db

                    //return success message object
                    filesImportedMsg = $"Imported File{(nodeSetXmlList.Count.Equals(1) ? "" : "s")}: {fileNames}";
                    await logToImportLog($"{filesImportedMsg}", TaskStatusEnum.Completed);
                }
                catch (Exception e)
                {
                    sw.Stop();
                    var elapsed2 = sw.Elapsed;
                    var elapsedMsg2 = $"{elapsed2.Minutes}:{elapsed2.Seconds} (min:sec)";
                    _logger.LogWarning($"Timestamp||ImportId:{logId}||Import time before failure: {elapsedMsg2}, Files: {fileNames} "); //use warning so it shows in app log in db
                                                                                                                                        //log complete message to logger and abbreviated message to user. 
                    _logger.LogCritical(e, $"ImportId:{logId}||ImportService|ImportOpcUaProfile|{e.Message}");
                    //TBD - once we stabilize, take out the specific exception message returned to user because user should not see a code message.
                    _profileDal.RollbackTransaction();
                    var message = e.InnerException != null ? e.InnerException.Message : e.Message;
                    // TODO is it necessary to reacquire the import log dal?
                    await logToImportLog($"An error occurred during the import: {message}.<br/>{filesImportedMsg}", TaskStatusEnum.Failed);
                    //await CreateImportLogMessage(GetImportLogDalIsolated(), logId, userToken, $"An error occurred during the import: {message}.<br/>{filesImportedMsg}", TaskStatusEnum.Failed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"ImportId:{logId}||ImportOpcUaNodeSet error", ex);
                _profileDal.RollbackTransaction();
                // TODO is it necessary to reaquire the import log dal?
                await logToImportLog($"An error occurred during the import: {ex.Message}.<br/>{filesImportedMsg}", TaskStatusEnum.Failed);
                //await CreateImportLogMessage(GetImportLogDalIsolated(), logId, userToken, $"An error occurred during the import: {ex.Message}.<br/>{filesImportedMsg}", TaskStatusEnum.Failed);
            }

            return nodesetWarnings;
        }

        private ProfileModel FindOrCreateProfileForNodeSet(ModelValue tmodel, IDal<Profile, ProfileModel> dalProfile, UserToken userToken, int logId, Stopwatch sw)
        {
            var nsModel = tmodel.NameVersion.CCacheId as NodeSetFileModel;
            _logger.LogTrace($"Timestamp||ImportId:{logId}||Loading nodeset {tmodel.NameVersion.ModelUri}: {sw.Elapsed}");
            var profile = dalProfile.Where(p => p.Namespace == tmodel.NameVersion.ModelUri /*&& p.PublicationDate == tmodel.NameVersion.PublicationDate*/ /*&& (p.AuthorId == null || p.AuthorId == userToken)*/,
                    userToken, verbose: false)?.Data?.OrderByDescending(p => p.Version)?.FirstOrDefault();
            _logger.LogTrace($"Timestamp||ImportId:{logId}||Loaded nodeset {tmodel.NameVersion.ModelUri}: {sw.Elapsed}");

            if (profile == null)
            {
                profile = new ProfileModel
                {
                    Namespace = tmodel.NameVersion.ModelUri,
                    PublishDate = tmodel.NameVersion.PublicationDate,
                    Version = tmodel.NameVersion.ModelVersion,
                    AuthorId = nsModel.AuthorId,
                    StandardProfileID = tmodel.NameVersion.UAStandardModelID,
                };
            }

            if (profile.NodeSetFiles == null)
            {
                profile.NodeSetFiles = new List<NodeSetFileModel>();
            }
            if (!profile.NodeSetFiles.Any(m => m.FileName == nsModel.FileName && m.PublicationDate == nsModel.PublicationDate))
            {
                profile.NodeSetFiles.Add(nsModel);
            }
            return profile;
        }

        private UANodeSetImportResult ImportAndDownloadNodeSetFiles(UANodeSetDBCache myNodeSetCache, UserToken userToken, List<string> nodeSetXmlStringList, Func<string, TaskStatusEnum, Task> logToImportLog)
        {
            OnNodeSet callback = (string namespaceUri, DateTime? publicationDate) =>
            {
                logToImportLog($"Downloading from Cloud Library: {namespaceUri} {publicationDate}", TaskStatusEnum.InProgress).Wait();
            };
            UANodeSetImportResult resultSet;
            try
            {
                _nodeSetResolver.OnDownloadNodeSet += callback;
                resultSet = UANodeSetImporter.ImportNodeSets(myNodeSetCache, null, nodeSetXmlStringList, false, userToken, _nodeSetResolver);
            }
            finally
            {
                _nodeSetResolver.OnDownloadNodeSet -= callback;
            }

            return resultSet;
        }


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

            return NodeModelFactoryOpc.LoadNodeSetAsync(this, nodeSet, profile, this.Aliases, doNotReimport);
        }

        public static readonly ImmutableList<string> _coreNodeSetUris = ImmutableList<string>.Empty.AddRange(new[] { strOpcNamespaceUri, strOpcDiNamespaceUri });

        public async System.Threading.Tasks.Task<Dictionary<string, ProfileTypeDefinitionModel>> ImportNodeSetModelAsync(NodeSetModel nodeSetModel, UserToken userToken)
        {
#if NODESETDBTEST
            {
                var sw2 = Stopwatch.StartNew();
                Logger.LogTrace($"Saving NodeSetModel");
                nsDBContext.SaveChanges();
                Logger.LogTrace($"Saved NodeSetModel after {sw2.Elapsed}");

                var savedModel = nsDBContext.NodeSets
                    .Where(m => m.ModelUri == nodeSetModel.ModelUri && m.PublicationDate == nodeSetModel.PublicationDate)
                    .FirstOrDefault();
                //.ToList();
                //var savedModel2 = nsDBContext.NodeSets.Find(nodeSetModel.ModelUri, nodeSetModel.PublicationDate);
            }
#endif

            ProfileModel profile = (ProfileModel)nodeSetModel.CustomState;

            var authorToken = userToken;
            if (_coreNodeSetUris.Contains(profile.Namespace))
            {
                userToken = UserToken.GetGlobalUser(userToken);
                authorToken = null;
            }
            _dal.StartTransaction();
            foreach (var nsFile in profile.NodeSetFiles)
            {
                await _nodeSetFileDal.UpsertAsync(nsFile, userToken, true);
            }
            await _profileDal.UpsertAsync(profile, userToken, true);
            var dalContext = new DALContext(this, userToken, authorToken, false);
            var profileItemsByNodeId = ImportProfileItems(nodeSetModel, dalContext);
            var sw = Stopwatch.StartNew();
            Logger.LogTrace($"Commiting transaction");
            await _dal.CommitTransactionAsync();
            Logger.LogTrace($"Committed transaction after {sw.Elapsed}");


            if ((profile.ID ?? 0) == 0)
            {
                // Ensure that the Profile has an ID, as it is referenced by the imported NodeModels.
                var writtenProfile = await _profileDal.GetExistingAsync(profile, userToken);
                profile.ID = writtenProfile?.ID;
            }
            return profileItemsByNodeId;
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
                profileModel = _profileDal.GetById(profileItem.ProfileId.Value, userToken);
            }
            var dalContext = new DALContext(this, userToken, authorId, false);
            _lastDalContext = dalContext;
            if (!this._nodesetModels.ContainsKey(strOpcNamespaceUri))
            {
                try
                {
                    // TODO find the right OPC version references in the nodeSet?
                    var opcNodeSetModel = _profileDal.Where(ns => ns.Namespace == strOpcNamespaceUri /*&& (ns.AuthorId == null || ns.AuthorId == userId)*/, userToken, null, null, false, true).Data.OrderByDescending(m => m.PublishDate).FirstOrDefault();
                    // workaround for bug https://github.com/dotnet/runtime/issues/67622
                    var fileCachePatched = opcNodeSetModel.NodeSetFiles[0].FileCache.Replace("<Value/>", "<Value xsi:nil='true' />");
                    using (MemoryStream nodeSetStream = new MemoryStream(Encoding.UTF8.GetBytes(fileCachePatched)))
                    {
                        UANodeSet nodeSet = UANodeSet.Read(nodeSetStream);

                        var opcModel = nodeSet.Models[0];
                        var opcProfile = _profileDal.Where(ns => ns.Namespace == opcModel.ModelUri, userToken, null, null).Data?.FirstOrDefault();
                        //TODO - this next line is time consuming, but still faster than loading from database.
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
            UANodeSet exportedNodeSet = null;
            foreach (var model in this._nodesetModels.Values.Where(model =>
                ((ProfileModel)model.CustomState).Namespace == profileModel.Namespace
                && ((ProfileModel)model.CustomState).PublishDate == profileModel.PublishDate).ToList())
            {
#if NODESETDBTEST
                model.AllNodesByNodeId.Clear();
                nsDBContext.Set<NodeModel>().Where(nm => nm.NodeSet == model).ForEachAsync(nm => model.AllNodesByNodeId.Add(nm.NodeId, nm)).Wait();
                
                foreach (var required in model.RequiredModels)
                {
                    var requiredNodeSet = GetOrAddNodesetModel(new ModelTableEntry { ModelUri = required.ModelUri, PublicationDate = required.PublicationDate ?? default, PublicationDateSpecified = required.PublicationDate != null });
                    requiredNodeSet.AllNodesByNodeId.Clear();
                    nsDBContext.Set<NodeModel>().Where(nm => nm.NodeSet == requiredNodeSet).ForEachAsync(nm => requiredNodeSet.AllNodesByNodeId.Add(nm.NodeId, nm)).Wait();
                    
                }
#else
                model.UpdateIndices();
#endif
                exportedNodeSet = UANodeSetModelImporter.ExportNodeSet(model, this._nodesetModels, this.Aliases);
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

            foreach (var uaObject in nodesetModel.Objects)
            {
                uaObject.ImportProfileItem(dalContext);
            }

            foreach (var uaVariable in nodesetModel.DataVariables)
            {
                if (uaVariable.Parent != null && uaVariable.Parent.Namespace != uaVariable.Namespace)
                {
                    dalContext.Logger.LogWarning($"UAVariable {uaVariable} ({uaVariable.GetDisplayNamePath()}) ignored because it's parent {uaVariable.Parent} is in a different namespace {uaVariable.Parent.Namespace}.");
                    continue;
                }
                if (uaVariable.Parent is DataVariableModel)
                {
                    var variableModel = uaVariable;
                    var parentModel = uaVariable.Parent;
                    //do
                    //{
                    if (dalContext.profileItemsByNodeId.TryGetValue(parentModel.NodeId, out var parentProfileItem))
                    {
                        var nodeIdParts = variableModel.NodeId.Split(';');
                        if (parentProfileItem.Attributes.FirstOrDefault(a => a.OpcNodeId == nodeIdParts[1] && nodeIdParts[0].EndsWith(a.Namespace)) != null)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (uaVariable.Parent is DataVariableModel dvParentModel && dalContext.profileItemsByNodeId.TryGetValue(dvParentModel.Parent.NodeId, out var dvGrandParent))
                        {

                            var nodeIdParts = uaVariable.Parent.NodeId.Split(';');
                            var attribute = dvGrandParent.Attributes?.FirstOrDefault(a => a.OpcNodeId == nodeIdParts[1] && nodeIdParts[0].EndsWith(a.Namespace));
                            if (attribute != null && !string.IsNullOrEmpty(attribute.DataVariableNodeIds))
                            {
                                var map = DataVariableNodeIdMap.GetMap(attribute.DataVariableNodeIds);
                                if (map?.DataVariableNodeIdsByBrowseName.ContainsKey(uaVariable.BrowseName) == true)
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    //} while (false); // containingNode != null); // TODO support nested datavariable
                }
                if (uaVariable.Parent is ObjectTypeModel || uaVariable.Parent is ObjectModel || uaVariable.Parent is VariableTypeModel)
                {
                    if (dalContext.profileItemsByNodeId.TryGetValue(uaVariable.Parent.NodeId, out var parent))
                    {
                        var nodeIdParts = uaVariable.NodeId.Split(';');
                        if (parent.Attributes?.FirstOrDefault(a => a.OpcNodeId == nodeIdParts[1] && nodeIdParts[0].EndsWith(a.Namespace)) != null)
                        {
                            continue;
                        }
                    }
                }
                foreach(var referencingNode in uaVariable.OtherReferencingNodes)
                {
                    if (dalContext.profileItemsByNodeId.TryGetValue(referencingNode.Node.NodeId, out var referencingProfileType))
                    {
                        var nodeIdParts = uaVariable.NodeId.Split(';');
                        if (referencingProfileType.Attributes?.FirstOrDefault(a => a.OpcNodeId == nodeIdParts[1] && nodeIdParts[0].EndsWith(a.Namespace)) != null)
                        {
                            continue;
                        }
                    }

                }
                dalContext.Logger.LogWarning($"UAVariable {uaVariable} ({uaVariable.GetDisplayNamePath()}) ignored.");
            }

            return dalContext.profileItemsByNodeId;
        }

//        static UANodeSet ExportNodeSet(NodeSetModel nodesetModel, Dictionary<string, NodeSetModel> nodesetModels, Dictionary<string, string> aliases)
//        {
//            var exportedNodeSet = new UANodeSet();
//            exportedNodeSet.LastModified = DateTime.UtcNow;
//            exportedNodeSet.LastModifiedSpecified = true;

//#if !NODESETDBTEST
//            nodesetModel.UpdateIndices();
//#endif
//            var namespaceUris = nodesetModel.AllNodesByNodeId.Values.Select(v => v.Namespace).Distinct().ToList();

//            var requiredModels = new List<ModelTableEntry>();

//            NamespaceTable namespaces;
//            // Ensure OPC UA model is the first one
//            if (exportedNodeSet.NamespaceUris?.Any() == true)
//            {
//                namespaces = new NamespaceTable(exportedNodeSet.NamespaceUris);
//            }
//            else
//            {
//                // Ensure OPC UA model is the first one
//                namespaces = new NamespaceTable(new[] { strOpcNamespaceUri });
//            }
//            foreach (var nsUri in namespaceUris)
//            {
//                namespaces.GetIndexOrAppend(nsUri);
//            }
//            var nodeIdsUsed = new HashSet<string>();
//            var items = ExportAllNodes(nodesetModel, aliases, namespaces, nodeIdsUsed);

//            // remove unused aliases
//            var usedAliases = aliases.Where(pk => nodeIdsUsed.Contains(pk.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);

//            // Add aliases for all nodeids from other namespaces
//            var currentNodeSetNamespaceIndex = namespaces.GetIndex(nodesetModel.ModelUri);
//            bool bAliasesAdded = false;
//            foreach (var nodeId in nodeIdsUsed)
//            {
//                var parsedNodeId = NodeId.Parse(nodeId);
//                if (parsedNodeId.NamespaceIndex != currentNodeSetNamespaceIndex
//                    && !usedAliases.ContainsKey(nodeId))
//                {
//                    var namespaceUri = namespaces.GetString(parsedNodeId.NamespaceIndex);
//                    var nodeIdWithUri = new ExpandedNodeId(parsedNodeId, namespaceUri).ToString();
//                    var nodeModel = nodesetModels.Select(nm => nm.Value.AllNodesByNodeId.TryGetValue(nodeIdWithUri, out var model) ? model : null).FirstOrDefault(n => n != null);
//                    var displayName = nodeModel?.DisplayName?.FirstOrDefault()?.Text;
//                    if (displayName != null && !usedAliases.ContainsValue(displayName))
//                    {
//                        usedAliases.Add(nodeId, displayName);
//                        aliases.Add(nodeId, displayName);
//                        bAliasesAdded = true;
//                    }
//                }
//            }

//            var aliasList = usedAliases
//                .Select(alias => new NodeIdAlias { Alias = alias.Value, Value = alias.Key })
//                .OrderBy(kv => kv.Value)
//                .ToList();
//            exportedNodeSet.Aliases = aliasList.ToArray();

//            if (bAliasesAdded)
//            {
//                // Re-export with new aliases
//                items = ExportAllNodes(nodesetModel, aliases, namespaces, null);
//            }

//            var allNamespaces = namespaces.ToArray();
//            if (allNamespaces.Length > 1)
//            {
//                exportedNodeSet.NamespaceUris = allNamespaces.Where(ns => ns != strOpcNamespaceUri).ToArray();
//            }
//            else
//            {
//                exportedNodeSet.NamespaceUris = allNamespaces;
//            }
//            foreach (var uaNamespace in allNamespaces.Except(namespaceUris))
//            {
//                if (!requiredModels.Any(m => m.ModelUri == uaNamespace))
//                {
//                    if (nodesetModels.TryGetValue(uaNamespace, out var requiredNodeSetModel))
//                    {
//                        var requiredModel = new ModelTableEntry
//                        {
//                            ModelUri = uaNamespace,
//                            Version = requiredNodeSetModel.Version,
//                            PublicationDate = requiredNodeSetModel.PublicationDate != null ? DateTime.SpecifyKind(requiredNodeSetModel.PublicationDate.Value, DateTimeKind.Utc) : default,
//                            PublicationDateSpecified = requiredNodeSetModel.PublicationDate != null,
//                            RolePermissions = null,
//                            AccessRestrictions = 0,
//                        };
//                        requiredModels.Add(requiredModel);
//                    }
//                }
//            }

//            var model = new ModelTableEntry
//            {
//                ModelUri = nodesetModel.ModelUri,
//                RequiredModel = requiredModels.ToArray(),
//                AccessRestrictions = 0,
//                PublicationDate = nodesetModel.PublicationDate != null ? DateTime.SpecifyKind(nodesetModel.PublicationDate.Value, DateTimeKind.Utc) : default,
//                PublicationDateSpecified = nodesetModel.PublicationDate != null,
//                RolePermissions = null,
//                Version = nodesetModel.Version,
//            };
//            if (exportedNodeSet.Models != null)
//            {
//                var models = exportedNodeSet.Models.ToList();
//                models.Add(model);
//                exportedNodeSet.Models = models.ToArray();
//            }
//            else
//            {
//                exportedNodeSet.Models = new ModelTableEntry[] { model };
//            }
//            if (exportedNodeSet.Items != null)
//            {
//                var newItems = exportedNodeSet.Items.ToList();
//                newItems.AddRange(items);
//                exportedNodeSet.Items = newItems.ToArray();
//            }
//            else
//            {
//                exportedNodeSet.Items = items.ToArray();
//            }
//            return exportedNodeSet;
//        }

//        private static List<UANode> ExportAllNodes(NodeSetModel nodesetModel, Dictionary<string, string> aliases, NamespaceTable namespaces, HashSet<string> nodeIdsUsed)
//        {
//            var items = new List<UANode>();
//            foreach (var node in nodesetModel.AllNodesByNodeId /*.Where(n => n.Value.Namespace == opcNamespace)*/.OrderBy(n => n.Key))
//            {
//                var result = NodeModelExportOpc.GetUANode(node.Value, namespaces, aliases, nodeIdsUsed);
//                items.Add(result.ExportedNode);
//                if (result.AdditionalNodes != null)
//                {
//                    items.AddRange(result.AdditionalNodes);
//                }
//            }
//            return items;
//        }

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

        private DALContext _lastDalContext;

#region IOpcUaContext
//        public override NodeModel GetModelForNode(string nodeId)
//        {
//            var nodesetModel = base.GetModelForNode(nodeId);
//            if (nodesetModel != null)
//            {
//                return nodesetModel;
//            }

//            var uaNamespace = NodeModelUtils.GetNamespaceFromNodeId(nodeId);

//#if NODESETDBTEST

//            var nodeModelDb = nsDBContext.NodeModels.FirstOrDefault(nm => nm.NodeId == nodeId && nm.NodeSet.ModelUri == uaNamespace);
//            if (nodeModelDb != null)
//            {
//                if (!nodeModelDb.NodeSet.AllNodesByNodeId.ContainsKey(nodeModelDb.NodeId))
//                {
//                    nodeModelDb.NodeSet.AllNodesByNodeId.Add(nodeModelDb.NodeId, nodeModelDb);
//                }
//            }
//            return nodeModelDb;
//#else
//            return null;
//#endif
//        }

        public override NodeSetModel GetOrAddNodesetModel(ModelTableEntry model, bool createNew = true)
        {
            if (!_nodesetModels.TryGetValue(model.ModelUri, out var nodesetModel))
            {
                var profile = _lastDalContext?.GetProfileForNamespace(model.ModelUri);
#if NODESETDBTEST
                var existingNodeSet = GetMatchingOrHigherNodeSetAsync(model.ModelUri, model.PublicationDateSpecified ? model.PublicationDate : null).Result;
                if (existingNodeSet != null)
                {
                    _nodesetModels.Add(existingNodeSet.ModelUri, existingNodeSet);
                    nodesetModel = existingNodeSet;
                    nodesetModel.CustomState = profile;
                    return nodesetModel;
                }
#endif
                if (profile != null)
                {
                    model.Version = profile.Version;
                    model.PublicationDate = profile.PublishDate ?? DateTime.MinValue;
                    model.PublicationDateSpecified = profile.PublishDate.HasValue;
                }
                if (model.PublicationDateSpecified && model.PublicationDate.Kind != DateTimeKind.Utc)
                {
                    model.PublicationDate = DateTime.SpecifyKind(model.PublicationDate, DateTimeKind.Utc);
                }

                nodesetModel = base.GetOrAddNodesetModel(model, createNew);
                if (profile != null)
                {
                    nodesetModel.CustomState = profile;
                }
#if NODESETDBTEST
                if (nodesetModel.PublicationDate == null)
                {
                    nodesetModel.PublicationDate = DateTime.MinValue;
                }
                nsDBContext.Add(nodesetModel);
#endif
            }
            return nodesetModel;
        }

#if NODESETDBTEST
        public Task<NodeSetModel> GetMatchingOrHigherNodeSetAsync(string modelUri, DateTime? publicationDate)
        {
            return GetMatchingOrHigherNodeSetAsync(nsDBContext, modelUri, publicationDate);
        }
        public static Task<NodeSetModel> GetMatchingOrHigherNodeSetAsync(NodeSetModelContext dbContext, string modelUri, DateTime? publicationDate)
        {
            var matchingNodeSet = dbContext.NodeSets.AsQueryable().Where(nsm => nsm.ModelUri == modelUri && (publicationDate == null || nsm.PublicationDate >= publicationDate)).OrderBy(nsm => nsm.PublicationDate).FirstOrDefaultAsync();
            return matchingNodeSet;
        }
#endif

#endregion
        internal ProfileModel GetNodeSetProfile(string uaNamespace)
        {
            if (_nodesetModels.TryGetValue(uaNamespace, out var nodesetModel))
            {
                return (ProfileModel)nodesetModel.CustomState;
            }
            return null;
        }
        private sealed class ProfileModelAndNodeSet
        {
            public ProfileModel Profile { get; set; }
            public ModelValue NodeSetModel { get; set; }
        }

        public sealed class WarningsByNodeSet
        {
            public int ProfileId { get; set; }
            public string Key { get; set; }
            public List<string> Warnings { get; set; }
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
        public Dictionary<string, ProfileTypeDefinitionModel> profileItemsByNodeId { get; } = new Dictionary<string, ProfileTypeDefinitionModel>();

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
                    ((customDataTypeProfile.ID ?? 0) != 0 && l.CustomTypeId == customDataTypeProfile.ID)
                    || ((customDataTypeProfile.ID ?? 0) == 0 && l.CustomType != null && l.CustomType.OpcNodeId == customDataTypeProfile.OpcNodeId && l.CustomType.Profile.Namespace == customDataTypeProfile.Profile.Namespace)
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
            var result = _importer._profileDal.Where(ns => ns.Namespace == uaNamespace, _userToken, null, null, false, false);
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