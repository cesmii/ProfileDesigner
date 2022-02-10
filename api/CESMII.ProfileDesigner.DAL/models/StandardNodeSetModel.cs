namespace CESMII.ProfileDesigner.DAL.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using CESMII.ProfileDesigner.Common.Enums;

    public class StandardNodeSetModel : AbstractModel
    {
        public string Namespace { get; set; }

        public string Version { get; set; }

        public string Filename { get; set; }

        public DateTime? PublishDate { get; set; }
    }

}