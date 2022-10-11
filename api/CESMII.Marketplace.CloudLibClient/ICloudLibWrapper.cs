using System;
using System.Collections.Generic;

using Opc.Ua.Cloud.Library.Client;
using CESMII.OpcUa.NodeSetImporter;

namespace CESMII.ProfileDesigner.CloudLibClient
{
    public interface ICloudLibWrapper
    {
        Task<IEnumerable<string>> ResolveNodeSetsAsync(List<ModelNameAndVersion> missingModels);
        Task<List<UANodesetResult>> Search(int limit, int skip, List<string> keywords, List<string> exclude);
        Task<UANameSpace> GetById(string id);
    }
}
