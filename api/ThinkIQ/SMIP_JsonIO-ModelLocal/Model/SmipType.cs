using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SMIP.JsonIO.Model
{
    public class SmipType : SmipNode
    {
        [JsonProperty("scripts")]
        public List<SmipScript> Scripts { get; set; }


        [JsonProperty("attributes")]
        public List<SmipTypeAttribute> Attributes { get; set; }

        //[JsonProperty("opcua_methods")]
        //public List<SmipOpcuaMethods> OpcuaMethods { get; set; }


        [JsonProperty("classification")]
        public string Classification { get; set; }


        [JsonProperty("child_equipment")]
        public List<SmipTypeComposition> ChildEquipment { get; set; }


        [JsonProperty("sub_type_of_fqn")]
        public List<string> SubTypeOfFqn { get; set; }

        public SmipType()
        {
            Scripts = new List<SmipScript>();
            Attributes = new List<SmipTypeAttribute>();
            Classification = "object";
            ChildEquipment = new List<SmipTypeComposition>();
            SubTypeOfFqn = new List<string>();
        }
    }
}