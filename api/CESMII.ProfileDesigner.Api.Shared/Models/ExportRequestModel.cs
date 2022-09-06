namespace CESMII.ProfileDesigner.Api.Shared.Models
{
    public class ExportRequestModel
    {
        public int ID { get; set; }
        public string Format { get; set; }
        public bool ForceReexport { get; set; }
    }
}
