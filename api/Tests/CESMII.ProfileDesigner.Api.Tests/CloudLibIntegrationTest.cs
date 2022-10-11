using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Packaging;
using System.Linq;
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
    public partial class Integration
    {
        [Theory]
        [InlineData(null)]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibrary(string[] keywords)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var allLocalProfiles = (await apiClient.LibraryAsync(new PagerFilterSimpleModel { Query = null, Skip = 0, Take = 100 })).Data;
            var allCloudProfiles = await PagedVsNonPagedAsync(apiClient, new CloudLibFilterModel { Keywords = null, Cursor = null, Take = 100, });

            var cloud = await PagedVsNonPagedAsync(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 100,  });
            var cloudNotLocal = await PagedVsNonPagedAsync(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 100, ExcludeLocalLibrary = true, });

            var findLocals = cloudNotLocal.Where(c => allLocalProfiles.Any(l => l.Namespace == c.Namespace)).ToList();
            Assert.Empty(findLocals);

            var cloudPlusLocal = await PagedVsNonPagedAsync(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 100, AddLocalLibrary = true, });


            var cloudNotLocalPlusLocal = await PagedVsNonPagedAsync(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 100, ExcludeLocalLibrary = true, AddLocalLibrary = true });
        }

        [Theory]
        [InlineData(null)]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibrarySingle(string[] keywords)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var cloud = await apiClient.CloudlibraryAsync(new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 100, });
        }
        [Theory]
        [InlineData(null)]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibraryPaged(string[] keywords)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var cloudPaged = await GetAllPaged(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 5, });
        }

        [Theory]
        [InlineData(null)]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibraryPagedNoLocal(string[] keywords)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var cloudPaged = await GetAllPaged(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 5, ExcludeLocalLibrary = true });
        }

        [Theory]
        [InlineData(null)]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibraryPagedAddLocal(string[] keywords)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var cloudPaged = await GetAllPaged(apiClient, new CloudLibFilterModel { Keywords = keywords, Cursor = null, Take = 5, AddLocalLibrary = true });
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

        private static async Task<List<CloudLibProfileModel>> GetAllPaged(Client apiClient, CloudLibFilterModel pagedFilter)
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
            return paged;
        }

        public static IEnumerable<object[]> TestKeywords()
        {
            return new List<object[]> 
            {
                new object[] { new string[] { "Type" } },
                new object[] { new string[] { "robotics" } },
                new object[] { new string[] { "di" } },
                new object[] { new string[] { "robotics", "di" } },
                new object[] { new string[] { "robotics", "di", "pump", "plastic" } },
                new object[] { new string[] { "abcdefg", "defghi", "dhjfhsdjfhsdjkfhsdjkf", "dfsjdhfjkshdfjksd" } },
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
