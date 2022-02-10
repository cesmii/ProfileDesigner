namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    public class ImportLog : AbstractEntityWithTenant
    {
        /// <summary>
        /// Commas separated list of files being imported
        /// </summary>
        [Column(name: "file_list")]
        public string FileList { get; set; }

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
}