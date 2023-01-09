using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMIP.JsonIO.Model
{
    public class SmipQuantity
    {

        [JsonProperty("quantity_symbol")]
        public string QuantitySymbol { get; set; }

        public SmipQuantity()
        {
            QuantitySymbol = "";
        }

    }
}
