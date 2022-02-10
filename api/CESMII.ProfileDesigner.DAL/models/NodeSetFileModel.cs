using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CESMII.ProfileDesigner.DAL.Models
{
    public class NodeSetFileModel : AbstractModel
    {
        public string FileName { get; set; }

        public string Version { get; set; }

        public DateTime PublicationDate { get; set; }

        public string FileCache { get; set; }

        public int? AuthorId { get; set; }
    }
}
