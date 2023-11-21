/* Author:      Markus Horstmann, C-Labs
 * Last Update: 4/13/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2022
 */

using Opc.Ua.Cloud.Library.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CESMII.OpcUa.NodeSetImporter
{
    public class UANodeSetCloudLibraryResolver : IUANodeSetResolverWithPending
    {
        public OnResolveNodeSets OnResolveNodeSets { get; set; }
        public OnNodeSet OnDownloadNodeSet { get; set; }
        public OnNodeSet OnNodeSetFound { get; set; }
        public OnNodeSet OnNodeSetNotFound { get; set; }

        public Func<Nodeset, bool> FilterPendingNodeSet { get; set; }

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
        public UANodeSetCloudLibraryResolver(UACloudLibClient.Options options) : this(options.EndPoint, options.Username, options.Password)
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

            OnResolveNodeSets?.Invoke();

            var nodesetWithURIAndDate = new List<(string NamespaceUri, DateTime? PublicationDate, string Identifier, string NodesetXml)?>();
            try
            {
                // Attempt to use the new API first
                foreach (var missingModel in missingModels)
                {
                    var nodeSets = await _client.GetNodeSetDependencies(modelUri: missingModel.ModelUri).ConfigureAwait(false);
                    foreach (var nodeSet in nodeSets)
                    {
                        nodesetWithURIAndDate.Add((nodeSet.NamespaceUri.OriginalString, nodeSet.PublicationDate, nodeSet.Identifier.ToString(CultureInfo.InvariantCulture), nodeSet.NodesetXml));
                    }
                    try
                    {
                        if (FilterPendingNodeSet != null)
                        {
                            var pendingNodeSets = await _client.GetNodeSetsPendingApprovalAsync(namespaceUri: missingModel.ModelUri).ConfigureAwait(false);
                            var filteredPending = pendingNodeSets.Nodes.Where(FilterPendingNodeSet).ToList();
                            foreach(var pending in filteredPending)
                            {
                                var pendingNodeSetDownload = await _client.GetNodeSetDependencies(identifier: pending.Identifier.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                                foreach (var nodeSet in pendingNodeSetDownload)
                                {
                                    nodesetWithURIAndDate.Add((nodeSet.NamespaceUri.OriginalString, nodeSet.PublicationDate, nodeSet.Identifier.ToString(CultureInfo.InvariantCulture), nodeSet.NodesetXml));
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            catch (GraphQlNotSupportedException)
            {
                // Fall back to retrieving and downloading all matching namespaces
                var namespacesAndIds = await _client.GetNamespaceIdsAsync().ConfigureAwait(false);
                if (namespacesAndIds != null)
                {
                    var matchingNamespacesAndIds = namespacesAndIds.Where(nsid => missingModels.Any(m => m.ModelUri == nsid.Item1)).ToList();
                    foreach (var nsid in matchingNamespacesAndIds)
                    {
                        OnDownloadNodeSet?.Invoke(nsid.NamespaceUri, null);
                        var nodeSet = await _client.DownloadNodesetAsync(nsid.Identifier).ConfigureAwait(false);
                        nodesetWithURIAndDate.Add((
                            nodeSet.Nodeset.NamespaceUri?.OriginalString ?? nsid.NamespaceUri, // TODO cloud lib currently doesn't return the namespace uri: report issue/fix
                            nodeSet.Nodeset.PublicationDate,
                            (string) null,
                            nodeSet.Nodeset.NodesetXml));
                    }
                }
            }
            foreach (var missing in missingModels)
            {
                // Find exact match or lowest matching version
                var bestMatch = nodesetWithURIAndDate.FirstOrDefault(n => n?.Item1 == missing.ModelUri && n?.Item2 == missing.PublicationDate);
                if (bestMatch == null)
                {
                    bestMatch = nodesetWithURIAndDate.Where(n => n?.Item1 == missing.ModelUri && (missing.PublicationDate == null || n?.Item2 >= missing.PublicationDate)).OrderBy(m => m?.Item2).FirstOrDefault();
                }
                if (bestMatch != null)
                {
                    string nodesetXml = bestMatch.Value.NodesetXml;
                    if (string.IsNullOrEmpty(nodesetXml) && !string.IsNullOrEmpty(bestMatch.Value.Identifier))
                    {
                        OnDownloadNodeSet?.Invoke(bestMatch?.NamespaceUri, bestMatch?.PublicationDate);
                        var nodeSet = await _client.DownloadNodesetAsync(bestMatch?.Identifier).ConfigureAwait(false);
                        nodesetXml = nodeSet?.Nodeset?.NodesetXml;
                    }
                    if (!string.IsNullOrEmpty(nodesetXml))
                    {
                        OnNodeSetFound?.Invoke(bestMatch?.NamespaceUri, bestMatch?.PublicationDate); 
                        downloadedNodeSets.Add(nodesetXml);
                    }
                    else
                    {
                        OnNodeSetNotFound?.Invoke(missing.ModelVersion, missing.PublicationDate);
                    }
                }
                else
                {
                    OnNodeSetNotFound?.Invoke(missing.ModelVersion, missing.PublicationDate);
                }
            }
            return downloadedNodeSets;
        }
    }
}
