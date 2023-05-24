using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace SMIP.JsonIO.Model
{
    public class SmipNode
    {
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("relative_name")]
        public string RelativeName { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("fqn")]
        public List<string> Fqn { get; set; }

        [JsonProperty("document")]
        public JObject? Document { get; set; }


        public SmipNode()
        {
            Fqn = new List<string>();
            RelativeName = "";
            DisplayName = "";
        }
    }
}
