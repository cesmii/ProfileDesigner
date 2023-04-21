namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    public class ImportLog : AbstractEntityWithTenant
    {
        [Column(name: "status_id")]
        public int StatusId { get; set; }

        public virtual LookupItem Status { get; set; }

        [Column(name: "created")]
        public DateTime Created { get; set; }

        [Column(name: "updated")]
        public DateTime Updated { get; set; }

        [Column(name: "completed")]
        public DateTime? Completed { get; set; }

        public virtual List<ImportLogMessage> Messages { get; set; }

        public virtual List<ImportProfileWarning> ProfileWarnings { get; set; }
        //[Column(name: "owner_id")]
        //public int OwnerId { get; set; }

        public virtual List<ImportFile> Files { get; set; }

        public virtual User Owner { get; set; }

        [Column(name: "is_active")]
        public bool IsActive { get; set; }
    }

    public class ImportLogMessage : AbstractEntity
    {
        [Column(name: "message")]
        public string Message { get; set; }

        [Column(name: "import_log_id")]
        public int ImportLogId { get; set; }

        public virtual ImportLog ImportLog { get; set; }

        [Column(name: "created")]
        public DateTime Created { get; set; }
    }

    public class ImportProfileWarning : AbstractEntity
    {
        [Column(name: "message")]
        public string Message { get; set; }

        [Column(name: "import_log_id")]
        public int ImportLogId { get; set; }

        public virtual ImportLog ImportLog { get; set; }

        [Column(name: "profile_id")]
        public int ProfileId { get; set; }

        public virtual Profile Profile { get; set; }
        
        [Column(name: "created")]
        public DateTime Created { get; set; }
    }

    public class ImportFile : AbstractEntity
    {
        [Column(name: "file_name")]
        public string FileName { get; set; }

        [Column(name: "import_id")]
        public int ImportActionId { get; set; }

        [Column(name: "total_chunks")]
        public int TotalChunks { get; set; }

        [Column(name: "total_bytes")]
        public long TotalBytes { get; set; }

        public virtual ImportLog ImportAction { get; set; }

        public virtual List<ImportFileChunk> Chunks { get; set; }
    }

    public class ImportFileChunk : AbstractEntity
    {
        [Column(name: "import_file_id")]
        public int ImportFileId { get; set; }

        [Column(name: "chunk_order")]
        public int ChunkOrder { get; set; }

        [Column(name: "contents")]
        public string Contents { get; set; }

        public virtual ImportFile ImportFile { get; set; }
    }

}