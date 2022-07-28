using Org.XmlUnit.Diff;
using System.IO;

namespace NodeSetDiff
{
    static internal class Program
    {
        static void Main(string[] args)
        {
            var compareFileName
            = @"..\..\..\..\..\TestNodeSets\opcfoundation.org.UA.Robotics.NodeSet2.xml";
            var testFileName
            = @"..\..\..\..\..\TestNodeSets\opcfoundation.org.UA.Robotics.Nodeset2.Exported.xml";

            Diff d = OpcNodeSetXmlUnit.DiffNodeSetFiles(compareFileName, testFileName);

            string diffControl, diffTest, diffSummary;
            OpcNodeSetXmlUnit.GenerateDiffSummary(d, out diffControl, out diffTest, out diffSummary);

            File.WriteAllText("summarydiff.xml", diffSummary);
            File.WriteAllText("controldiff.xml", diffControl);
            File.WriteAllText("testdiff.xml", diffTest);
        }

    }
}

