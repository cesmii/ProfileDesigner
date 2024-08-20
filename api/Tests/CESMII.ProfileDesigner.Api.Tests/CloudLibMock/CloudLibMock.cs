using CESMII.OpcUa.NodeSetImporter;
using CESMII.Common.CloudLibClient;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Library.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CESMII.ProfileDesigner.Api.Tests
{
    public class CloudLibMock : ICloudLibWrapper, IDisposable
    {
        private readonly ICloudLibWrapper _wrapper;
        static  bool recording = false;
        const string strSearchDataFile = "CloudLibMock/Searchdata.json";
        public CloudLibMock()
        {
        }

        public CloudLibMock(CloudLibWrapper wrapper)
        {
            if (!recording && File.Exists(strSearchDataFile))
            {
                if (_searchData?.Any() != true)
                {
                    var searchDataJson = File.ReadAllText(strSearchDataFile);
                    var sd = JsonConvert.DeserializeObject<List<KeyValuePair<SearchInputs, GraphQlResult<Nodeset>>>>(searchDataJson);
                    var comparer = new SearchInputs.Comparer();
                    _searchData = sd.ToDictionary(kv => kv.Key, kv => kv.Value, comparer);
                    _lastSavedCount = _searchData.Count;
                }
            }
            else
            {
                _wrapper = wrapper;
                recording = true;
            }
        }
        public Task<GraphQlResult<Nodeset>> GetManyAsync(List<string> identifiers)
        {
            return null;
        }

        record SearchInputs
        {
            public string[] Keywords { get; set; }
            public string Cursor { get; set; }
            public bool PageBackwards { get; set; }
            public int? Limit { get; set; }

            internal class Comparer : IEqualityComparer<SearchInputs>
            {
                bool IEqualityComparer<SearchInputs>.Equals(SearchInputs x, SearchInputs y)
                {
                    return x == y || 
                        (x.Cursor == y.Cursor 
                         && x.Limit == y.Limit 
                         && (x.Keywords == y.Keywords 
                            || (    x.Keywords != null 
                                 && y.Keywords != null
                                 && x.Keywords.SequenceEqual(y.Keywords)
                               )
                            )
                         );
                }

                int IEqualityComparer<SearchInputs>.GetHashCode(SearchInputs obj)
                {
                    if (obj == null) return 0;
                    unchecked
                    {
                        return obj.Cursor?.GetHashCode() ?? 0 + obj.Limit.GetHashCode() + obj.Keywords?.Aggregate(0, (s, k) =>
                        {
                            unchecked
                            {
                                return s + k.GetHashCode();
                            }
                        }) ?? 0;
                    }
                }
            }
        }
        static Dictionary<SearchInputs, GraphQlResult<Nodeset>> _searchData = new Dictionary<SearchInputs, GraphQlResult<Nodeset>>(new SearchInputs.Comparer());
        static int _lastSavedCount = 0;
        private bool disposedValue;

        public OnResolveNodeSets OnResolveNodeSets { get; set; }
        public OnNodeSet OnDownloadNodeSet { get; set; }
        public OnNodeSet OnNodeSetFound { get; set; }
        public OnNodeSet OnNodeSetNotFound { get; set; }

        public async Task<GraphQlResult<Nodeset>> SearchAsync(int? limit, string cursor, bool pageBackwards, List<string> keywords, List<string> exclude, bool noTotalCount, object? order)
        {
            var inputs = new SearchInputs
            {
                Keywords = keywords?.ToArray(),
                Cursor = cursor,
                PageBackwards = pageBackwards,
                Limit = limit,
            };
            if (_wrapper != null)
            {
                var result = await _wrapper.SearchAsync(limit, cursor, pageBackwards, keywords, exclude, noTotalCount, order);

                if (!_searchData.ContainsKey(inputs))
                {
                    _searchData.Add(inputs, result);
                }
                return result;
            }
            if (_searchData.TryGetValue(inputs, out var data))
            {
                return data;
            }
            throw new Exception($"Request not in mock data: {inputs}");
        }

        public async Task<GraphQlResult<Nodeset>> SearchAsync(int? limit, string cursor, bool pageBackwards, List<string> keywords, List<string> exclude, bool noTotalCount)
        {
            var inputs = new SearchInputs
            {
                Keywords = keywords?.ToArray(),
                Cursor = cursor,
                PageBackwards = pageBackwards,
                Limit = limit,
            };
            if (_wrapper != null)
            {
                var result = await _wrapper.SearchAsync(limit, cursor, pageBackwards, keywords, exclude, noTotalCount);

                if (!_searchData.ContainsKey(inputs))
                {
                    _searchData.Add(inputs, result);
                }
                return result;
            }
            if (_searchData.TryGetValue(inputs, out var data))
            {
                return data;
            }
            throw new Exception($"Request not in mock data: {inputs}");
        }


        public Task<UANameSpace> DownloadAsync(string id)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<string>> ResolveNodeSetsAsync(List<ModelNameAndVersion> missingModels)
        {
            return Task.FromResult(new List<string>().AsEnumerable());
        }


        public Task<UANameSpace> GetAsync(string modelUri, DateTime? publicationDate, bool exactMatch)
        {
            return Task.FromResult<UANameSpace>(null);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                if (recording && _searchData.Any() && _searchData.Count != _lastSavedCount)
                {
                    try
                    {
                        var toSave = _searchData.ToList();
                        File.WriteAllText(strSearchDataFile, JsonConvert.SerializeObject(toSave));
                        _lastSavedCount = toSave.Count;
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                }
                disposedValue = true;
            }
        }

        ~CloudLibMock()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Task<string> UploadAsync(UANameSpace uaNamespace)
        {
            throw new NotImplementedException();
        }

        public Task<GraphQlResult<Nodeset>> GetNodeSetsPendingApprovalAsync(int? limit, string cursor, bool pageBackwards, bool noTotalCount = false, UAProperty prop = null)
        {
            throw new NotImplementedException();
        }
        public Task<UANameSpace> UpdateApprovalStatusAsync(string nodeSetId, string newStatus, string statusInfo, UAProperty additionalProperty = null)
        {
            throw new NotImplementedException();
        }

        public Task<UANameSpace> GetAsync(string identifier)
        {
            throw new NotImplementedException();
        }

    }
}
