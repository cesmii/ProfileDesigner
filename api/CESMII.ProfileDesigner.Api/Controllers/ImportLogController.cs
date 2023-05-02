using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using CESMII.ProfileDesigner.Common;
using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.Api.Shared.Controllers;
using CESMII.ProfileDesigner.Api.Shared.Models;

namespace CESMII.ProfileDesigner.Api.Controllers
{
    [Authorize(Policy = nameof(PermissionEnum.UserAzureADMapped)), Route("api/[controller]")]
    public class ImportLogController : BaseController<ImportLogController>
    {
        private readonly IDal<ImportLog, ImportLogModel> _dal;
        private readonly Utils.ImportService _svcImport;

        public ImportLogController(IDal<ImportLog, ImportLogModel> dal, UserDAL dalUser,
            Utils.ImportService svcImport,
            ConfigUtil config, ILogger<ImportLogController> logger)
            : base(config, logger, dalUser)
        {
            _dal = dal;
            _svcImport = svcImport;
        }

        [HttpPost, Route("GetByID")]
        //[ProducesResponseType(200, Type = typeof(NodeSetModel))]
        [ProducesResponseType(200, Type = typeof(ImportLogModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByID([FromBody] IdIntModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ImportLogController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }
            var result = _dal.GetById(model.ID, base.DalUserToken);
            if (result == null)
            {
                _logger.LogWarning($"ImportLogController|GetById|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            return Ok(result);
        }


        /// <summary>
        /// Get my import logs
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Mine")]
        [ProducesResponseType(200, Type = typeof(DALResult<ProfileModel>))]
        public IActionResult GetMine([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("ImportLogController|GetMine|Invalid model.");
                return BadRequest("Profile|GetMine|Invalid model");
            }

            model.Query = model.Query?.ToLower();
            var old = -60; //minutes - eventually move this to a config
            var dtCompare = DateTime.UtcNow.AddMinutes(old);

            //self-cleanup...always filter out "old" messages - many times the user does not dismiss old import messages
            //help them with this. do this in separate threads to avoid collision of connection
            var oldRows = _dal.Where(s => s.Created <= dtCompare, base.DalUserToken, null, null, false, true).Data;
            if (oldRows.Any())
            {
                var dalCleanup = _svcImport.GetImportLogDalIsolated();
                dalCleanup.DeleteManyAsync(oldRows.Select(s => s.ID.Value).ToList(), base.DalUserToken);
            }

            //now get the remaining messages
            var result = _dal.Where(s =>
                            //string query section
                            (string.IsNullOrEmpty(model.Query) || string.Join(",", s.Files.Select(f => f.FileName)).ToLower().Contains(model.Query))
                            //always filter out "old" messages - many times the user does not dismiss old messages
                            //help them with this.
                            && s.Created > dtCompare
                            , base.DalUserToken, null, null, false, false);

            return Ok(result);
        }

        /// <summary>
        /// Delete an existing import log. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Delete")]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Delete([FromBody] IdIntModel model)
        {
            //This is a soft delete but also gets rid of the file chunks data
            var result = await _dal.DeleteAsync(model.ID, base.DalUserToken);
            if (result < 0)
            {
                _logger.LogWarning($"ImportLogController|Delete|Could not delete item. Invalid id:{model.ID}.");
                return BadRequest("Could not delete item. Invalid id.");
            }
            _logger.LogInformation($"ImportLogController|Delete|Deleted item. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was deleted." });
        }


        #region Import Chunked File
        //private string _UPLOAD_FOLDER = Path.Combine(AppContext.BaseDirectory, "uploads");
        //private long _chunkSize;

        /// <summary>
        /// Create an import record in the DB and return to caller. This id will serve as the value
        /// to tie everything together. We make this a separate call because there will 
        /// be a series of subsequent processing calls which are split into chunks to allow for multiple
        /// files as well as large files split into individual chunks. 
        /// This returns a "process id" that should be used in the /process calls to tie everything together. 
        /// </summary>
        /// <param name="model">list of files w/ some additional info about the files to be uploaded</param>
        /// <returns></returns>
        [HttpPost("init")]
        [ProducesResponseType(200, Type = typeof(ImportLogModel))]
        public async Task<IActionResult> ImportStart([FromBody] ImportStartModel model)
        {
            if (model == null || model.Items?.Count == 0)
            {
                return BadRequest("Model is null or empty. At least one file is required to start the import process.");
            }

            TryValidateModel(model);
            if (!ModelState.IsValid)
            {
                var errors = ExtractModelStateErrors();
                _logger.LogCritical($"ProfileController|ImportStart|User Id:{LocalUser.ID}, Errors: {errors}");
                return BadRequest("The import could not start. Please correct the following: " + errors.ToString());
            }

            //make a call to DB to insert a record and return its id
            //the rest of the fields are set in the dal
            var itemAdd = new ImportLogModel()
            {
                Messages = new List<ImportLogMessageModel>() {
                    new ImportLogMessageModel() {
                        Message = $"Starting file upload(s)..."
                    }, 

                },
                Files = model.Items
            };

            //determine if user should get an extra warning due to large file sizes...
            long totalSize = model.Items.Select(x => x.TotalChunks).Sum(y => y) / (1024 * 1024);
            if (totalSize > 10)
            {
                itemAdd.Messages.Add(new ImportLogMessageModel(){   
                    Message = $"The size of the file(s) being imported is approximately ${totalSize}. This import will several minutes to complete."
                }
                );
            }

            var importId = await _dal.AddAsync(itemAdd, base.DalUserToken);

            //now get latest record and return so that the subsequent calls can pass in the correct associations
            var result = await _dal.GetByIdAsync(importId.Value, base.DalUserToken);

            return Ok(result);
        }

        [HttpPost("uploadfiles")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> ImportUploadFiles([FromBody] ImportFileChunkProcessModel model)
        {
            if (model == null)
            {
                return BadRequest("Model is null or empty. Unable to process this file chunk.");
            }

            //get the import action record and then get the import file id associated with this chunk.
            var importItem = await _dal.GetByIdAsync(model.ImportActionId, base.DalUserToken);

            try
            {
                //convert string representation of bytes into byte[], if it fails, then return
                /*
                byte[] contents;
                try
                {
                    contents = (new System.Text.ASCIIEncoding()).GetBytes(model.Contents);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical($"ProfileController|ImportUploadFiles|User Id:{LocalUser.ID}, FileName: {model.FileName}, Chunk: {model.ChunkOrder}. Could not convert content data into byte[].");
                    throw new ImportException($"Could not upload part of {model.FileName}.", ex);
                }
                */

                TryValidateModel(model);
                if (!ModelState.IsValid)
                {
                    var errors = ExtractModelStateErrors();
                    _logger.LogCritical($"ProfileController|ImportUploadFiles|User Id:{LocalUser.ID}, Errors: {errors}");
                    throw new ImportException($"The import file chunk is invalid. " + errors.ToString());
                }

                //then get the import file id associated with this chunk.
                var file = importItem.Files?.Find(x => x.ID.Equals(model.ImportFileId));
                if (file == null)
                {
                    _logger.LogCritical($"Import file record not found. Import Action Id: {model.ImportActionId}, Import File Id: {model.ImportFileId}");
                    throw new ImportException($"Import failed. Cannot find associated file {model.FileName}.");
                }

                try
                {
                    //import this chunk and save to db
                    file.Chunks.Add(new ImportFileChunkModel()
                    {
                        ChunkOrder = model.ChunkOrder,
                        Contents = model.Contents, //contents,
                        ImportFileId = model.ImportFileId
                    });
                    //add import log message so we can update front end on progress or failures
                    var msgPart = file.TotalChunks < 2 ? "" : $" (part { model.ChunkOrder} of { file.TotalChunks})";
                    _logger.LogInformation($"Uploading {file.FileName} file contents{msgPart}...");  
                    //don't put part X of Y in user facing message b/c it will not go in order necessarily and can be confusing.
                    importItem.Messages.Add(new ImportLogMessageModel() { Message = $"Uploading {file.FileName} file contents..." });
                    importItem.Status = TaskStatusEnum.InProgress;
                    await _dal.UpdateAsync(importItem, base.DalUserToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, $"Import failure. Import Action Id: {model.ImportActionId}, Import File Id: {model.ImportFileId}. {ex.Message}");
                    throw new ImportException($"Import failed on part {model.ChunkOrder} of {model.FileName}...{ex.Message}", ex);
                }

                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = true,
                        Message = $"Imported chunk {model.ChunkOrder} for file {model.FileName}",
                        Data = model.ChunkOrder
                    }
                );
            }
            //funnel all exceptions to here so we can log import message, stop the import and let user know why
            catch (ImportException ex)
            {
                //add message to import log, then return message to user
                importItem.Messages.Add(new ImportLogMessageModel() { Message = ex.Message });
                importItem.Status = TaskStatusEnum.Failed;
                await _dal.UpdateAsync(importItem, base.DalUserToken);

                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = false,
                        Message = ex.Message,
                        Data = null
                    }
                );
            }
        }

