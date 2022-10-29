namespace CESMII.ProfileDesigner.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
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

    /// <summary>
    /// Extend ProfileModel for the export scenario.
    /// </summary>
    public class CloudLibProfileModel : ProfileModel
    {
        public CloudLibProfileModel()
        {
        }
        public static CloudLibProfileModel MapFromProfile(ProfileModel profile)
        {
            return new CloudLibProfileModel
            {
                ID = profile.ID,
                Namespace = profile.Namespace,
                PublishDate = profile.PublishDate,
                Version = profile.Version,
                AuthorId = profile.AuthorId,
                StandardProfileID = profile.StandardProfileID,
                StandardProfile = profile.StandardProfile,
                NodeSetFiles = profile.NodeSetFiles,
                HasLocalProfile = true,
                CloudLibraryId = profile.StandardProfile?.CloudLibraryId,
            };
        }

        // TODO Additional CloudLib properties like License, Organisation etc.
        public bool HasLocalProfile { get; set; }
        public string CloudLibraryId { get; set; }
        public string NodesetXml { get; set; }
        public string Description { get; set; }
        public string Contributor { get; set; }
        public string ExternalAuthor { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public List<string> MetaTags { get; set; }
        public DateTime? Updated { get; set; }
    }

}