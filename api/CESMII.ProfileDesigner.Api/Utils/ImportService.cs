using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.OpcUa;

namespace CESMII.ProfileDesigner.Api.Utils
{
    public class ImportService
    {
        private readonly ILogger<ImportService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IDal<ImportLog, ImportLogModel> _dalImportLog;
        private readonly IConfiguration _configuration;
        private readonly ImportNotificationUtil _importNotifyUtil;

        public ImportService(IServiceScopeFactory serviceScopeFactory,
            IDal<ImportLog, ImportLogModel> dalImportLog,
            ILogger<ImportService> logger,
            IConfiguration configuration,
            ImportNotificationUtil importNotifyUtil)

        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _dalImportLog = dalImportLog;
            _configuration = configuration;
            _importNotifyUtil = importNotifyUtil;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Preserve backward compatability where we generate the import item within this service.
        /// The newer approach to support large files does some advanced processing and prepares import item in advance</remarks>
        /// <returns></returns>
        public async Task<int> ImportOpcUaNodeSet(List<ImportOPCModel> nodeSetXmlList, ImportUserModel userInfo, bool allowMultiVersion, bool upgradePreviousVersions)
        {
            //the rest of the fields are set in the dal
            var importItem = new ImportLogModel()
            {
                Files = nodeSetXmlList.Select(f => new ImportFileModel()
                {
                    FileName = f.FileName,
                    TotalBytes = (long)f.Data.Length,
                    TotalChunks = 1
                }).ToList(),
                Messages = new List<ImportLogMessageModel>() {
                    new ImportLogMessageModel() {
                        Message = $"Starting..."
                    }
                }
            };

            var logId = await _dalImportLog.AddAsync(importItem, userInfo.UserToken);

            return ImportOpcUaNodeSet(importItem.ID.Value, nodeSetXmlList, userInfo, allowMultiVersion, upgradePreviousVersions);
        }

        public int ImportOpcUaNodeSet(int importId, List<ImportOPCModel> nodeSetXmlList, ImportUserModel userInfo, bool allowMultiVersion, bool upgradePreviousVersions)
        {

            //slow task - kick off in background
            _ = Task.Run(async () =>
            {
                //kick off the importer
                //wrap in scope in the internal method so that we don't lose the scope of the dependency injected objects once the 
                //web api request completes and disposes of the import service object (and its module vars)
                try
                {
                    var backgroundTask = ImportOpcUaNodeSetInternal(nodeSetXmlList, importId, userInfo, allowMultiVersion, upgradePreviousVersions);
                    await backgroundTask;
                }
                catch (Exception ex)
                {
                    _logger.LogError(new EventId(), ex, "Unhandled exception in background importer.");
                    //update import log to indicate unexpected failure
                    var dalImportLog = GetImportLogDalIsolated();
                    await CreateImportLogMessage(_importNotifyUtil, dalImportLog, importId, userInfo, "Unhandled exception in background importer.", TaskStatusEnum.Failed);
                }
            });

            //return result async
            return importId;
        }



