using CESMII.ProfileDesigner.DAL.Models;
using System;

namespace CESMII.ProfileDesigner.Api.Shared.Models
{
    public class SubmittedProfileModel : AbstractModel
    {
        public string strReason { get; set; }
        public UserSimpleModel Author { get; set; }
        public string Description { get; set; }
        public string License { get; set; }
        public string Namespace { get; set; }
        public DateTime? PublishDate { get; set; }
        public string Title { get; set; }
        public string Version { get; set; }
        public string Status { get; set; }


        public SubmittedProfileModel(ProfileModel profile, ApprovalModel appmodel, string strStatus)
        {
            strReason = appmodel.ApprovalDescription;
            Author = profile.Author;
            Description = profile.Description;
            License = profile.License;
            Namespace = profile.Namespace;
            PublishDate = profile.PublishDate;
            Title = profile.Title;
            Version = profile.Version;
            Status = strStatus;
        }
    }
}
