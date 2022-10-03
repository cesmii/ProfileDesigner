namespace CESMII.ProfileDesigner.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using CESMII.ProfileDesigner.Common.Enums;

    public class ProfileModel : AbstractModel
    {
        public string Namespace { get; set; }

        public string Version { get; set; }

        public DateTime? PublishDate { get; set; }

        public int? StandardProfileID { get; set; }

        public StandardNodeSetModel StandardProfile { get; set; }

        public List<NodeSetFileModel> NodeSetFiles { get; set; }

        public virtual List<ImportProfileWarningModel> ImportWarnings { get; set; }

        public int? AuthorId { get; set; }

        public UserSimpleModel Author { get; set; }

        public bool IsReadOnly
        {
            get
            {
                return !this.AuthorId.HasValue || this.StandardProfileID.HasValue;
            }
        }

        public override string ToString()
        {
            var valPublishDate = PublishDate.HasValue ? $"({PublishDate.Value.ToString("yyyy-MM-dd")})" : ""; 
            return $"{Namespace} {Version} {valPublishDate}";
        }

    }
}