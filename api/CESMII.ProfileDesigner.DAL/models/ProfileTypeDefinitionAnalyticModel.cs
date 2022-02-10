namespace CESMII.ProfileDesigner.DAL.Models
{
    public class ProfileTypeDefinitionAnalyticModel : AbstractProfileDesignerModel
    {
        public int ProfileTypeDefinitionId { get; set; }

        public int PageVisitCount { get; set; }

        public int ExtendCount { get; set; }

        public int ManualRank { get; set; }
    }
}