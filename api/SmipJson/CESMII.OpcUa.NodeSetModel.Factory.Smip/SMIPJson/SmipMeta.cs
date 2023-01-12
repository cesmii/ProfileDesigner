using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMIP.JsonIO.Model
{
    public class SmipMeta
    {
        [JsonProperty("file_version")]
        public string FileVersion { get; set; }

        [JsonProperty("database_name")]
        public string DatabaseName { get; set; }

        [JsonProperty("export_timestamp")]
        public DateTime? ExportTimestamp { get; set; }

        [JsonProperty("export_library_fqn")]
        public List<string> ExportLibraryFqn { get; set; }

        [JsonProperty("database_schema_version")]
        public string DatabaseSchemaVersion { get; set; }

        public SmipMeta()
        {
            FileVersion = "";
            DatabaseName = "";
            ExportTimestamp = null;
            ExportLibraryFqn = new List<string>();
            DatabaseSchemaVersion = "";
        }
    }
}
