using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMIP.JsonIO.Model
{
    public class SmipEnumerationType:SmipNode
    {

        [JsonProperty("enumeration_names")]
        public List<string> EnumerationNames { get; set; } 


        [JsonProperty("enumeration_color_codes")]
        public List<string> EnumerationColorCodes { get; set; } 


        [JsonProperty("enumeration_descriptions")]
        public List<string> EnumerationDescriptions { get; set; } 


        [JsonProperty("default_enumeration_values")]
        public List<string> DefaultEnumerationValues { get; set; } 


        public SmipEnumerationType()
        {
            
            EnumerationNames = new List<string>();
            EnumerationColorCodes = new List<string>();
            EnumerationDescriptions = new List<string>();
            DefaultEnumerationValues = new List<string>();

        }
    }
}
