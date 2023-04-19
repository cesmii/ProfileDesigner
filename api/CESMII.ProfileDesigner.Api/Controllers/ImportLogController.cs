using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                            , base.DalUserToken, null, null, false, true);

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
            //This is a soft delete
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
        public async Task<IActionResult> ImportStart([FromBody] List<ImportFileModel> model)
        {
            if (model == null || model.Count == 0)
            {
                return BadRequest("Model is null or empty. At least one file is required to start the import process");
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
                    }
                },
                Files = model
            };
            var importId = await _dal.AddAsync(itemAdd, base.DalUserToken);

            //now get latest record and return so that the subsequent calls can pass in the correct associations
            var result = await _dal.GetByIdAsync(importId.Value, base.DalUserToken);

            return Ok(result);
        }

        [HttpPost("uploadfiles")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> ImportUploadFiles([FromBody] ImportFileChunkProcessModel model)
        {
            TryValidateModel(model);
            if (!ModelState.IsValid)
            {
                var errors = ExtractModelStateErrors();
                _logger.LogCritical($"ProfileController|ImportUploadFiles|User Id:{LocalUser.ID}, Errors: {errors}");
                return BadRequest("The import file chunk is invalid. Please correct the following: " + errors.ToString());
            }

            //get the import action record and then get the import file id associated with this chunk.
            var importItem = await _dal.GetByIdAsync(model.ImportActionId, base.DalUserToken);
            //then get the import file id associated with this chunk.
            var file = importItem.Files?.Find(x => x.ID.Equals(model.ImportFileId));

            if (file == null)
            {
                _logger.LogCritical($"Import file record not found. Import Action Id: {model.ImportActionId}, Import File Id: {model.ImportFileId}");
                //add import log message so we can update front end on progress or failures
                importItem.Messages.Add(new ImportLogMessageModel() { Message = $"Import failed. Cannot find associated file {model.FileName}." });
                importItem.Status = TaskStatusEnum.Failed;
                await _dal.UpdateAsync(importItem, base.DalUserToken);
                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = false,
                        Message = $"Import failed. Cannot find associated file.",
                        Data = null
                    }
                );
            }

            try
            {
                //import this chunk and save to db
                file.Chunks.Add(new ImportFileChunkModel()
                {
                    ChunkOrder = model.ChunkOrder,
                    Contents = model.Contents,
                    ImportFileId = model.ImportFileId
                });
                //add import log message so we can update front end on progress or failures
                var msgPart = file.TotalChunks < 2 ? "" : $" (part { model.ChunkOrder} of { file.TotalChunks})";
                importItem.Messages.Add(new ImportLogMessageModel() { Message = $"Uploading {file.FileName} file contents{msgPart}..." });
                importItem.Status = TaskStatusEnum.InProgress;
                await _dal.UpdateAsync(importItem, base.DalUserToken);

                /*
                //order of the files matters when we go to reassemble.
                //To ensure ordering isn't 1, 10, 11, 2, 3, we prepend leading 0s to number value
                string chunkId = model.ChunkOrder.ToString();
                while (chunkId.Length < 4)  //this is more than adequate - there won't be any file with 9999 many chunks
                {
                    chunkId = $"0{chunkId}";
                }
                string processingPath = Path.Combine(_UPLOAD_FOLDER, LocalUser.ObjectIdAAD, model.ImportFileId);
                string newFile = Path.Combine(processingPath,$"{chunkId}-{model.FileName}"); // Path.Combine(_tempFolder + "/Temp", (string)(model.FileName + model.ChunkId));
                if (!Directory.Exists(processingPath)) Directory.CreateDirectory(processingPath);
                using (FileStream fs = System.IO.File.Create(newFile))
                {
                    await fs.WriteAsync(model.Contents);
                }
                */
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"Import failure. Import Action Id: {model.ImportActionId}, Import File Id: {model.ImportFileId}. {ex.Message}");
                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = false,
                        Message = $"Import failed on part {model.ChunkOrder} of {model.FileName}...{ex.Message}",
                        Data = null
                    }
                );
            }

            return Ok(
                new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = $"Import chunk {model.ChunkOrder} for file {model.FileName}",
                    Data = model.ChunkOrder
                }
            );
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
        public async Task<IActionResult> ImportProcessFiles([FromBody] IdIntModel model)
        {
            return await ImportProcessFilesInternal(model, false);
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
            return await ImportProcessFilesInternal(model, true);
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
        private async Task<IActionResult> ImportProcessFilesInternal(IdIntModel model, bool allowMultiVersion)
        {
            TryValidateModel(model);
            if (!ModelState.IsValid)
            {
                var errors = ExtractModelStateErrors();
                _logger.LogCritical($"ProfileController|ImportProcessFiles|User Id:{LocalUser.ID}, Errors: {errors}");
                return BadRequest("The import complete model is invalid. Please correct the following: " + errors.ToString());
            }

            //get the import action record and then get the import file id associated with this chunk.
            var importItem = await _dal.GetByIdAsync(model.ID, base.DalUserToken);

            if (importItem == null)
            {
                _logger.LogCritical($"Import item not found. Import Action Id: {model.ID}.");
                //add import log message so we can update front end on progress or failures
                importItem.Messages.Add(new ImportLogMessageModel() { Message = $"Import failed. Cannot find the import item associated with this import." });
                importItem.Status = TaskStatusEnum.Failed;
                await _dal.UpdateAsync(importItem, base.DalUserToken);
                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = false,
                        Message = $"Import failed. Cannot find associated import record.",
                        Data = null
                    }
                );
            }

            var fileNames = importItem.Files == null ? "[empty]" : string.Join(", ", importItem.Files.Select(x => x.FileName).ToArray());

            try
            {
                //loop over and reassemble chunked files.
                //create an importopcmodel for each file.
                var importItems = new List<ImportOPCModel>();
                foreach (var file in importItem.Files)
                {
                    if (file.Chunks == null || file.Chunks.Count == 0)
                    {
                        _logger.LogCritical($"ProfileController|ImportProcessFiles|Failed. Invalid file chunks for id: {file.ID}, {file.FileName}");
                        return Ok(new ResultMessageWithDataModel()
                        {
                            IsSuccess = false,
                            Message = $"The import has failed.An error occurred completing the upload process for {file.FileName}. ",
                            Data = null
                        }
                        );
                    }

                    //prepare merged byte array for this file
                    var merged = MergeChunks(file);

                    ///validate / compare file to expected size, number of chunks
                    if (!ValidateChunkedFile(file, merged.Length, out string msgValidation))
                    {
                        return Ok(new ResultMessageWithDataModel()
                        {
                            IsSuccess = false,
                            Message = msgValidation,
                            Data = null
                        }
                        );
                    }

                    //assemble the file and create model used during import
                    var contentString = merged == null ? "" : System.Text.Encoding.UTF8.GetString(merged, 0, merged.Length);
                    importItems.Add(new ImportOPCModel() { FileName = file.FileName, Data = contentString });
                }

                //update import messages, clean out import chunk files to keep db size manageable. 
                importItem.Messages.Add(new ImportLogMessageModel() { Message = $"Raw files uploaded. Starting processing of {fileNames}..." });
                importItem.Status = TaskStatusEnum.InProgress;
                await _dal.UpdateAsync(importItem, base.DalUserToken);

                //call the existing import code. 
                //pass in the author id as current user
                //kick off background process, logid is returned immediately so front end can track progress...
                await _svcImport.ImportOpcUaNodeSet(importItem.ID.Value, importItems, base.DalUserToken, allowMultiVersion: allowMultiVersion, upgradePreviousVersions: false);

                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = true,
                        Message = "Import is processing...",
                        Data = model.ID
                    }
                );
                /*
                string processingPath = Path.Combine(_UPLOAD_FOLDER, LocalUser.ObjectIdAAD, model.ID);
                if (!Directory.Exists(processingPath)) Directory.CreateDirectory(processingPath);

                //get chunked files from processing folder, the order matters when we reassemble
                string[] filePaths = Directory.GetFiles(processingPath).Where(p => p.Contains(fileName)).OrderBy(p => p).ToArray();

                //if number of files does not match total chunks, return error
                if (filePaths.Length == 0 || filePaths.Length != model.TotalChunks)
                {
                    var msg = $"Number of chunked files does not match for {fileName}. Expected: {model.TotalChunks}, actual: {filePaths.Length}";
                    _logger.LogCritical($"ProfileController|ImportProcessFiles|User Id:{LocalUser.ID}, {msg}");
                    return Ok(new ResultMessageWithDataModel()
                        {
                            IsSuccess = false,
                            Message = msg,
                            Data = null
                        }
                    );
                }

                //merge the file chunks into one final file in parent folder
                string mergeFileName = Path.Combine(processingPath, fileName);
                foreach (string filePath in filePaths)
                {
                    MergeChunks(mergeFileName, filePath);
                }
                //uniquely name this final file to ensure no collisions
                var finalFileName = Path.Combine(_UPLOAD_FOLDER, LocalUser.ObjectIdAAD, $"{model.ProcessId}_{fileName}");
                System.IO.File.Move(mergeFileName, finalFileName);

                //now get the file and compare against original
                var content = System.IO.File.ReadAllBytes(finalFileName);

                if (content.Length == 0 || content.Length != model.TotalBytes)
                {
                    var msg = $"Merged file size does not match original file size for {fileName}. Expected: {model.TotalBytes}, actual: {content.Length}";
                    _logger.LogCritical($"ProfileController|ImportProcessFiles|User Id:{LocalUser.ID}, {msg}");
                    return Ok(
                        new ResultMessageWithDataModel()
                        {
                            IsSuccess = false,
                            Message = msg,
                            Data = null
                        }
                    );
                }

                //convert to string representation for import processing
                var contentString = System.Text.Encoding.UTF8.GetString(content, 0, content.Length); ;

                //remove the processing path
                Directory.Delete(processingPath, true);

                //Now start the import processing
                //TODO: handle multi-file imports w/ chunked file imports
                //_logger.LogInformation($"ProfileController|ImportProcessFiles|Importing {model.Count} nodeset files. User Id:{LocalUser.ID}.");
                _logger.LogInformation($"ProfileController|ImportProcessFiles|Importing {model.FileName} chunked file. User Id:{LocalUser.ID}.");

                //convert file contents to list ImportOpcModel for our import
                var importModel = new List<ImportOPCModel>() { new ImportOPCModel { FileName = model.FileName, Data = contentString } };
                
                //pass in the author id as current user
                //kick off background process, logid is returned immediately so front end can track progress...
                var logId = await _svcImport.ImportOpcUaNodeSet(importModel, base.DalUserToken, allowMultiVersion: false, upgradePreviousVersions: false);

                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = true,
                        Message = "Import is processing...",
                        Data = logId
                    }
                );
                 */
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, ex.Message);
                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = false,
                        Message = $"Import failed for file(s) {fileNames}...{ex.Message}",
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

}
