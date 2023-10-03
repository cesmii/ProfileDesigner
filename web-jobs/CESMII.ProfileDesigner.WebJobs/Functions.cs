using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;

using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.Importer;
using CESMII.ProfileDesigner.Importer.Utils;
using CESMII.ProfileDesigner.Common.Enums;

namespace CESMII.ProfileDesigner.WebJobs
{
    [Filters.FunctionErrorHandler]
    public class Functions
    {
        private readonly ImportDirectWrapper _importWrapper;
        private readonly ImportService _svcImport;
        private readonly IDal<ImportLog, ImportLogModel> _dalImportLog;
        private readonly ImportNotificationUtil _importNotifyUtil;
        private readonly UserDAL _dalUser;
        private readonly ILogger<Functions> _logger;

        public Functions(
            ImportDirectWrapper importWrapper,
            ImportService svcImport,
            IDal<ImportLog, ImportLogModel> dalImportLog,
            ImportNotificationUtil importNotifyUtil,
            UserDAL dalUser,
            ILogger<Functions> logger
        )
        {
            _importWrapper = importWrapper;
            _svcImport = svcImport;
            _dalImportLog = dalImportLog;
            _importNotifyUtil = importNotifyUtil;
            _dalUser = dalUser;
            _logger = logger;
        }

        /*
        [NoAutomaticTrigger]
        public static void CreateQueueMessage(
        ILogger logger,
        string value,
        [Queue("outputqueue")] out string message)
        {
            message = value;
            logger.LogInformation("Creating queue message: ", message);
        }
        */
        /*
        [FunctionName("ProcessImportCloud1")]
        public void ProcessImportCloud1(
            [QueueTrigger("z-import-queue")] string msg,
            ILogger logger)
        {
            //call import, process file
            logger.LogInformation("WebJob|ProcessImportCloud|import-queue|handle message.");
        }
        */


        [FunctionName("TestWebJob")]
        public void TestWebJob(
            [QueueTrigger("test-queue")] string msg,
            ILogger logger)
        {
            //call import, process file
            logger.LogInformation("WebJob|TestWebJob|test-queue|handle message.");

            logger.LogInformation($"Raw data: {msg}");

            //tbd - cycle through config values and confirm presence of settings

            //log that import completed
            logger.LogInformation("WebJob|TestWebJob|test-queue|complete.");
        }


        [FunctionName("TestEmail")]
        public async Task TestEmail(
            [QueueTrigger("import-email-queue")] string msg,
            ILogger logger)
        {
            //call import, process file
            logger.LogInformation("WebJob|TestEmail|import-email-queue|handle message.");

            logger.LogInformation($"Raw data: {msg}");

            //send an email to a recipient with the status information from the import
            ImportJobDataModel model = JsonConvert.DeserializeObject<ImportJobDataModel>(msg);
            var item = await _dalImportLog.GetByIdAsync(model.ID, model.UserToken);
            var user = await _dalUser.GetByIdAsync(model.UserToken.UserId, model.UserToken);
            await _importNotifyUtil.SendEmailNotification(item, user);

            //log that import completed
            logger.LogInformation("WebJob|TestWebJob|test-queue|complete.");
        }


