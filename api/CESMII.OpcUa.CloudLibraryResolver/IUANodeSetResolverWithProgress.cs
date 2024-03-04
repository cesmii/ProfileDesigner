/* Author:      Markus Horstmann, C-Labs
 * Last Update: 5/26/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2022
 */

using Opc.Ua.Cloud.Library.Client;
using System;

namespace CESMII.OpcUa.NodeSetImporter
{
    public interface IUANodeSetResolverWithProgress : IUANodeSetResolver
    {
        public OnResolveNodeSets OnResolveNodeSets { get; set; }
        public OnNodeSet OnDownloadNodeSet { get; set; }
        public OnNodeSet OnNodeSetFound { get; set; }
        public OnNodeSet OnNodeSetNotFound { get; set; }

    }
    public delegate void OnResolveNodeSets();
    public delegate void OnNodeSet(string namespaceUri, DateTime? publicationDate);

    public interface IUANodeSetResolverWithPending : IUANodeSetResolverWithProgress
    {
        public Func<Nodeset, bool> FilterPendingNodeSet { get; set; }
    }
}
