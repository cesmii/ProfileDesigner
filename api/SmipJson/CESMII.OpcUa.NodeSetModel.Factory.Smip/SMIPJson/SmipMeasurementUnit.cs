using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMIP.JsonIO.Model
{
    public class SmipMeasurementUnit
    {

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("unece_code")]
        public string? UneceCode { get; set; }

        [JsonProperty("unece_name")]
        public string? UneceName { get; set; }

        [JsonProperty("quantity_fqn")]
        public List<string> QuantityFqn { get; set; }

        [JsonProperty("opcua_unit_id")]
        public int? OpcuaUnitId { get; set; }

        [JsonProperty("conversion_offset")]
        public float ConversionOffset { get; set; }

        [JsonProperty("conversion_multiplier")]
        public float ConversionMultiplier { get; set; }

        public SmipMeasurementUnit()
        {
            Symbol = "";
            QuantityFqn = new List<string>();
            ConversionOffset = 0;
            ConversionMultiplier = 1;
        }

    }
}