        /// <summary>
        /// Re-factor - Moved this to its own method to be shared by two different endpoints. Only other changes were
        /// returning result message model false instead of badRequest. 
        /// </summary>
        /// <param name="nodeSetXmlList"></param>
        /// <param name="authorToken"></param>
        /// <returns></returns>
        private async Task ImportOpcUaNodeSetInternal(List<ImportOPCModel> nodeSetXmlList, int logId, ImportUserModel userInfo, bool allowMultiVersion, bool upgradePreviousVersions)
        {
            var dalImportLog = GetImportLogDalIsolated();

            //wrap in scope so that we don't lose the scope of the dependency injected objects once the 
            //web api request completes and disposes of the import service object (and its module vars)
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                _logger.LogTrace($"Timestamp||ImportId:{logId}||Getting DAL services");

                //var dalProfile = scope.ServiceProvider.GetService<IDal<Profile, ProfileModel>>();
                //var dalNodeSetFile = scope.ServiceProvider.GetService<IDal<NodeSetFile, NodeSetFileModel>>();
                //var dalStandardNodeSet = scope.ServiceProvider.GetService<IDal<StandardNodeSet, StandardNodeSetModel>>();
                //var dalEngineeringUnits = scope.ServiceProvider.GetService<IDal<EngineeringUnit, EngineeringUnitModel>>();

                var importNotifyUtil = scope.ServiceProvider.GetService<ImportNotificationUtil>();
                var importer = scope.ServiceProvider.GetService<OpcUaImporter>();
                _logger.LogTrace($"Timestamp||ImportId:{logId}||Retrieved DAL services");

                var nodesetWarnings = await importer.ImportUaNodeSets(nodeSetXmlList, userInfo.UserToken,
                    async (message, status) =>
                    {
                        await CreateImportLogMessage(importNotifyUtil, dalImportLog, logId, userInfo, message, status);
                    },
                    logId, allowMultiVersion, upgradePreviousVersions);

                if (nodesetWarnings != null)
                {
                    //handle import warnings. Save to DB for each nodeset / profile.
                    //Store for later use when we export profile. 
                    try
                    {
                        foreach (var warningList in nodesetWarnings)
                        {
                            //save each nodesets warnings to the DB...for display upon export
                            //don't show a warning message on the import ui at this point.
                            await CreateImportLogWarnings(dalImportLog, logId, warningList, userInfo.UserToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        _logger.LogCritical($"ImportId:{logId}||ImportOpcUaNodeSet||Save Import Profile Warnings||error||{message}", ex);
                    }
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
        public IDal<ImportLog, ImportLogModel> GetImportLogDalIsolated()
        {
            var connString = _configuration.GetConnectionString("ProfileDesignerDB");
            var options = new DbContextOptionsBuilder<Data.Contexts.ProfileDesignerPgContext>()
                .UseNpgsql(connString)
                .Options;
            var dbContextLogging = new Data.Contexts.ProfileDesignerPgContext(options);
            Data.Repositories.IRepository<ImportLog> repo =
                new Data.Repositories.BaseRepo<ImportLog, Data.Contexts.ProfileDesignerPgContext>(dbContextLogging, _configuration);
            return new ImportLogDAL(repo);
        }

        private async Task CreateImportLogMessage(ImportNotificationUtil importNotifyUtil,
            IDal<ImportLog, ImportLogModel> dalImportLog,
            int logId,
            ImportUserModel userInfo,
            string message,
            TaskStatusEnum status)
        {
            var logItem = dalImportLog.GetById(logId, userInfo.UserToken);
            logItem.Status = status;
            if (status == TaskStatusEnum.Failed || status == TaskStatusEnum.Cancelled || status == TaskStatusEnum.Completed)
            {
                logItem.Completed = DateTime.UtcNow;
            }
            logItem.Messages.Add(new ImportLogMessageModel() { Message = message });
            await dalImportLog.UpdateAsync(logItem, userInfo.UserToken);

            //send notification after update completes so that an email error does not prevent import completion
            if (logItem.NotifyOnComplete &&
                (status == TaskStatusEnum.Failed || status == TaskStatusEnum.Cancelled || status == TaskStatusEnum.Completed))
            {
                await importNotifyUtil.SendEmailNotification(logItem, userInfo.User);
            }
        }

        /// <summary>
        /// Take a list of warnings and save them all in one step to the DB.
        /// </summary>
        private static async Task CreateImportLogWarnings(IDal<ImportLog, ImportLogModel> dalImportLog, int logId, OpcUaImporter.WarningsByNodeSet warningsList, UserToken userToken)
        {
            var logItem = dalImportLog.GetById(logId, userToken);
            if (logItem.ProfileWarnings == null) logItem.ProfileWarnings = new List<ImportProfileWarningModel>();
            foreach (var message in warningsList.Warnings)
            {
                logItem.ProfileWarnings.Add(new ImportProfileWarningModel() { Message = message, ProfileId = warningsList.ProfileId });
            }
            await dalImportLog.UpdateAsync(logItem, userToken);
        }

        //private sealed class ProfileModelAndNodeSet
        //{
        //    public ProfileModel Profile { get; set; }
        //    public ModelValue NodeSetModel { get; set; }
        //}

        //private sealed class WarningsByNodeSet
        //{
        //    public int ProfileId { get; set; }
        //    public string Key { get; set; }
        //    public List<string> Warnings { get; set; }
        //}
    }
}
