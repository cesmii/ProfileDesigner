using CESMII.OpcUa.NodeSetImporter;
using CESMII.OpcUa.NodeSetModel;
using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.Api.Shared.Utils;
using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.Opc.Ua.NodeSetDBCache;
using CESMII.ProfileDesigner.OpcUa;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CESMII.ProfileDesigner.Api.Utils
{
    public class ImportService
    {
        private readonly ILogger<ImportService> _logger;
        private readonly BackgroundWorkerQueue _backgroundWorkerQueue;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IDal<ImportLog, ImportLogModel> _dalImportLog;
        private readonly IConfiguration _configuration;
        private readonly IUANodeSetResolver _nodeSetResolver;

        public ImportService(BackgroundWorkerQueue backgroundWorkerQueue, IServiceScopeFactory serviceScopeFactory,
            IDal<ImportLog, ImportLogModel> dalImportLog,
            IUANodeSetResolver cloudLibResolver,
            ILogger<ImportService> logger,
            IConfiguration configuration)

        {
            _backgroundWorkerQueue = backgroundWorkerQueue;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _dalImportLog = dalImportLog;
            _configuration = configuration;
            _nodeSetResolver = cloudLibResolver;
        }

        public async Task<int> CallSlowMethod(List<ImportOPCModel> nodeSetXmlList, UserToken userToken)
        {
            //create a start log item
            var fileNames = string.Join(", ", nodeSetXmlList.Select(f => f.FileName).ToArray<string>());

            //the rest of the fields are set in the dal
            ImportLogModel logItem = new ImportLogModel()
            {
                FileList = nodeSetXmlList.Select(f => f.FileName).ToArray<string>(),
                Messages = new List<ImportLogMessageModel>() {
                    new ImportLogMessageModel() {
                        Message = $"The import is processing for files {fileNames}..."
                    }
                }
            };
            var logId = await _dalImportLog.Add(logItem, userToken);

            //slow task - kick off in background
            //_backgroundWorkerQueue.QueueBackgroundWorkItem(async token =>
            _ = Task.Run(async () =>
            {
                //wrap in scope so that we don't lose the scope of the dependency injected objects once the 
                //web api request completes and disposes of the import service object (and its module vars)
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dalImportLog = scope.ServiceProvider.GetService<IDal<ImportLog, ImportLogModel>>();
                    await Task.Delay(20000);
                    //await Task.Delay(5000);
                    //completed message
                    var logItem = dalImportLog.GetById(logId.Value, userToken);
                    logItem.Status = TaskStatusEnum.Completed;
                    logItem.Completed = DateTime.UtcNow;
                    logItem.Messages.Add(new ImportLogMessageModel() { Message = "Completed" });
                    await dalImportLog.Update(logItem, userToken);
                }
            });

            //return result async
            return logId.Value;
        }

        public async Task<int> ImportOpcUaNodeSet(List<ImportOPCModel> nodeSetXmlList, UserToken userToken)
        {
            //create a start log item
            var fileNames = string.Join(", ", nodeSetXmlList.Select(f => f.FileName).ToArray<string>());

            //the rest of the fields are set in the dal
            ImportLogModel logItem = new ImportLogModel()
            {
                FileList = nodeSetXmlList.Select(f => f.FileName).ToArray<string>(),
                Messages = new List<ImportLogMessageModel>() {
                    new ImportLogMessageModel() {
                        Message = $"Starting..."
                    }
                }
            };
            var logId = await _dalImportLog.Add(logItem, userToken);

            Task backgroundTask = null;

            //slow task - kick off in background
            //_backgroundWorkerQueue.QueueBackgroundWorkItem(async token =>
            _ = Task.Run(async () =>
            {
                //kick off the importer
                //wrap in scope in the internal method so that we don't lose the scope of the dependency injected objects once the 
                //web api request completes and disposes of the import service object (and its module vars)
                try
                {
                    backgroundTask = ImportOpcUaNodeSetInternal(nodeSetXmlList, logId.Value, userToken);
                    await backgroundTask;
                }
                catch (Exception ex)
                {
                    _logger.LogError(new EventId(), ex, "Unhandled exception in background importer.");
                }
            });

            //return result async
            return logId.Value;
        }



        /// <summary>
        /// Re-factor - Moved this to its own method to be shared by two different endpoints. Only other changes were
        /// returning result message model false instead of badRequest. 
        /// </summary>
        /// <param name="nodeSetXmlList"></param>
        /// <param name="authorToken"></param>
        /// <returns></returns>
        private async Task ImportOpcUaNodeSetInternal(List<ImportOPCModel> nodeSetXmlList, int logId, UserToken userToken)
        {
            var dalImportLog = GetImportLogDalIsolated();

            var sw = Stopwatch.StartNew();
            _logger.LogTrace("Starting import");
            #region CM new code for importing all NodeSet in correct order and with Dependency Resolution
            var fileNames = string.Join(", ", nodeSetXmlList.Select(f => f.FileName).ToArray<string>());
            var filesImportedMsg = $"Importing File{(nodeSetXmlList.Count.Equals(1) ? "" : "s")}: {fileNames}";

            _logger.LogInformation($"ImportService|ImportOpcUaProfile|{filesImportedMsg}. User Id:{userToken}.");

            //wrap in scope so that we don't lose the scope of the dependency injected objects once the 
            //web api request completes and disposes of the import service object (and its module vars)
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                _logger.LogTrace($"Timestamp||ImportId:{logId}||Getting DAL services: {sw.Elapsed}");

                var dalProfile = scope.ServiceProvider.GetService<IDal<Profile, ProfileModel>>();
                var dalNodeSetFile = scope.ServiceProvider.GetService<IDal<NodeSetFile, NodeSetFileModel>>();
                var dalStandardNodeSet = scope.ServiceProvider.GetService<IDal<StandardNodeSet, StandardNodeSetModel>>();
                var dalEngineeringUnits = scope.ServiceProvider.GetService<IDal<EngineeringUnit, EngineeringUnitModel>>();

                var importer = scope.ServiceProvider.GetService<OpcUaImporter>();
                _logger.LogTrace($"Timestamp||ImportId:{logId}||Retrieved DAL services: {sw.Elapsed}");

                if (dalEngineeringUnits.Count(userToken) == 0)
                {
                    await CreateImportLogMessage(dalImportLog, logId, userToken, $"Importing engineering units...<br/>{filesImportedMsg}", TaskStatusEnum.InProgress);
                    await importer.ImportEngineeringUnitsAsync(UserToken.GetGlobalUser(userToken));
                }
                await CreateImportLogMessage(dalImportLog, logId, userToken, $"Validating nodeset files and dependencies...<br/>{filesImportedMsg}", TaskStatusEnum.InProgress);

                //init the warnings object outside the try/catch so that saving warnings happens after conclusion of import.
                //We don't want an execption saving warnings to DB to cause a "failed" import message
                //if something goes wrong on the saving of the warnings to the DB, we handle it outside of the import messages.  
                List<WarningsByNodeSet> nodesetWarnings = new List<WarningsByNodeSet>();

                //wrap the importNodesets for total coverage of exceptions
                //we need to inform the front end of an exception and update the import log on 
                //any failure so it can refresh front end accordingly
                //Todo: Revisit this and limit the try/catch blocks
                try
                {
                    //TODO: Can we pass in authorId (nullable) to the import and then assign it if is passed in. In either case,
                    //assign external author to the value set within the nodeset.
                    //TODO: C2-95: Last parameter should be a setting in the UX if somebody wants to use the Precise NodeSet Version instead of the highest(last) version available
                    //The first parameter can be used to define a custom UANodeSetCache. If null the default FileCache is used
                    //var myNodeSetCache = new OPCUANodeSetHelpers.UANodeSetFileCache();        //FILE CACHE
                    var myNodeSetCache = new UANodeSetDBCache(dalNodeSetFile, dalStandardNodeSet, userToken); // DB CACHE

                    dalProfile.StartTransaction();
                    _logger.LogTrace($"Timestamp||ImportId:{logId}||Importing node set files: {sw.Elapsed}");

                    var nodeSetXmlStringList = nodeSetXmlList.Select(nodeSetXml => nodeSetXml.Data).ToList();
                    var resultSet = UANodeSetImporter.ImportNodeSets(myNodeSetCache, null, nodeSetXmlStringList, false, userToken, _nodeSetResolver);
                    _logger.LogTrace($"Timestamp||ImportId:{logId}||Imported node set files: {sw.Elapsed}");
                    if (!string.IsNullOrEmpty(resultSet.ErrorMessage))
                    {
                        //The UA Importer encountered a crash/error
                        //failed complete message
                        dalProfile.RollbackTransaction();
                        await CreateImportLogMessage(dalImportLog, logId, userToken, resultSet.ErrorMessage + $"<br/>{filesImportedMsg}", TaskStatusEnum.Failed);
                        return;
                    }
                    if (resultSet?.MissingModels?.Count > 0)
                    {
                        //The UA Importer tried to resolve already all missing NodeSet either from Cache or CloudLib but could not find all dependencies
                        //failed complete message
                        dalProfile.RollbackTransaction();
                        var missingModelsText = string.Join(", ", resultSet.MissingModels);
                        await CreateImportLogMessage(dalImportLog, logId, userToken, $"Missing dependent node sets: {missingModelsText}.", TaskStatusEnum.Failed);
                        return;
                    }
                    var profilesAndNodeSets = new List<ProfileModelAndNodeSet>();

                    //This area will be put in an interface that can be used by the Importer (after Friday Presentation)
                    try
                    {
                        _logger.LogTrace($"Timestamp||ImportId:{logId}||Getting standard nodesets files: {sw.Elapsed}");
                        var res = dalStandardNodeSet.GetAll(userToken);
                        _logger.LogTrace($"Timestamp||ImportId:{logId}||Verifying standard nodeset: {sw.Elapsed}");
                        resultSet = UANodeSetValidator.VerifyNodeSetStandard(resultSet, res);
                        //TODO: @Chris - Capture if specific nodeset is not in standard table and report that specifically. Separate that validation
                        //      from potential issues with the import itself.
                        //Chris: Done, but ErrorMessage is only set if there was an issue with the function, NOT if a noodeset was in the standard table.
                        //       If a nodeset is UA Standard, the NameVersion.UAStandardProfileID is set to >0. otherwise its 0.
                        if (!string.IsNullOrEmpty(resultSet?.ErrorMessage))
                        {
                            await CreateImportLogMessage(dalImportLog, logId, userToken, resultSet.ErrorMessage.ToLower() + $"<br/>{filesImportedMsg}", TaskStatusEnum.Failed);
                            return;
                        }

                        //if (myNodeSetCache == null || myNodeSetCache.GetType() != typeof(OPCUANodeSetHelpers.UANodeSetFileCache))
                        {
                            foreach (var tmodel in resultSet.Models)
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
                                if (!profile.NodeSetFiles.Where(m => m.FileName == nsModel.FileName && m.PublicationDate == nsModel.PublicationDate).Any())
                                {
                                    profile.NodeSetFiles.Add(nsModel);
                                }
                                profilesAndNodeSets.Add(new ProfileModelAndNodeSet
                                {
                                    Profile = profile, // TODO use the nodesetfile instead
                                    NodeSetModel = tmodel,
                                });
                            }
                        }

                        await CreateImportLogMessage(dalImportLog, logId, userToken, $"Nodeset files validated.<br/>{filesImportedMsg}", TaskStatusEnum.InProgress);
                    }
                    catch (Exception e)
                    {
                        myNodeSetCache.DeleteNewlyAddedNodeSetsFromCache(resultSet);
                        //log complete message to logger and abbreviated message to user. 
                        _logger.LogCritical(e, $"ImportId:{logId}||ImportService|ImportOpcUaProfile|{e.Message}");
                        //failed complete message
                        dalProfile.RollbackTransaction();
                        await CreateImportLogMessage(dalImportLog, logId, userToken, $"Nodeset validation failed: {e.Message}.<br/>{filesImportedMsg}", TaskStatusEnum.Failed);
                        return;
                    }
                    //To Here
                    #endregion

                    _logger.LogTrace($"Timestamp||ImportId:{logId}||Starting node import: {sw.Elapsed}");
                    await CreateImportLogMessage(dalImportLog, logId, userToken, $"Processing nodeset data...<br/>{filesImportedMsg}", TaskStatusEnum.InProgress);
                    //shield the front end from an exception message. Catch it, log it, and return success is false w/ simplified message
                    try
                    {

                        Dictionary<string, ProfileTypeDefinitionModel> profileItems = new Dictionary<string, ProfileTypeDefinitionModel>();

                        int? result = 0;
                        // TODO Expose in the UI? Feedback from Jonathan if this option is interesting
                        bool requiredTypesOnly = userToken != null; // Only import profiles for types actually used by the node set
                        Task primeEFCacheTask = null;
                        var startEFCache = sw.Elapsed;
                        if (true)
                        {
                            var profileIds = profilesAndNodeSets.Select(pn => pn.Profile.ID).Where(i => (i ?? 0) != 0);
                            _logger.LogTrace($"Timestamp||ImportId:{logId}||Loading EF cache: {sw.Elapsed}");
                            primeEFCacheTask = importer._dal.LoadIntoCacheAsync(pt => profileIds.Contains(pt.ProfileId));
                            primeEFCacheTask = primeEFCacheTask.ContinueWith((t) => importer._dtDal.LoadIntoCacheAsync(dt => profileIds.Contains(dt.CustomType.ProfileId))).Unwrap();
                        }
                        var modelsToImport = new List<NodeSetModel>();
                        foreach (var profileAndNodeSet in profilesAndNodeSets)
                        {
                            //only show message for the items which are newly imported...
                            if (profileAndNodeSet.NodeSetModel.NewInThisImport)
                            {
                                await CreateImportLogMessage(dalImportLog, logId, userToken, $"Processing nodeset file: {profileAndNodeSet.NodeSetModel.NameVersion}...", TaskStatusEnum.InProgress);
                            }
                            var logList = new List<string>();
                            (importer.Logger as LoggerCapture).LogList = logList;

                            var nodeSetModels = await importer.LoadNodeSetAsync(profileAndNodeSet.NodeSetModel.NodeSet, profileAndNodeSet.Profile, !profileAndNodeSet.NodeSetModel.NewInThisImport);
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

                                        var items = await importer.ImportNodeSetModelAsync(model, userToken);
                                        if (items != null)
                                        {
                                            foreach (var item in items)
                                            {
                                                profileItems[item.Key] = item.Value;
                                            }
                                        }
                                        // Uncomment to test nodesetmodel fidelity vs. profile import/export
                                        //string xmlNodeSet = null;
                                        //using (var xmlNodeSetStream = new MemoryStream())
                                        //{
                                        //    if (importer.ExportNodeSet(profileAndNodeSet.Profile, xmlNodeSetStream, userToken, null))
                                        //    {
                                        //        xmlNodeSet = Encoding.UTF8.GetString(xmlNodeSetStream.ToArray());
                                        //    }
                                        //}
                                        //File.WriteAllText($"{profileAndNodeSet.Profile.Namespace.Replace("http://", "").Replace("/", ".")}reexported.xml", xmlNodeSet);
                                    }
                                    (importer.Logger as LoggerCapture).LogList = null;
                                    if (logList.Any())
                                    {
                                        nodesetWarnings.Add(new WarningsByNodeSet()
                                        { ProfileId = profileAndNodeSet.Profile.ID.Value, Key = profileAndNodeSet.Profile.ToString(), Warnings = logList });
                                        //nodesetWarnings[profileAndNodeSet.Profile.ToString()] = logList;
                                    }
                                }
                            }
                        }

                        if (profileItems.Any())
                        {
                            result = profileItems.Last().Value.ID;
                        }
                        //foreach(var profileItem in profileItems)
                        //{
                        //    result = await ImportInternalAsync(profileItem);
                        //}

                        result = 1; // TOD: OPC imported profiles don't get a profiletype when being read back for some reason
                        sw.Stop();
                        var elapsed = sw.Elapsed;
                        var elapsedMsg = $"{ elapsed.Minutes }:{ elapsed.Seconds} (min:sec)";
                        _logger.LogTrace($"Timestamp||ImportId:{logId}||Import time: {elapsedMsg}, Files: {fileNames} "); //use warning so it shows in app log in db

                        //return success message object
                        filesImportedMsg = $"Imported File{(nodeSetXmlList.Count.Equals(1) ? "" : "s")}: {fileNames}";
                        await CreateImportLogMessage(dalImportLog, logId, userToken, $"{filesImportedMsg}", TaskStatusEnum.Completed);
                    }
                    catch (Exception e)
                    {
                        sw.Stop();
                        var elapsed2 = sw.Elapsed;
                        var elapsedMsg2 = $"{ elapsed2.Minutes }:{ elapsed2.Seconds} (min:sec)";
                        _logger.LogWarning($"Timestamp||ImportId:{logId}||Import time before failure: {elapsedMsg2}, Files: {fileNames} "); //use warning so it shows in app log in db
                                                                                                                                            //log complete message to logger and abbreviated message to user. 
                        _logger.LogCritical(e, $"ImportId:{logId}||ImportService|ImportOpcUaProfile|{e.Message}");
                        //TBD - once we stabilize, take out the specific exception message returned to user because user should not see a code message.
                        dalProfile.RollbackTransaction();
                        var message = e.InnerException != null ? e.InnerException.Message : e.Message;
                        await CreateImportLogMessage(GetImportLogDalIsolated(), logId, userToken, $"An error occurred during the import: {message}.<br/>{filesImportedMsg}", TaskStatusEnum.Failed);
                        //return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical($"ImportId:{logId}||ImportOpcUaNodeSet error", ex);
                    dalProfile.RollbackTransaction();
                    await CreateImportLogMessage(GetImportLogDalIsolated(), logId, userToken, $"An error occurred during the import: {ex.Message}.<br/>{filesImportedMsg}", TaskStatusEnum.Failed);
                    //return;
                }

                //handle import warnings. Save to DB for each nodeset / profile.
                //Store for later use when we export profile. 
                try
                {
                    foreach (var warningList in nodesetWarnings)
                    {
                        //save each nodesets warnings to the DB...for display upon export
                        //don't show a warning message on the import ui at this point.
                        await CreateImportLogWarnings(dalImportLog, logId, warningList, userToken);
                        //var msgCount = warningList.Warnings.Count == 1 ? "There is 1 warning." : $"There are {warningList.Warnings.Count} warnings.";
                        //filesImportedMsg += $"\r\nWarning: Some data in nodeset {warningList.Key} is not supported by this editor and will be lost if exported. {msgCount}";
                    }
                }
                catch (Exception ex)
                {
                    var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    _logger.LogCritical($"ImportId:{logId}||ImportOpcUaNodeSet||Save Import Profile Warnings||error||{message}", ex);
                }

            } //end createScope using

        }

        /// <summary>
        /// The import happens in a transaction within the context shared by all
        /// during the scope of the request. 
        /// Create and isolate a 2nd context outside the scope of the main context and submit 
        /// log messages to it. 
        /// </summary>
        /// <returns></returns>
        private IDal<ImportLog, ImportLogModel> GetImportLogDalIsolated()
        {
            var connString = _configuration.GetConnectionString("ProfileDesignerDB");
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Data.Contexts.ProfileDesignerPgContext>()
                .UseNpgsql(connString)
                .Options;
            var dbContextLogging = new Data.Contexts.ProfileDesignerPgContext(options);
            Data.Repositories.IRepository<ImportLog> repo =
                new Data.Repositories.BaseRepo<ImportLog, Data.Contexts.ProfileDesignerPgContext>(dbContextLogging, _configuration);
            return new ImportLogDAL(repo);
        }

        private async Task CreateImportLogMessage(IDal<ImportLog, ImportLogModel> dalImportLog, int logId, UserToken userToken,
            string message, TaskStatusEnum status)
        {
            var logItem = dalImportLog.GetById(logId, userToken);
            logItem.Status = status;
            if (status == TaskStatusEnum.Failed || status == TaskStatusEnum.Cancelled || status == TaskStatusEnum.Completed)
            {
                logItem.Completed = DateTime.UtcNow;
            }
            logItem.Messages.Add(new ImportLogMessageModel() { Message = message });
            await dalImportLog.Update(logItem, userToken);
        }

        /// <summary>
        /// Take a list of warnings and save them all in one step to the DB.
        /// </summary>
        private async Task CreateImportLogWarnings(IDal<ImportLog, ImportLogModel> dalImportLog, int logId, WarningsByNodeSet warningsList, UserToken userToken)
        {
            var logItem = dalImportLog.GetById(logId, userToken);
            if (logItem.ProfileWarnings == null) logItem.ProfileWarnings = new List<ImportProfileWarningModel>();
            foreach (var message in warningsList.Warnings)
            {
                logItem.ProfileWarnings.Add(new ImportProfileWarningModel() { Message = message, ProfileId = warningsList.ProfileId });
            }
            await dalImportLog.Update(logItem, userToken);
        }

        private class ProfileModelAndNodeSet
        {
            public ProfileModel Profile { get; set; }
            public ModelValue NodeSetModel { get; set; }
        }

        private class WarningsByNodeSet
        {
            public int ProfileId { get; set; }
            public string Key { get; set; }
            public List<string> Warnings { get; set; }
        }
    }


}