        /// <summary>
        /// Once all individual and chunked files are imported, then we should call this to initiate
        /// the actual import processing which converts the files into profiles in profile designer.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("processfiles")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> ImportProcessFiles([FromBody] IdIntModel model)
        {
            return await ImportProcessFilesInternal(model, false, false);
        }

        /// <summary>
        /// Once all individual and chunked files are imported, then we should call this to initiate
        /// the actual import processing which converts the files into profiles in profile designer.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("admin/processfiles")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        [Authorize(Roles = "cesmii.profiledesigner.admin")]
        public async Task<IActionResult> ImportProcessFilesAdmin([FromBody] IdIntModel model)
        {
            return await ImportProcessFilesInternal(model, true, false);
        }

        /// <summary>
        /// Once all individual and chunked files are imported, then we should call this to initiate
        /// the actual import processing which converts the files into profiles in profile designer.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("admin/processfiles/upgrade")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        [Authorize(Roles = "cesmii.profiledesigner.admin")]
        public async Task<IActionResult> ImportProcessFilesUpgrade([FromBody] IdIntModel model)
        {
            return await ImportProcessFilesInternal(model, true, true);
        }

        /// <summary>
        /// Once all individual and chunked files are imported, then we should call this to initiate
        /// the actual import processing which converts the files into profiles in profile designer.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("processfiles")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        //public async Task<IActionResult> ImportProcessFiles([FromBody] IdIntModel model)
        private async Task<IActionResult> ImportProcessFilesInternal(IdIntModel model, bool allowMultiVersion, bool upgradePreviousVersions)
        {
            //get the import action record and then get the import file id associated with this chunk.
            var importItem = await _dal.GetByIdAsync(model.ID, base.DalUserToken);

            try
            { 
                TryValidateModel(model);
                if (!ModelState.IsValid)
                {
                    var errors = ExtractModelStateErrors();
                    _logger.LogCritical($"ProfileController|ImportProcessFiles|User Id:{LocalUser.ID}, Errors: {errors}");
                    throw new ImportException("The import complete model is invalid. Please correct the following: " + errors.ToString());
                }

                if (importItem == null)
                {
                    _logger.LogCritical($"Import item not found. Import Action Id: {model.ID}.");
                    throw new ImportException($"Import failed. Cannot find associated import record.");
                }

                var fileNames = importItem.Files == null ? "[empty]" : string.Join(", ", importItem.Files.Select(x => x.FileName).ToArray());

                //loop over and reassemble chunked files.
                //create an importopcmodel for each file.
                var importItems = new List<ImportOPCModel>();
                foreach (var file in importItem.Files)
                {
                    if (file.Chunks == null || file.Chunks.Count == 0)
                    {
                        _logger.LogCritical($"ProfileController|ImportProcessFiles|Failed. Invalid file chunks for id: {file.ID}, {file.FileName}");
                        throw new ImportException($"The import has failed.An error occurred completing the upload process for {file.FileName}. ");
                    }

                    //prepare merged byte array for this file
                    var merged = MergeChunks(file);

                    ///validate / compare file to expected size, number of chunks
                    if (!ValidateChunkedFile(file, merged.Length, out string msgValidation))
                    {
                        throw new ImportException(msgValidation);
                    }

                    //assemble the file and create model used during import
                    //var contentString = merged == null ? "" : System.Text.Encoding.UTF8.GetString(merged, 0, merged.Length);
                    importItems.Add(new ImportOPCModel() { FileName = file.FileName, Data = merged });
                }

                //update import messages, clean out import chunk files to keep db size manageable. 
                importItem.Messages.Add(new ImportLogMessageModel() { Message = $"Raw files uploaded. Starting processing of {fileNames}..." });
                importItem.Status = TaskStatusEnum.InProgress;
                await _dal.UpdateAsync(importItem, base.DalUserToken);

                //call the existing import code. 
                //pass in the author id as current user
                //kick off background process, logid is returned immediately so front end can track progress...
                var userInfo = new ImportUserModel() { User = LocalUser, UserToken = base.DalUserToken };
                await _svcImport.ImportOpcUaNodeSet(importItem.ID.Value, importItems, userInfo, allowMultiVersion: allowMultiVersion, upgradePreviousVersions: upgradePreviousVersions);

                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = true,
                        Message = "Import is processing...",
                        Data = model.ID
                    }
                );

            }
            //funnel all exceptions to here so we can log import message, stop the import and let user know why
            catch (ImportException ex)
            {
                //add message to import log, then return message to user
                importItem.Messages.Add(new ImportLogMessageModel() { Message = ex.Message });
                importItem.Status = TaskStatusEnum.Failed;
                await _dal.UpdateAsync(importItem, base.DalUserToken);

                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = false,
                        Message = ex.Message,
                        Data = null
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, ex.Message);

