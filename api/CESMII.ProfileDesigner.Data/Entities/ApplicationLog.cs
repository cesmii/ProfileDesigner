namespace CESMII.ProfileDesigner.Data.Entities
{
    using System.ComponentModel.DataAnnotations.Schema;

    public class ApplicationLog : AbstractEntity
    {
        [Column(TypeName = "VARCHAR(20)")]
        public string Level { get; set; }

        public string CallSite { get; set; }

        public string Type { get; set; }

        public string Message { get; set; }

        public string StackTrace { get; set; }

        public string InnerException { get; set; }

        public string AdditionalInfo { get; set; }

        public string Time { get; set; }
    }
}