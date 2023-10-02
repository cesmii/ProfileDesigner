using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;

using Azure.Storage.Queues;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.Common;
using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Entities;

namespace CESMII.ProfileDesigner.Importer
{
    public interface IImportWrapper
    {
        Task<ResultMessageModel> ProcessCloudImport(int importId, UserToken userToken, ImportQueueCloudModel model);
        Task<ResultMessageModel> ProcessFileImport(int importId, UserToken userToken, ImportQueueModel model);
    }

    
    /// <summary>
    /// Wrapper class to call the code to execute the import. 
    /// In localhost and test, this calls the import service directly. 
    /// In stage and prod, this calls the Azure function which 
    /// in turn calls the import service. 
    /// </summary>
    public class ImportFunctionWrapper: IImportWrapper
    {
        private readonly ILogger<ImportFunctionWrapper> _logger;
        protected readonly IHttpClientFactory _httpClientFactory;
        private readonly ConfigUtil _configUtil;
        private readonly IDal<ImportLog, ImportLogModel> _dalImportLog;

        public ImportFunctionWrapper(ILogger<ImportFunctionWrapper> logger, 
            IHttpClientFactory httpClientFactory, 
            ConfigUtil configUtil,
            IDal<ImportLog, ImportLogModel> dalImportLog
            ) {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configUtil = configUtil;
            _dalImportLog = dalImportLog;
        }
 
        public async Task<ResultMessageModel> ProcessCloudImport(
            int importId, UserToken userToken, ImportQueueCloudModel model)
        {

            var queueData = JsonConvert.SerializeObject(model);
            var item = await _dalImportLog.GetByIdAsync(importId, userToken);
            item.QueueData = queueData;
            await _dalImportLog.UpdateAsync(item, userToken);
            var data = JsonConvert.SerializeObject(
                new ImportJobDataModel() { ID = importId, UserToken = userToken, ImportSource = ImportSourceEnum.Cloud });
            //add the body as JSON content
            var body = new StringContent(data);

            return await ProcessImportAzureFunction(_configUtil.ImportSettings.ImportCloudFunctionUrl,
                body, model.BearerToken);
        }

        public async Task<ResultMessageModel> ProcessFileImport(
            int importId, UserToken userToken, ImportQueueModel model)
        {
            var queueData = JsonConvert.SerializeObject(model);
            var item = await _dalImportLog.GetByIdAsync(importId, userToken);
            item.QueueData = queueData;
            await _dalImportLog.UpdateAsync(item, userToken);
            var data = JsonConvert.SerializeObject(
                new ImportJobDataModel() { ID = importId, UserToken = userToken, ImportSource = ImportSourceEnum.File });
            //add the body as JSON content
            var body = new StringContent(data);

            return await ProcessImportAzureFunction(_configUtil.ImportSettings.ImportFileFunctionUrl,
                body, model.BearerToken);
        }

