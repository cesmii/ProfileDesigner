namespace CESMII.ProfileDesigner.DAL.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CESMII.ProfileDesigner.Common.Enums;

    public class ProfileModel : AbstractModel
    {
        public string Namespace { get; set; }

        public string Version { get; set; }

        public DateTime? PublishDate { get; set; }

        /// <summary>
        /// The URI to use for XML elements for this nodeset
        /// </summary>
        public string XmlSchemaUri{ get; set; }

        public string CloudLibraryId { get; set; }
        public bool? CloudLibPendingApproval { get; set; }
        public string CloudLibApprovalStatus { get; set; } = "UNKNOWN";
        public string CloudLibApprovalDescription { get; set; }

        public List<NodeSetFileModel> NodeSetFiles { get; set; }

        public virtual List<ImportProfileWarningModel> ImportWarnings { get; set; }

        public int? AuthorId { get; set; }

        public UserSimpleModel Author { get; set; }

        #region Cloud Library meta data
        /// <summary>Gets or sets the title.</summary>
        /// <value>The title.</value>
        public string Title { get; set; }

        /// <summary>Gets or sets the license.</summary>
        /// <value>The license.</value>
        public string License { get; set; }

        /// <summary>Gets or sets the license URL.</summary>
        /// <value>The license URL.</value>
        public string LicenseUrl { get; set; }

        /// <summary>Gets or sets the copyright text.</summary>
        /// <value>The copyright text.</value>
        public string CopyrightText { get; set; }

        /// <summary>Gets or sets the contributor.</summary>
        /// <value>The contributor name.</value>
        public string ContributorName { get; set; }

        /// <summary>Gets or sets the description.</summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>Gets or sets the category.</summary>
        /// <value>The category name.</value>
        public string CategoryName { get; set; }

        /// <summary>
        /// Link to additional documentation, specifications, GitHub, etc.
        /// For example, If the address space is based on a standard or official UA Information Model, this links to the standard or the OPC specification URL.
        /// </summary>
        public string DocumentationUrl { get; set; }

        /// <summary>Gets or sets the icon URL.</summary>
        /// <value>The icon URL.</value>
        public string IconUrl { get; set; }

        /// <summary>Gets or sets the key words.</summary>
        /// <value>The key words.</value>
        public List<string> Keywords { get; set; }

        /// <summary>Gets or sets the purchasing information URL.</summary>
        /// <value>The purchasing information URL.</value>
        public string PurchasingInformationUrl { get; set; }

        /// <summary>Gets or sets the release notes URL.</summary>
        /// <value>The release notes URL.</value>
        public string ReleaseNotesUrl { get; set; }

        /// <summary>Gets or sets the release notes URL.</summary>
        /// <value>The release notes URL.</value>
        public string TestSpecificationUrl { get; set; }

        /// <summary>
        /// Supported ISO language codes
        /// </summary>
        public List<string> SupportedLocales { get; set; }

        /// <summary>Gets or sets the additional properties.</summary>
        /// <value>The additional properties.</value>
        public List<AdditionalProperty> AdditionalProperties { get; set; }
        #endregion // Cloud Library meta data
        public bool IsReadOnly
        {
            get
            {
                return !this.AuthorId.HasValue || !string.IsNullOrEmpty(this.CloudLibraryId)/* this.StandardProfileID.HasValue*/;
            }
        }

        public override string ToString()
        {
            var valPublishDate = PublishDate.HasValue ? $"({PublishDate.Value.ToString("yyyy-MM-dd")})" : "";
            return $"{Namespace} {Version} {valPublishDate}";
        }

        /// <summary>
        /// Calculate and return the profile state. This state will drive how the UI displays this profile.

        /// </summary>
        public ProfileStateEnum ProfileState
        {
            get
            {
                if (!string.IsNullOrEmpty(this.CloudLibraryId) && 
                        this.CloudLibApprovalStatus?.ToUpper() == "PENDING") return ProfileStateEnum.CloudLibPending;
                else if (!string.IsNullOrEmpty(this.CloudLibraryId) &&
                        this.CloudLibApprovalStatus?.ToUpper() == "REJECTED") return ProfileStateEnum.CloudLibRejected;
                else if (!string.IsNullOrEmpty(this.CloudLibraryId) &&
                        this.CloudLibApprovalStatus?.ToUpper() == "APPROVED") return ProfileStateEnum.CloudLibApproved;
                else if (!string.IsNullOrEmpty(this.CloudLibraryId) &&
                        this.CloudLibApprovalStatus?.ToUpper() == "CANCELED") return ProfileStateEnum.CloudLibCancelled;
                else if (!string.IsNullOrEmpty(this.CloudLibraryId)) return ProfileStateEnum.CloudLibPublished;
                else if (!this.AuthorId.HasValue && !string.IsNullOrEmpty(this.CloudLibraryId)) return ProfileStateEnum.Core;
                // Note author id will only be set to the the user making the request or null. 
                // Other areas of the code will check to make sure that the requesting user only gets their stuff or core stuff
                else if (this.AuthorId.HasValue && string.IsNullOrEmpty(this.CloudLibraryId)) return ProfileStateEnum.Local; 
                else return ProfileStateEnum.Unknown; 
            }
        }


    }

    public class AdditionalProperty
    {
        public string Name { get; set; }
        public string Value { get; set; }
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
                XmlSchemaUri = profile.XmlSchemaUri,
                AuthorId = profile.AuthorId,
                NodeSetFiles = profile.NodeSetFiles,
                HasLocalProfile = true,
                CloudLibraryId = profile.CloudLibraryId,
                Title = profile.Title,
                Description = profile.Description,
                License = profile.License,
                LicenseUrl = profile.LicenseUrl,
                Keywords = profile.Keywords,
                AdditionalProperties = profile.AdditionalProperties.Select(p => new AdditionalProperty {  Name = p.Name, Value =p.Value}).ToList(),
                CategoryName = profile.CategoryName,
                ContributorName = profile.ContributorName,
                CopyrightText = profile.CopyrightText,
                Author = profile.Author,
                DocumentationUrl = profile.DocumentationUrl,
                IconUrl = profile.IconUrl,
                ImportWarnings = profile.ImportWarnings,
                PurchasingInformationUrl = profile.PurchasingInformationUrl,
                ReleaseNotesUrl = profile.ReleaseNotesUrl,
                SupportedLocales = profile.SupportedLocales,
                TestSpecificationUrl = profile.TestSpecificationUrl,
                NodesetXml = null,
            };
        }

        public bool HasLocalProfile { get; set; }
        public string NodesetXml { get; set; }
    }

}