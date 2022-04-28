/* Author:      Markus Horstmann, C-Labs
 * Last Update: 4/13/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2022
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua.CloudLib.Client;

namespace CESMII.OpcUa.NodeSetImporter
{
    public class UANodeSetCloudLibraryResolver : IUANodeSetResolver
    {
        public class CloudLibraryOptions
        {
            public string EndPoint { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
        }
        public UANodeSetCloudLibraryResolver(string strUserName, string strPassword)
        {
            _client = new UACloudLibClient(strUserName, strPassword);
        }
        public UANodeSetCloudLibraryResolver(string strEndPoint, string strUserName, string strPassword)
        {
            if (string.IsNullOrEmpty(strEndPoint))
            {
                _client = new UACloudLibClient(strUserName, strPassword);
            }
            else
            {
                _client = new UACloudLibClient(strEndPoint, strUserName, strPassword);
            }
        }
        public UANodeSetCloudLibraryResolver(CloudLibraryOptions options) : this(options.EndPoint, options.UserName, options.Password)
        {
        }
        public UANodeSetCloudLibraryResolver(UACloudLibClient client)
        {
            _client = client;
        }

        private readonly UACloudLibClient _client;
        public async Task<IEnumerable<String>> ResolveNodeSetsAsync(List<ModelNameAndVersion> missingModels)
        {
            var downloadedNodeSets = new List<string>();

            // TODO Is there an API to download matching nodeset directly via URI/PublicationDate? Should we push to add this?
            var namespacesAndIds = await _client.GetNamespaceIdsAsync().ConfigureAwait(false);
            if (namespacesAndIds != null)
            {
                var matchingNamespacesAndIds = namespacesAndIds.Where(nsid => missingModels.Any(m => m.ModelUri == nsid.Item1)).ToList();
                var nodesetWithURIAndDate = new List<(string, DateTime, string)?>();
                foreach (var nsid in matchingNamespacesAndIds)
                {
                    var nodeSet = await _client.DownloadNodesetAsync(nsid.Identifier).ConfigureAwait(false);
                    nodesetWithURIAndDate.Add((
                        nodeSet.Nodeset.NamespaceUri?.ToString() ?? nsid.NamespaceUri, // TODO cloud lib currently doesn't return the namespace uri: report issue/fix
                        nodeSet.Nodeset.PublicationDate, 
                        nodeSet.Nodeset.NodesetXml));
                }

                foreach (var missing in missingModels)
                {
                    // Find exact match or lowest matching version
                    var bestMatch = nodesetWithURIAndDate.FirstOrDefault(n => n?.Item1 == missing.ModelUri && n?.Item2 == missing.PublicationDate);
                    if (bestMatch == null)
                    {
                        bestMatch = nodesetWithURIAndDate.Where(n => n?.Item1 == missing.ModelUri && n?.Item2 >= missing.PublicationDate).OrderBy(m => m?.Item2).FirstOrDefault();
                    }
                    if (bestMatch != null)
                    {
                        downloadedNodeSets.Add(bestMatch?.Item3);
                    }
                }
            }
            return downloadedNodeSets;
        }
    }
}
