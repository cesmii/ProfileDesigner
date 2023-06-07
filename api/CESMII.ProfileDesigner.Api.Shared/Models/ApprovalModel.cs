using CESMII.ProfileDesigner.Common.Enums;

namespace CESMII.ProfileDesigner.Api.Shared.Models
{
    public class ApprovalModel
    {
        public string ID { get; set; }
        public ProfileStateEnum ApproveState { get; set; }
        public string ApprovalDescription { get; set; }
    }

}
