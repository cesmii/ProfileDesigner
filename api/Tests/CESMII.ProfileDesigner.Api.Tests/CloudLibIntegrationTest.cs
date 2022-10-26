using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CESMII.ProfileDesigner.Api.Controllers;
using Microsoft.Extensions.DependencyInjection;
using MyNamespace;
using NLog.Filters;
using NodeSetDiff;
using Opc.Ua.Export;
using Org.XmlUnit.Diff;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CESMII.ProfileDesigner.Api.Tests
{
    [TestCaseOrderer("CESMII.ProfileDesigner.Api.Tests.ImportExportIntegrationTestCaseOrderer", "CESMII.ProfileDesigner.Api.Tests")]
    public class CloudLib : IClassFixture<CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup>>
    {
        private readonly CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup> _factory;
        private readonly ITestOutputHelper output;

        public CloudLib(CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup> factory, ITestOutputHelper output)
        {
            _factory = factory;
            this.output = output;
        }
    
        [Theory]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibrary(string[] keywords, int expectedCount, int expectedNotLocal, int expectedPlusLocal, int expectedNotLocalPlusLocal)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var allLocalProfiles = (await apiClient.LibraryAsync(new PagerFilterSimpleModel { Query = null, Skip = 0, Take = 100 })).Data;
            var allCloudProfiles = await PagedVsNonPagedAsync(apiClient, new CloudLibFilterModel { Keywords = null, Cursor = null, Take = 100, });

            var cloud = await PagedVsNonPagedAsync(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 100, });
            Assert.Equal(expectedCount, cloud.Count);

            var cloudNotLocal = await PagedVsNonPagedAsync(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 100, ExcludeLocalLibrary = true, });
            Assert.Equal(expectedNotLocal, cloudNotLocal.Count);

            var findLocals = cloudNotLocal.Where(c => allLocalProfiles.Any(l => l.Namespace == c.Namespace)).ToList();
            Assert.Empty(findLocals);

            var cloudPlusLocal = await PagedVsNonPagedAsync(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 100, AddLocalLibrary = true, });
            Assert.Equal(expectedPlusLocal, cloudPlusLocal.Count);

            var cloudNotLocalPlusLocal = await PagedVsNonPagedAsync(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 100, ExcludeLocalLibrary = true, AddLocalLibrary = true });
            Assert.Equal(expectedNotLocalPlusLocal, cloudNotLocalPlusLocal.Count);
        }

        [Theory]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibrarySingle(string[] keywords, int expectedCount, int expectedNotLocal, int expectedPlusLocal, int expectedNotLocalPlusLocal)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var cloud = await apiClient.CloudlibraryAsync(new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 100, });
            Assert.Equal(expectedCount, cloud.Count);
        }
        [Theory]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibraryPaged(string[] keywords, int expectedCount, int expectedNotLocal, int expectedPlusLocal, int expectedNotLocalPlusLocal)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var cloudPaged = await GetAllPaged(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 5, });
            Assert.Equal(expectedCount, cloudPaged.Count);
        }

        [Theory]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibraryPagedNoLocal(string[] keywords, int expectedCount, int expectedNotLocal, int expectedPlusLocal, int expectedNotLocalPlusLocal)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var cloudPaged = await GetAllPaged(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 5, ExcludeLocalLibrary = true });
            Assert.Equal(expectedNotLocal, cloudPaged.Count);
        }
        [Theory]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibraryPagedNoLocalPlusLocal(string[] keywords, int expectedCount, int expectedNotLocal, int expectedPlusLocal, int expectedNotLocalPlusLocal)
        {
            // Note: the current results seem incomplete because local profile keyword search only matches the namespace, not the displaynames of hte profile type definitions etc.
            // Once CloudLib and local keyword search are identical, this should return the same counts as expectedPlusLocal

            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var cloudPaged = await GetAllPaged(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 5, AddLocalLibrary = true, ExcludeLocalLibrary = true });
            Assert.Equal(expectedNotLocalPlusLocal, cloudPaged.Count);
        }

        [Theory]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibraryPagedAddLocal(string[] keywords, int expectedCount, int expectedNotLocal, int expectedPlusLocal, int expectedNotLocalPlusLocal)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var cloudPaged = await GetAllPaged(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 5, AddLocalLibrary = true });
            Assert.Equal(expectedPlusLocal, cloudPaged.Count);
        }

        private async Task<ICollection<CloudLibProfileModel>> PagedVsNonPagedAsync(Client apiClient, CloudLibFilterModel filter)
        {
            var cloud = (await apiClient.CloudlibraryAsync(filter)).Data;

            var pagedFilter = new CloudLibFilterModel
            {
                Keywords = filter.Keywords,
                ExcludeLocalLibrary = filter.ExcludeLocalLibrary,
                AddLocalLibrary = filter.AddLocalLibrary,
                Cursor = null,
                Take = 5,
            };

            List<CloudLibProfileModel> paged = await GetAllPaged(apiClient, pagedFilter);
            Assert.True(paged.Count == cloud.Count);
            Assert.Equal(cloud, paged/*.Take(cloud.Count)*/, new CloudLibProfileComparer());

            return cloud;
        }

        private async Task<List<CloudLibProfileModel>> GetAllPaged(Client apiClient, CloudLibFilterModel pagedFilter)
        {
            bool bComplete = false;
            var paged = new List<CloudLibProfileModel>();
            do
            {
                var page = await apiClient.CloudlibraryAsync(pagedFilter);
                Assert.True(page.Data.Count <= pagedFilter.Take, "CloudLibAsync returned more profiles than requested");
                paged.AddRange(page.Data);
                if (page.Data.Count < pagedFilter.Take || page.Data.Count == 0)
                {
                    bComplete = true;
                }
                //pagedFilter.Skip += pagedFilter.Take;
                pagedFilter.Cursor = page.Cursor;
            } while (!bComplete && paged.Count < 100);
            output.WriteLine($"Filter: {paged.Count}");
            return paged;
        }

        public static IEnumerable<object[]> TestKeywords()
        {
            return new List<object[]>
            {
                // string[] keywords, int expectedCount, int expectedNotLocal, int expectedPlusLocal, int expectedNotLocalPlusLocal 
                new object[ ]{ null, 63, 7, 67, 67, },
                new object[] { new string[] { "BaseObjectType" }, 6, 0, 6, 0, },
                new object[] { new string[] { "di" }, 61, 5, 61, 11, },
                new object[] { new string[] { "robotics" }, 2, 1, 2, 2, },
                new object[] { new string[] { "plastic" }, 15, 0, 15, 14, },
                new object[] { new string[] { "pump" } , 6, 0, 6, 2,},
                new object[] { new string[] { "robotics", "di" }, 61, 5, 61, 12, },
                new object[] { new string[] { "robotics", "di", "pump", "plastic" }, 61, 5, 61, 26, },
                new object[] { new string[] { "robotics", "pump", }, 8, 1, 8, 4, },
                new object[] { new string[] { "robotics", "plastic" }, 17, 1, 17, 16, },
                new object[] { new string[] { "robotics", "pump", "plastic" }, 20, 1, 20, 17, },
                new object[] { new string[] { "abcdefg", "defghi", "dhjfhsdjfhsdjkfhsdjkf", "dfsjdhfjkshdfjksd" } , 0, 0, 0, 0, },
                new object[] { new string[] { "Interface" }, 24, 0, 24, 0, },
                new object[] { new string[] { "Event" }, 23, 1, 23, 1, },
                new object[] { new string[] { "Interface", "BaseObjectType" }, 28, 0, 28, 0, },
                new object[] { new string[] { "BaseObjectType", "Interface" }, 28, 0, 28, 0, },
                new object[] { new string[] { "Interface", "BaseObjectType", "Event" }, 40, 1, 40, 1, },
            };
        }
    }

    internal class CloudLibProfileComparer : IEqualityComparer<CloudLibProfileModel>
    {
        public bool Equals(CloudLibProfileModel x, CloudLibProfileModel y)
        {
            return x.Namespace == y.Namespace && x.PublishDate == y.PublishDate;
        }

        public int GetHashCode([DisallowNull] CloudLibProfileModel p)
        {
            return p.Namespace.GetHashCode() + p.PublishDate.GetHashCode();
        }
    }
}
