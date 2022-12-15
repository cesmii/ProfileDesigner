using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMIP.JsonIO.Model
{
    public class SmipScript : SmipNode
    {
        public string Script { get; set; }
        public SmipScript()
        {
            Script = "";
        }
    }
}
