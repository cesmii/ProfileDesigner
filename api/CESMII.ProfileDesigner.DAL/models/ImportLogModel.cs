namespace CESMII.ProfileDesigner.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using CESMII.ProfileDesigner.Common.Enums;

    public class ImportLogModel : AbstractModel
    {
        /// <summary>
        /// string array of file names being imported
        /// </summary>
        public string[] FileList { get; set; }

        //public LookupItemModel Status { get; set; }
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

        public int OwnerId { get; set; }

        //public UserSimpleModel Owner { get; set; }

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

        //public ProfileModel Profile { get; set; }

        public int ProfileId { get; set; }

        public DateTime Created { get; set; }
    }

}