        private async Task<ResultMessageModel> ProcessImportAzureFunction(
            string functionUrl, 
            HttpContent body, 
            string bearerToken)
        {
            //call Azure function. 
            //get base url from config, get function specific endpoint from config
            //build header - include user info, keys, etc. 
            //build parameters into body
            //make call
            //make the API call
            HttpClient client = _httpClientFactory.CreateClient();
            try
            {
                client.BaseAddress = new Uri(_configUtil.ImportSettings.AzureFunctionBaseUrl);

                //prevent 403 forbidden error - this is a workaround. Re-visit this and ask H&M to grant us permission.
                //client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "...");

                // Add an Accept header for JSON format. "application/json"
                client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

                //prepare the request
                using (var requestMessage = new HttpRequestMessage
                    (HttpMethod.Post, functionUrl))
                {
                    //add the body as JSON content
                    requestMessage.Content = body;

                    //Add specific headers to ensure proper access and authorization
                    requestMessage.Headers.Add
                        ("x-functions-clientid", _configUtil.ImportSettings.AzureFunctionClientKey);
                    //TBD - create or re-use bearer token from original request
                    requestMessage.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", bearerToken);

                    HttpResponseMessage response = await client.SendAsync(requestMessage);

                    if (response.IsSuccessStatusCode)
                    {
                        // Parse the response body.
                        var resData = response.Content.ReadAsStringAsync().Result;  //Make sure to add a reference to System.Net.Http.Formatting.dll
                        var result = JsonConvert.DeserializeObject<ResultMessageModel>(resData);

                        if (!result.IsSuccess)
                        {
                            _logger.Log(LogLevel.Error, $"ImportFunctionWrapper|ProcessImportAzure|Url:{requestMessage.RequestUri.ToString()}|Import process failed: {result.Message}");
                            //we capture the raw message in the db, now simplify the error message
                            //result.Message = "If this error continues, please contact the system administrator.";
                        }
                        else
                        {
                            _logger.Log(LogLevel.Information, $"ImportFunctionWrapper|ProcessImportAzure|Import Process started.");
                        }

                        return result;
                    }
                    else
                    {
                        _logger.Log(LogLevel.Error, $"ImportFunctionWrapper|ProcessImportAzure|Url:{requestMessage.RequestUri.ToString()}|Unexpected Response: {response.StatusCode}::{response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, $"|Url:{_configUtil.ImportSettings.AzureFunctionBaseUrl}{functionUrl}|An error occurred starting the import process:{ex.Message}");
                throw;
            }
            finally
            {
                // Dispose once all HttpClient calls are complete. This is not necessary if the containing object will be disposed of; for example in this case the HttpClient instance will be disposed automatically when the application terminates so the following call is superfluous.
                client.Dispose();
            }
            return null;
        }
    }
    

    /// <summary>
    /// Wrapper class to call the code to execute the import. 
    /// In localhost and test, this calls the import service directly. 
    /// In stage and prod, this calls the Azure function which 
    /// in turn calls the import service. 
    /// </summary>
    public class ImportWebJobWrapper: IImportWrapper
    {
        private readonly ILogger<ImportWebJobWrapper> _logger;
        private readonly ConfigUtil _configUtil;
        private readonly IDal<ImportLog, ImportLogModel> _dalImportLog;

        public ImportWebJobWrapper(ILogger<ImportWebJobWrapper> logger,
            ConfigUtil configUtil,
            IDal<ImportLog, ImportLogModel> dalImportLog)
        {
            _logger = logger;
            _configUtil = configUtil;
            _dalImportLog = dalImportLog;
        }

        /// <summary>
        /// Submit import request to AzureWebJobsStorage. 
        /// Azure WebJob will be monitoring queue and pick up the job to process 
        /// when a message arrives. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<ResultMessageModel> ProcessCloudImport(
            int importId, UserToken userToken, ImportQueueCloudModel model)
        {
            //add the body as JSON content string
            var queueData = JsonConvert.SerializeObject(model);
            var item = await _dalImportLog.GetByIdAsync(importId, userToken);
            item.QueueData = queueData;
            await _dalImportLog.UpdateAsync(item, userToken);
            var data = JsonConvert.SerializeObject(
                new ImportJobDataModel() { ID = importId, UserToken = userToken, ImportSource = ImportSourceEnum.Cloud });
            return await SendMessageToQueue(data);
        }

        /// <summary>
        /// Submit import request to AzureWebJobsStorage. 
        /// Azure WebJob will be monitoring queue and pick up the job to process 
        /// when a message arrives. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<ResultMessageModel> ProcessFileImport(
            int importId, UserToken userToken, ImportQueueModel model)
        {
            //add the body as JSON content string
            var queueData = JsonConvert.SerializeObject(model);
            var item = await _dalImportLog.GetByIdAsync(importId, userToken);
            item.QueueData = queueData;
            await _dalImportLog.UpdateAsync(item, userToken);
            var data = JsonConvert.SerializeObject(
                new ImportJobDataModel() { ID = importId, UserToken = userToken, ImportSource = ImportSourceEnum.File });
            return await SendMessageToQueue(data);
        }

        /// <summary>
        /// Submit import request to AzureWebJobsStorage. 
        /// Azure WebJob will be monitoring queue and pick up the job to process 
        /// when a message arrives. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<ResultMessageModel> SendMessageToQueue(string msg)
        {
            QueueClient client = new QueueClient(_configUtil.ImportSettings.AzureWebJobsStorage,
                _configUtil.ImportSettings.AzureWebJobsStorageQueueName);
            var msgBytes = System.Text.Encoding.UTF8.GetBytes(msg);
            var result = await client.SendMessageAsync(System.Convert.ToBase64String(msgBytes));
            _logger.Log(LogLevel.Information,
                $"AzureWebJobSendMessage|SendMessage|{_configUtil.ImportSettings.AzureWebJobsStorageQueueName}|Message Id: {result.Value.MessageId}.");

            return new ResultMessageModel()
            {
                IsSuccess = !string.IsNullOrEmpty(result.Value.MessageId),
                Message = "Import is processing..."
            };
        }
    }

