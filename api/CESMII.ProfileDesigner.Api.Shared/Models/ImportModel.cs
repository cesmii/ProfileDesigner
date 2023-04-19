using CESMII.ProfileDesigner.DAL.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CESMII.ProfileDesigner.Api.Shared.Models
{
    public class ImportOPCModel
    {
        public string FileName { get; set; }
        public string Data { get; set; }
        public string CloudLibraryId { get; set; }
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

}
