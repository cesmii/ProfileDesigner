using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMIP.JsonIO.Model
{
    public class SmipTypeSystem
    {
        [JsonProperty("libraries")]
        public List<SmipLibrary> Libraries { get; set; }

        [JsonProperty("meta")]
        public SmipMeta Meta { get; set; }

        [JsonProperty("types")]
        public List<SmipType> Types { get; set; }


        //[JsonProperty("objects")]
        //public List<SmipObject> Objects { get; set; }


        [JsonProperty("quantities")]
        public List<SmipQuantity> Quantities { get; set; }


        //[JsonProperty("relationships")]
        //public List<SmipRelationship> Relationships { get; set; }


        //[JsonProperty("attribute_types")]
        //public List<SmipAttributeType> AttributeTypes { get; set; }


        //[JsonProperty("opcua_variables")]
        //public List<SmipOpcuaVariable> OpcuaVariables { get; set; }


        //[JsonProperty("opcua_data_types")]
        //public List<SmipOpcuaDataType> OpcuaDataTypes { get; set; }


        //[JsonProperty("script_templates")]
        //public List<SmipScript> Scripts { get; set; }


        [JsonProperty("enumeration_types")]
        public List<SmipEnumerationType> EnumerationTypes { get; set; }


        [JsonProperty("measurement_units")]
        public List<SmipMeasurementUnit> MeasurementUnits { get; set; }


        //[JsonProperty("relationship_types")]
        //public List<SmipRelationshipType> RelationshipTypes { get; set; }


        //[JsonProperty("opcua_variable_types")]
        //public List<SmipOpcuaVariableType> OpcuaVariableTypes { get; set; }


        //[JsonProperty("opcua_reference_types")]
        //public List<SmipOpcuaReferenceType> OpcuaReferenceTypes { get; set; }


        public SmipTypeSystem()
        {
            Meta = new SmipMeta();
            Types = new List<SmipType>();
            Libraries = new List<SmipLibrary>();
            Quantities = new List<SmipQuantity>();
            EnumerationTypes = new List<SmipEnumerationType>();
            MeasurementUnits = new List<SmipMeasurementUnit>();
        }
    }
}
