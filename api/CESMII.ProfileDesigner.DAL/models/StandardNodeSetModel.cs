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
        public string CloudLibraryId { get; set; }
    }

    public static class StandardNodeSetExtensions
    {
        public static StandardNodeSetModel ToStandardNodeSet(this CloudLibProfileModel profileToImport)
        {
            return new StandardNodeSetModel 
            { 
                Namespace = profileToImport.Namespace,
                Version = profileToImport.Version,
                PublishDate = profileToImport.PublishDate,
                CloudLibraryId = profileToImport.CloudLibraryId,
                // TODO Filename = ,
            };
        }
    }

}