using CESMII.OpcUa.NodeSetImporter;
using CESMII.ProfileDesigner.CloudLibClient;
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
                }
            }
            else
            {
                _wrapper = wrapper;
                recording = true;
            }
        }
        public Task<UANameSpace> GetById(string id)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<string>> ResolveNodeSetsAsync(List<ModelNameAndVersion> missingModels)
        {
            throw new System.NotImplementedException();
        }

        record SearchInputs
        {
            public string[] Keywords { get; set; }
            public string Cursor { get; set; }
            public int Limit { get; set; }

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
        private bool disposedValue;

        public async Task<GraphQlResult<Nodeset>> Search(int limit, string cursor, List<string> keywords, List<string> exclude)
        {
            var inputs = new SearchInputs
            {
                Keywords = keywords?.ToArray(),
                Cursor = cursor,
                Limit = limit,
            };
            if (_wrapper != null)
            {
                var result = await _wrapper.Search(limit, cursor, keywords, exclude);

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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                if (recording && _searchData.Any())
                {
                    try
                    {
                        File.WriteAllText(strSearchDataFile, JsonConvert.SerializeObject(_searchData.ToList()));
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
    }
}
