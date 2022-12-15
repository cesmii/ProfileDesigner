using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMIP.JsonIO.Model
{
    public class SmipAttributeLimit
    {

        [JsonProperty("direction")]
        public string Direction { get; set; }

        [JsonProperty("float_value")]
        public float FloatValue { get; set; }

        public SmipAttributeLimit()
        {
            Direction = "";
        }

    }
}
