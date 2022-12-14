using Newtonsoft.Json;

namespace SMIP.JsonIO.Model
{
    public class SmipTypeComposition : SmipNode
    {
        [JsonProperty("child_type_fqn")]
        public string[] ChildTypeFqn { get; set; }

        [JsonProperty("is_required")]
        public bool IsRequired { get; set; }

    }
}