                var fileNames = string.Join(", ", importItem.Files.Select(x => x.FileName).ToArray());
                var msg = $"Import failed for file(s) {fileNames}...{ex.Message}";

                //add message to import log, then return message to user
                importItem.Messages.Add(new ImportLogMessageModel() { Message = msg });
                importItem.Status = TaskStatusEnum.Failed;
                await _dal.UpdateAsync(importItem, base.DalUserToken);

                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = false,
                        Message = msg,
                        Data = null
                    }
                );
            }

        }

        private bool ValidateChunkedFile(ImportFileModel file, int totalBytesLength, out string msg)
        {
            msg = "";
            //validate chunk
            if (file.TotalChunks != file.Chunks.Count)
            {
                msg = $"Number of chunked files does not match for {file.FileName}. Expected: {file.TotalChunks}, actual: {file.Chunks.Count}";
                _logger.LogCritical($"ProfileController|ValidateChunkedFile|User Id:{LocalUser.ID}, {msg}");
                return false;
            }

            //validate chunk
            //var totalBytesLength = file.Chunks.Sum(x => x.Data.Length);
            if (file.TotalBytes != totalBytesLength)
            {
                msg = $"Merged file size does not match original file size for {file.FileName}. Expected: {file.TotalBytes}, actual: {totalBytesLength}";
                _logger.LogCritical($"ProfileController|ValidateChunkedFile|User Id:{LocalUser.ID}, {msg}");
                return false;
            }
            return true;
        }

        private string MergeChunks(ImportFileModel file)
        {
            return string.Join("", 
                file.Chunks
                .OrderBy(x => x.ChunkOrder)
                //.ToList()
                .Select(x => x.Contents));
        }
        
        /*
        private byte[] MergeChunks(ImportFileModel file)
        {
            //if one chunk, return it as is
            if (file.Chunks.Count == 1)
            {
                return file.Chunks[0].Contents;
            }

            //multiple files - merge into single byte array in proper order.
            file.Chunks = file.Chunks.OrderBy(x => x.ChunkOrder).ToList();
            byte[] result = null;
            foreach (var chunk in file.Chunks)
            {
                result = result == null ? chunk.Contents : result.Concat(chunk.Contents).ToArray();
            }
            return result;
        }
        */

        /*
        private static void MergeChunks(string chunk1, string chunk2)
        {
            FileStream fs1 = null;
            FileStream fs2 = null;
            try
            {
                fs1 = System.IO.File.Open(chunk1, FileMode.Append);
                fs2 = System.IO.File.Open(chunk2, FileMode.Open);
                byte[] fs2Content = new byte[fs2.Length];
                fs2.Read(fs2Content, 0, (int)fs2.Length);
                fs1.Write(fs2Content, 0, (int)fs2.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " : " + ex.StackTrace);
            }
            finally
            {
                if (fs1 != null) fs1.Close();
                if (fs2 != null) fs2.Close();
                System.IO.File.Delete(chunk2);
            }
        }
        */
        #endregion
    }

    [Serializable]
    public class ImportException : Exception
    {
        public ImportException()
        {
        }

        public ImportException(string? message) : base(message)
        {
        }

        public ImportException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ImportException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

}
