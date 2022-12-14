using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMIP.JsonIO.Model
{
    public class SmipTypeAttribute : SmipNode
    {

        [JsonProperty("data_type")]
        public string DataType { get; set; }

        [JsonProperty("is_required")]
        public bool IsRequired { get; set; }

        [JsonProperty("is_hidden")]
        public bool IsHidden { get; set; }

        [JsonProperty("max_value")]
        public float? MaxValue { get; set; }

        [JsonProperty("min_value")]
        public float? MinValue { get; set; }

        [JsonProperty("expression")]
        public string? Expression { get; set; }

        [JsonProperty("importance")]
        public float Importance { get; set; }

        [JsonProperty("default_value")]
        public string? DefaultValue { get; set; }

        [JsonProperty("source_category")]
        public string SourceCategory { get; set; }

        [JsonProperty("attribute_limits")]
        public List<SmipAttributeLimit> AttributeLimits { get; set; }

        //[JsonProperty("enumeration_type_id")] // Is this correct? Couldn't find in any export
        //public int? EnumerationTypeId { get; set; }

        // default_enumeration_values ?
        // attribute_type_fqn

        [JsonProperty("enumeration_type_fqn")]
        public List<string>? EnumerationTypeFqn { get; set; }

        [JsonProperty("interpolation_method")]
        public string InterpolationMethod { get; set; }

        [JsonProperty("default_measurement_unit_fqn")]
        public List<string>? MeasurementUnitFqn { get; set; }

        // Is this correct? Couldn't find in any export
        //[JsonProperty("type_to_attribute_type_fqn")]
        //public List<string> TypeToAttributeTypeFqn { get; set; }

        public SmipTypeAttribute()
        {
            DataType = "";
            IsHidden = false;
            SourceCategory = "dynamic";
            AttributeLimits = new List<SmipAttributeLimit>();
            InterpolationMethod = "previous";
            MeasurementUnitFqn = new List<string>();
            //TypeToAttributeTypeFqn = new List<string>();
        }
    }
}