    /// <summary>
    /// Wrapper class to call the code to execute the import. 
    /// In localhost and test, this calls the import service directly. 
    /// In stage and prod, this calls the Azure function which 
    /// in turn calls the import service. 
    /// </summary>
    public class ImportDirectWrapper
    {
        private readonly ILogger<ImportDirectWrapper> _logger;
        private readonly ICloudLibDal<CloudLibProfileModel> _cloudLibDal;
        private readonly ImportService _svcImport;

        public ImportDirectWrapper(ILogger<ImportDirectWrapper> logger,
            ICloudLibDal<CloudLibProfileModel> cloudLibDal,
            ImportService svcImport)
        {
            _logger = logger;
            _cloudLibDal = cloudLibDal;
            _svcImport = svcImport;
        }

        /// <summary>
        /// Call this from an Azure function endpoint or the API endpoint
        /// This will take the parameters, grab the calling user info, prepare the data
        /// and either execute the import call directly in a background task
        /// Or, if called from an Azure function, it will kick off the import in process but 
        /// not wait for completion to return the result value to the calling user. 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="user"></param>
        /// <param name="userToken"></param>
        /// <param name="allowMultiVersion"></param>
        /// <param name="upgradePreviousVersions"></param>
        /// <param name="isAsync"></param>
        /// <returns></returns>
        public async Task<ResultMessageWithDataModel> ProcessCloudImport(
            int importId,
            List<IdStringModel> model,
            UserModel user,
            UserToken userToken,
            bool allowMultiVersion,
            bool upgradePreviousVersions,
            bool isAsync = true)
        {
            List<ImportOPCModel> importModels = new();
            foreach (var modelId in model.Select(m => m.ID))
            {
                try
                {
                    var nodeSetToImport = await _cloudLibDal.DownloadAsync(modelId);
                    if (nodeSetToImport == null)
                    {
                        _logger.LogWarning($"ImportDirectWrapper|ProcessCloudImport|Did not find nodeset in Cloud Library: {modelId}.");
                        return new ResultMessageWithDataModel()
                        {
                            IsSuccess = false,
                            Message = "NodeSet not found in Cloud Library."
                        };
                    }
                    var importModel = new ImportOPCModel
                    {
                        Data = nodeSetToImport.NodesetXml,
                        FileName = nodeSetToImport.Namespace,
                        CloudLibraryId = nodeSetToImport.CloudLibraryId,
                    };
                    importModels.Add(importModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"ImportDirectWrapper|ProcessCloudImport|Failed to download from Cloud Library: {modelId} {ex.Message}.");
                    return new ResultMessageWithDataModel()
                    {
                        IsSuccess = false,
                        Message = "Error downloading NodeSet from Cloud Library."
                    };
                }
            }

            //kick off background process, logid is returned immediately so front end can track progress...
            var userInfo = new ImportUserModel() { User = user, UserToken = userToken };
            await _svcImport.PrepareImportLogForCloudImport(importId, importModels, userInfo);

            //call the existing import code for cloud library import flow. 
            //pass in the author id as current user
            if (isAsync)
            {
                //kick off background process, logid is returned immediately so front end can track progress...
                await _svcImport.ImportOpcUaNodeSetAsync(importId, importModels, userInfo, allowMultiVersion, upgradePreviousVersions);
            }
            else
            {
                //kick off in process, logid is returned immediately so front end can track progress...
                _svcImport.ImportOpcUaNodeSetInProcess(importId, importModels, userInfo, allowMultiVersion, upgradePreviousVersions);
            }

            return new ResultMessageWithDataModel()
            {
                IsSuccess = true,
                Message = "Import is processing...",
                Data = importId
            };
        }
    }
}
