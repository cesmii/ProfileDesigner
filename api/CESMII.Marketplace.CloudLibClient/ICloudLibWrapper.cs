using System;
using System.Collections.Generic;

using Opc.Ua.Cloud.Library.Client;
using CESMII.OpcUa.NodeSetImporter;

namespace CESMII.ProfileDesigner.CloudLibClient
{
    public interface ICloudLibWrapper
    {
        Task<IEnumerable<string>> ResolveNodeSetsAsync(List<ModelNameAndVersion> missingModels);
        Task<GraphQlResult<Nodeset>> Search(int limit, string cursor, List<string> keywords, List<string> exclude);
        Task<UANameSpace> GetById(string id);
    }
}
