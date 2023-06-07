using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.DAL.Models;

namespace CESMII.ProfileDesigner.Api.Shared.Models
{
    public class ImportOPCModel
    {
        public string FileName { get; set; }
        public string Data { get; set; }
        public string CloudLibraryId { get; set; }
    }


    public class ImportStartModel
    {
        public bool NotifyOnComplete { get; set; }
        public List<ImportFileModel> Items { get; set; }
    }


    /// <summary>
    /// This will be used in the profile controller for processing file chunks
    /// </summary>
    public class ImportFileChunkProcessModel : ImportFileChunkModel
    {
        [Required]
        public int ImportActionId { get; set; }

        /// <summary>
        /// Purely informational to help display relevant message should something go wrong.
        /// </summary>
        [Required]
        public string FileName { get; set; }
    }

    /// <summary>
    /// This will be used to pass a couple of pieces of related user info needed downstream in the import
    /// </summary>
    public class ImportUserModel
    {
        public UserModel User { get; set; }

        public UserToken UserToken { get; set; }
    }

    /// <summary>
    /// This will be used in the notification email to inform user of completion
    /// </summary>
    public class ImportCompleteNotifyModel 
    {
        public ImportLogModel ImportItem { get; set; }

        /// <summary>
        /// This is the user who performed the upload.
        /// </summary>
        public UserModel Author { get; set; }

        /// <summary>
        /// only populate this if import fails. We will populate this extra info in a separate email
        /// to admin / support user
        /// </summary>
        public UserModel AdminUserInfo { get; set; } = null;

        /// <summary>
        /// used in email template to determine proper link
        /// </summary>
        public string BaseUrl { get; set; }
    }


}
