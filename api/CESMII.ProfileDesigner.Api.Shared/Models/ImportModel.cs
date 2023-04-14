using System;
using System.ComponentModel.DataAnnotations;

namespace CESMII.ProfileDesigner.Api.Shared.Models
{
    public class ImportOPCModel
    {
        public string FileName { get; set; }
        public string Data { get; set; }
        public string CloudLibraryId { get; set; }
    }

    public class ImportFileChunk
    {
        /// <summary>
        /// This should be some unique id that ties together the processing of a particular chunked file
        /// This will ensure that we don't pick up files with same name in the event someone 
        /// kicks off 
        /// </summary>
        [Required]
        public string ProcessId { get; set; }
        /// <summary>
        /// This is an id which ties together a file containing multiple chunks. 
        /// </summary>
        [Required]
        public string FileName { get; set; }

        [Range(1, Int32.MaxValue, ErrorMessage = "Invalid Value")]
        public int ChunkId { get; set; }

        [Range(1, Int32.MaxValue, ErrorMessage = "Invalid Value")]
        public int TotalChunks { get; set; }

        /// <summary>
        /// This represents a data chunk (up to 20mb). For small file imports, this may be the entire file.
        /// For large files, this will be a portion of the file. We will pass back an id to the caller so subsequent 
        /// file chunks can be associated to the same import file operation
        /// </summary>
        public byte[] Data { get; set; }
    }

    /// <summary>
    /// Use this model once all parts of chunked file are uploaded. This info
    /// will be used to compare against totals of chunked files to ensure nothing
    /// is lost. 
    /// </summary>
    public class ImportFileChunkComplete
    {
        /// <summary>
        /// This should be some unique id that ties together the processing of a particular chunked file
        /// This will ensure that we don't pick up files with same name in the event someone 
        /// kicks off 
        /// </summary>
        [Required]
        public string ProcessId { get; set; }
        /// <summary>
        /// This is an id which ties together a file containing multiple chunks. 
        /// </summary>
        [Required]
        public string FileName { get; set; }

        [Range(1, Int32.MaxValue, ErrorMessage = "Invalid Value")]
        public int TotalChunks { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Invalid Value")]
        public long TotalBytes { get; set; }
    }
}
