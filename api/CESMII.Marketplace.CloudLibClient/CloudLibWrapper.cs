using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Opc.Ua.Cloud.Library.Client;
using CESMII.OpcUa.NodeSetImporter;
using Microsoft.Extensions.Options;

namespace CESMII.ProfileDesigner.CloudLibClient
{
    public class CloudLibWrapper : ICloudLibWrapper
    {
        private readonly UACloudLibClient _client;
        private readonly UANodeSetCloudLibraryResolver _cloudLibResolver;
        private readonly ILogger<CloudLibWrapper> _logger;

        public OnResolveNodeSets OnResolveNodeSets
        {
            get => _cloudLibResolver.OnResolveNodeSets;
            set => _cloudLibResolver.OnResolveNodeSets = value;
        }
        public OnNodeSet OnDownloadNodeSet
        {
            get => _cloudLibResolver.OnDownloadNodeSet;
            set => _cloudLibResolver.OnDownloadNodeSet = value;
        }
        public OnNodeSet OnNodeSetFound
        {
            get => _cloudLibResolver.OnNodeSetFound;
            set => _cloudLibResolver.OnNodeSetFound = value;
        }
        public OnNodeSet OnNodeSetNotFound
        {
            get => _cloudLibResolver.OnNodeSetNotFound;
            set => _cloudLibResolver.OnNodeSetNotFound = value;
        }

        public CloudLibWrapper(IOptions<UACloudLibClient.Options> cloudLibOptions, ILogger<CloudLibWrapper> logger)
        {
            _logger = logger;

            //initialize cloud lib client
            _client = new UACloudLibClient(cloudLibOptions.Value);
            _cloudLibResolver = new UANodeSetCloudLibraryResolver(_client);
        }

        public Task<IEnumerable<string>> ResolveNodeSetsAsync(List<ModelNameAndVersion> missingModels)
        {
            return _cloudLibResolver.ResolveNodeSetsAsync(missingModels);
        }

        public async Task<GraphQlResult<Nodeset>> SearchAsync(int limit, string cursor, List<string> keywords, List<string> exclude)
        {
            var result = await _client.GetNodeSets(keywords: keywords?.ToArray(), after: cursor, first: limit);

            return result;

            //var result = await _client.GetBasicNodesetInformationAsync(skip, limit, keywords).ConfigureAwait(false);
            ////TBD - don't need this. Keep it here for now in case we temporarily need to get more data.
            ////foreach (var nodeset in result)
            ////{
            ////    //get more details
            ////}

            ////if exclude has values, strip out nodesets where exclude value == nodeset uri
            //if (exclude != null && result != null)
            //{
            //    result = result.Where(x => !exclude.Any(y => y.ToLower().Equals(x.NameSpaceUri.ToLower()))).ToList();
            //}

            //return result ?? new List<UANodesetResult>();
        }

        public async Task<UANameSpace> DownloadAsync(string id)
        {
            var result = await _client.DownloadNodesetAsync(id).ConfigureAwait(false);
            return result;
        }

        public async Task<UANameSpace?> GetAsync(string namespaceUri, DateTime? publicationDate, bool exactMatch)
        {
            uint? id;
            var nodeSetResult = await _client.GetNodeSets(namespaceUri: namespaceUri, publicationDate: publicationDate);
            id = nodeSetResult.Edges?.FirstOrDefault()?.Node?.Identifier;

            if (id == null && !exactMatch)
            {
                nodeSetResult = await _client.GetNodeSets(namespaceUri: namespaceUri);
                id = nodeSetResult.Edges?.OrderByDescending(n => n.Node.PublicationDate).FirstOrDefault(n => n.Node.PublicationDate >= publicationDate)?.Node?.Identifier;
            }
            if (id == null)
            {
                return null;
            }

            var uaNamespace = await _client.DownloadNodesetAsync(id.ToString());
            return uaNamespace;
        }
    }
}