        [FunctionName("ProcessImport")]
        public async Task ProcessImport(
            [QueueTrigger("import-queue")] string msg,
            ILogger logger)
        {
            //call import, process file
            logger.LogInformation("WebJob|ProcessImport|import-queue|handle message.");

            if (string.IsNullOrEmpty(msg))
            {
                logger.LogError($"WebJob|ProcessImport|import-queue|Msg is null or empty");
                throw new ArgumentNullException(nameof(msg));
            }

            logger.LogTrace($"Raw data:");
            logger.LogTrace(msg);

            //convert to JSON object
            try
            {
                ImportJobDataModel model = JsonConvert.DeserializeObject<ImportJobDataModel>(msg);

                //do the actual processing and importing
                if (model.ImportSource == ImportSourceEnum.Cloud)
                {
                    await ProcessCloudImport(model);
                    return;
                }
                else if (model.ImportSource == ImportSourceEnum.File)
                {
                    await ProcessFileImport(model);
                    return;
                }
                else
                {
                    logger.LogError($"WebJob|ProcessImport|invalid import source: {model.ImportSource.ToString()}");
                    throw new NotSupportedException("ImportSource");
                }
            }
            catch (Exception exD) when (exD is JsonReaderException || exD is JsonSerializationException)
            {
                logger.LogError(exD, $"WebJob|ProcessImport|Error deserializing data: {exD.Message}. Data: {msg}");
            }
            //log that import completed
            logger.LogInformation($"WebJob|ProcessImport|Import Completed|Data: {msg}");
        }

        private async Task ProcessCloudImport(ImportJobDataModel model)
        {
            //rehydrate import item
            var item = await _dalImportLog.GetByIdAsync(model.ID, model.UserToken);

            //call import, process file
            try
            {
                //extract queue data, then use info for processing
                var queueData = JsonConvert.DeserializeObject<ImportQueueCloudModel>(item.QueueData);

                //get user
                var user = await _dalUser.GetByIdAsync(model.UserToken.UserId, model.UserToken);

                //call the existing import code. 
                //pass in the author id as current user
                //kick off background process, logid is returned immediately so front end can track progress...
                var result = _importWrapper.ProcessCloudImport(model.ID, queueData.Items, user, model.UserToken, queueData.AllowMultiVersion, queueData.UpgradePreviousVersions, isAsync:false ).Result;
            }
            catch (Exception exD) when (exD is JsonReaderException || exD is JsonSerializationException)
            {
                _logger.LogError(exD, $"WebJob|ProcessCloudImport|Error deserializing data: {exD.Message}. Data: {item.QueueData}");
            }
            catch (ImportException exI)
            {
                _logger.LogError(exI, $"WebJob|ProcessCloudImport|Import Failed|Import Id: {item.ID}, {exI.Message}");
                await ImportUtils.HandleImportException(exI, item, model.UserToken, _dalImportLog);
            }
        }

        private async Task ProcessFileImport(ImportJobDataModel model)
        {
            //rehydrate import item
            var item = await _dalImportLog.GetByIdAsync(model.ID, model.UserToken);

            //call import, process file
            try
            {
                //extract queue data, then use info for processing
                var queueData = JsonConvert.DeserializeObject<ImportQueueModel>(item.QueueData);

                //get user
                var user = await _dalUser.GetByIdAsync(model.UserToken.UserId, model.UserToken);

                //get the list of files in the import and reassemble into merged files for import
                var fileNames = item.Files == null ? "[empty]" : string.Join(", ", item.Files.Select(x => x.FileName).ToArray());
                var importItems = ImportUtils.MergeChunkedFiles(item, _logger);

                //call the existing import code. 
                //pass in the author id as current user
                //kick off background process, logid is returned immediately so front end can track progress...
                var userInfo = new ImportUserModel() { User = user, UserToken = model.UserToken };
                _svcImport.ImportOpcUaNodeSetInProcess(item.ID.Value, importItems, userInfo, queueData.AllowMultiVersion, queueData.UpgradePreviousVersions);
            }
            catch (Exception exD) when (exD is JsonReaderException || exD is JsonSerializationException)
            {
                _logger.LogError(exD, $"WebJob|ProcessFileImport|Error deserializing queue data: {exD.Message}. Data: {item.QueueData}");
            }
            catch (ImportException exI)
            {
                _logger.LogError(exI, $"WebJob|ProcessFileImport|Import Failed|Import Id: {item.ID}, {exI.Message}");
                await ImportUtils.HandleImportException(exI, item, model.UserToken, _dalImportLog);
            }

        }
    }
}

