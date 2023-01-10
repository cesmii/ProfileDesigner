using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMIP.JsonIO.Model
{
    public class SmipLibrary : SmipNode
    {
        [JsonProperty("version")]
        public string Version { get; set; }
        public SmipLibrary()
        {

        }
    }
}
