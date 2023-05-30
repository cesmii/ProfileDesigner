namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Profile : AbstractEntityWithTenant
    {
        [Column(name: "namespace")]
        public string Namespace { get; set; }

        [Column(name: "version")]
        public string Version { get; set; }

        [Column(name: "publish_date")]
        public DateTime? PublishDate { get; set; }

        [Column(name: "xml_schema_uri")]
        public string XmlSchemaUri { get; set; }

        [Column(name: "cloud_library_id")]
        public string CloudLibraryId { get; set; }
        [Column(name: "cloud_library_pending_approval")]
        public bool? CloudLibPendingApproval { get; set; }

        public virtual List<ImportProfileWarning> ImportWarnings { get; set; }

        [Column(name: "author_id")]
        public int? AuthorId { get; set; }

        /// <summary>
        /// TBD - this may not be a user in the system
        /// </summary>
        public virtual User Author { get; set; }

        #region Cloud Library meta data
        /// <summary>Gets or sets the title.</summary>
        /// <value>The title.</value>
        [Column(name: "title")]
        public string Title { get; set; }

        /// <summary>Gets or sets the license.</summary>
        /// <value>The license.</value>
        [Column(name: "license")]
        public string License { get; set; }

        /// <summary>Gets or sets the license URL.</summary>
        /// <value>The license URL.</value>
        [Column(name: "license_url")]
        public string LicenseUrl { get; set; }

        /// <summary>Gets or sets the copyright text.</summary>
        /// <value>The copyright text.</value>
        [Column(name: "copyright_text")]
        public string CopyrightText { get; set; }

        /// <summary>Gets or sets the contributor.</summary>
        /// <value>The contributor name.</value>
        [Column(name: "contributor_name")]
        public string ContributorName { get; set; }

        /// <summary>Gets or sets the description.</summary>
        /// <value>The description.</value>
        [Column(name: "description")]
        public string Description { get; set; }

        /// <summary>Gets or sets the category.</summary>
        /// <value>The category name.</value>
        [Column(name: "category_name")]
        public string CategoryName { get; set; }

        /// <summary>
        /// Link to additional documentation, specifications, GitHub, etc.
        /// For example, If the address space is based on a standard or official UA Information Model, this links to the standard or the OPC specification URL.
        /// </summary>
        [Column(name: "documentation_url")]
        public string DocumentationUrl { get; set; }

        /// <summary>Gets or sets the icon URL.</summary>
        /// <value>The icon URL.</value>
        [Column(name: "icon_url")]
        public string IconUrl { get; set; }

        /// <summary>Gets or sets the key words.</summary>
        /// <value>The key words.</value>
        [Column(name: "keywords")]
        public string[] Keywords { get; set; }

        /// <summary>Gets or sets the purchasing information URL.</summary>
        /// <value>The purchasing information URL.</value>
        [Column(name: "purchasing_information_url")]
        public string PurchasingInformationUrl { get; set; }

        /// <summary>Gets or sets the release notes URL.</summary>
        /// <value>The release notes URL.</value>
        [Column(name: "release_notes_url")]
        public string ReleaseNotesUrl { get; set; }

        /// <summary>Gets or sets the release notes URL.</summary>
        /// <value>The release notes URL.</value>
        [Column(name: "test_specification_url")]
        public string TestSpecificationUrl { get; set; }

        /// <summary>
        /// Supported ISO language codes
        /// </summary>
        [Column(name: "supported_locales")]
        public string[] SupportedLocales { get; set; }

        /// <summary>Gets or sets the additional properties.</summary>
        /// <value>The additional properties.</value>
        public virtual List<UAProperty> AdditionalProperties { get; set; }
        #endregion // Cloud Library meta data

        // Many-to-many relationship: EF managed
        virtual public List<NodeSetFile> NodeSetFiles { get; set; }

    }

    public class UAProperty : AbstractEntity
    {
        [Column(name: "profile_id")]
        public int? ProfileId { get; set; }
        public virtual Profile Profile { get; set; }
        [Column(name: "name")]
        public string Name { get; set; }
        [Column(name: "value")]
        public string Value { get; set; }
    }
}
