namespace CESMII.ProfileDesigner.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using CESMII.ProfileDesigner.Common.Enums;

    public class ImportLogModel : AbstractModel
    {
        /// <summary>
        /// convenience getter of list of files names for this import
        /// </summary>
        public string FileList { 
            get 
            {
                if (this.Files == null) return "";
                return string.Join(",", this.Files.Select(f => f.FileName));
            } 
        }

        public TaskStatusEnum Status { get; set; }

        public string StatusName {
            get {
                return this.Status.ToString();
            }
        }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public DateTime? Completed { get; set; }

        public List<ImportLogMessageModel> Messages { get; set; }

        public List<ImportProfileWarningModel> ProfileWarnings { get; set; }

        /// <summary>
        /// List of file names to be imported as one atomic action
        /// </summary>
        public List<ImportFileModel> Files { get; set; }

        public int OwnerId { get; set; }

        public bool IsActive { get; set; }
    }

    public class ImportLogMessageModel : AbstractModel
    {
        public string Message { get; set; }

        public ImportLogModel ImportLog { get; set; }

        public DateTime Created { get; set; }
    }

    public class ImportProfileWarningModel : AbstractModel
    {
        public string Message { get; set; }

        public int ProfileId { get; set; }

        public DateTime Created { get; set; }
    }

    /// <summary>
    /// This will be the parent file record which has one or many file chunks.
    /// Some summary data will be stored here to allow for post upload validation
    /// </summary>
    public class ImportFileModel : AbstractModel
    {
        /// <summary>
        /// Parent File - fk to parent record which ties together an individual file 
        /// This should be some unique id that ties together the processing of a particular chunked file
        /// This will ensure that we don't pick up files with same name in the event someone 
        /// kicks off 
        /// </summary>
        [Required]
        public int ImportActionId { get; set; }

        /// <summary>
        /// TODO: This will move into DAL
        /// Original file name
        /// This record will have a collection of child records which hold the chunked content
        /// </summary>
        [Required]
        public string FileName { get; set; }

        public List<ImportFileChunkModel> Chunks { get; set; }

        /// <summary>
        /// Use this value when doing post upload validation
        /// </summary>
        [Range(1, Int32.MaxValue, ErrorMessage = "Invalid Value")]
        public int TotalChunks { get; set; }

        /// <summary>
        /// Use this value when doing post upload validation
        /// </summary>
        [Range(1, long.MaxValue, ErrorMessage = "Invalid Value")]
        public long TotalBytes { get; set; }

    }

    public class ImportFileChunkModel : AbstractModel
    {
        /// <summary>
        /// Parent File - fk to parent record which ties together an individual file 
        /// This should be some unique id that ties together the processing of a particular chunked file
        /// This will ensure that we don't pick up files with same name in the event someone 
        /// kicks off 
        /// </summary>
        [Required]
        public int ImportFileId { get; set; }

        /// <summary>
        /// Order of this file chunk relative to the parent file.
        /// Small files will only have one chunk
        /// </summary>
        [Range(1, Int32.MaxValue, ErrorMessage = "Invalid Value")]
        public int ChunkOrder { get; set; }

        /// <summary>
        /// This represents a data chunk (up to 20mb). For small file imports, this may be the entire file.
        /// For large files, this will be a portion of the file. We will pass back an id to the caller so subsequent 
        /// file chunks can be associated to the same import file operation
        /// </summary>
        public byte[] Contents { get; set; }
    }

}