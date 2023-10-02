using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Api.Shared.Models;

namespace CESMII.ProfileDesigner.Importer.Utils
{
    public static class ImportUtils
    {
        public static List<ImportOPCModel> MergeChunkedFiles(ImportLogModel item, ILogger logger)
        {
            //loop over and reassemble chunked files.
            //create an importopcmodel for each file.
            var result = new List<ImportOPCModel>();
            foreach (var file in item.Files)
            {
                if (file.Chunks == null || file.Chunks.Count == 0)
                {
                    logger.LogCritical($"ImportUtils|MergeChunkedFiles|Failed. Invalid file chunks for id: {file.ID}, {file.FileName}");
                    throw new ImportException($"The import has failed.An error occurred completing the upload process for {file.FileName}. ");
                }

                //prepare merged byte array for this file
                var merged = MergeChunks(file);

                ///validate / compare file to expected size, number of chunks
                if (!ValidateChunkedFile(file, merged.Length, logger, out string msgValidation))
                {
                    throw new ImportException(msgValidation);
                }

                //assemble the file and create model used during import
                //var contentString = merged == null ? "" : System.Text.Encoding.UTF8.GetString(merged, 0, merged.Length);
                result.Add(new ImportOPCModel() { FileName = file.FileName, Data = merged });
            }
            return result;
        }

        private static string MergeChunks(ImportFileModel file)
        {
            return string.Join("",
                file.Chunks
                .OrderBy(x => x.ChunkOrder)
                //.ToList()
                .Select(x => x.Contents));
        }

        private static bool ValidateChunkedFile(ImportFileModel file, int totalBytesLength, ILogger logger, out string msg)
        {
            msg = "";
            //validate chunk
            if (file.TotalChunks != file.Chunks.Count)
            {
                msg = $"Number of chunked files does not match for {file.FileName}. Expected: {file.TotalChunks}, actual: {file.Chunks.Count}";
                logger.LogCritical($"ProfileController|ValidateChunkedFile|File Id / Name:{file.ID}/{file.FileName}, {msg}");
                return false;
            }

            //validate chunk
            //var totalBytesLength = file.Chunks.Sum(x => x.Data.Length);
            if (file.TotalBytes != totalBytesLength)
            {
                msg = $"Merged file size does not match original file size for {file.FileName}. Expected: {file.TotalBytes}, actual: {totalBytesLength}";
                logger.LogCritical($"ProfileController|ValidateChunkedFile|File Id / Name:{file.ID}/{file.FileName}, {msg}");
                return false;
            }
            return true;
        }

        public static async Task HandleImportException(Exception ex, ImportLogModel item, UserToken userToken, IDal<ImportLog,ImportLogModel> dalImportLog)
        {
            //add message to import log, then return message to user
            item.Messages.Add(new ImportLogMessageModel() { Message = ex.Message });
            item.Status = Common.Enums.TaskStatusEnum.Failed;
            await dalImportLog.UpdateAsync(item, userToken);
        }

    }
}
