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
using CESMII.ProfileDesigner.Common.Enums;
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

        //shared filter used to represent an inclusion of local profiles 
        private List<LookupGroupByModel> FilterIncludeLocalItems
        {
            get
            {
                return new List<LookupGroupByModel>{
                    new LookupGroupByModel() {
                        Name = "Source", Id = (int)ProfileSearchCriteriaCategoryEnum.Source,
                        Items = new List<LookupItemFilterModel>() {
                            new LookupItemFilterModel() { Id = (int)ProfileSearchCriteriaSourceEnum.BaseProfile, Selected = true }
                        }
                    }
                };
            }
        }

        //shared filter used to represent an exclusion of local profiles 
        private List<LookupGroupByModel> FilterExcludeLocalItems
        {
            get
            {
                return new List<LookupGroupByModel>{
                    new LookupGroupByModel() {
                        Name = "Source", Id = (int)ProfileSearchCriteriaCategoryEnum.Source,
                        Items = new List<LookupItemFilterModel>() {
                            new LookupItemFilterModel() { Id = (int)ProfileSearchCriteriaSourceEnum.BaseProfile, Selected = false }
                        }
                    }
                };
            }
        }

        private List<LookupGroupByModel> FilterAddLocalProfiles
        {
            get
            {
                return new List<LookupGroupByModel>{
                    new LookupGroupByModel() {
                        Name = "Source",
                        Id = (int)ProfileSearchCriteriaCategoryEnum.Source,
                        Items = new List<LookupItemFilterModel>() {
                            new LookupItemFilterModel() { Id = (int)ProfileSearchCriteriaSourceEnum.BaseProfile, Selected = true },
                            new LookupItemFilterModel() { Id = 999, Selected = true }
                        }
                    }
                };
            }
        }
        private List<LookupGroupByModel> FilterReplaceCloudLibWithLocal
        {
            get
            {
                return new List<LookupGroupByModel>{
                    new LookupGroupByModel() {
                        Name = "Source",
                        Id = (int)ProfileSearchCriteriaCategoryEnum.Source,
                        Items = new List<LookupItemFilterModel>() {
                            new LookupItemFilterModel() { Id = (int)ProfileSearchCriteriaSourceEnum.BaseProfile, Selected = false },
                            new LookupItemFilterModel() { Id = 999, Selected = true }
                        }
                    }
                };
            }
        }

        public CloudLib(CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup> factory, ITestOutputHelper output)
        {
            _factory = factory;
            this.output = output;
        }

        [Theory]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibraryCombo(string query, int expectedCount, int expectedNotLocal, int expectedPlusLocal) // , int expectedNotLocalPlusLocal)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var allLocalProfiles = (await apiClient.LibraryAsync(new PagerFilterSimpleModel { Query = null, Skip = 0, Take = 100 })).Data;
            var allCloudProfiles = await PagedVsNonPagedAsync(apiClient, new CloudLibFilterModel { Query = null, Cursor = null, Take = 100, Filters = FilterIncludeLocalItems });

            var cloud = await PagedVsNonPagedAsync(apiClient, new CloudLibFilterModel { Query = query, Cursor = null, Take = 100, Filters = FilterIncludeLocalItems });
            Assert.Equal(expectedCount, cloud.Count);

            var cloudNotLocal = await PagedVsNonPagedAsync(apiClient, new CloudLibFilterModel { Query = query, Cursor = null, Take = 100, Filters = FilterExcludeLocalItems });
            Assert.Equal(expectedNotLocal, cloudNotLocal.Count);

            var findLocals = cloudNotLocal.Where(c => allLocalProfiles.Any(l => l.Namespace == c.Namespace)).ToList();
            Assert.Empty(findLocals);

            var cloudPlusLocal = await PagedVsNonPagedAsync(apiClient, new CloudLibFilterModel { Query = query, Cursor = null, Take = 100, Filters = FilterAddLocalProfiles });
            Assert.Equal(expectedPlusLocal, cloudPlusLocal.Count);
        }

#pragma warning disable xUnit1026  // Stop warnings related to parameters not used in test cases. 

        [Theory]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibrarySingle(string query, int expectedCount, int expectedNotLocal, int expectedPlusLocal, int expectedNotLocalPlusLocal)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var cloud = await apiClient.CloudlibraryAsync(new CloudLibFilterModel { Query = query, Cursor = null, Take = 100, Filters = FilterIncludeLocalItems });
            Assert.Equal(expectedCount, cloud.Count);
        }
        [Theory]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibraryPaged(string query, int expectedCount, int expectedNotLocal, int expectedPlusLocal, int expectedNotLocalPlusLocal)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var cloudPaged = await GetAllPaged(apiClient, new CloudLibFilterModel { Query = query, Cursor = null, Take = 5, Filters = FilterIncludeLocalItems });
            Assert.Equal(expectedCount, cloudPaged.Count);
        }

        [Theory]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibraryPagedNoLocal(string query, int expectedCount, int expectedNotLocal, int expectedPlusLocal, int expectedNotLocalPlusLocal)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var cloudPaged = await GetAllPaged(apiClient, new CloudLibFilterModel { Query = query, Cursor = null, Take = 5, Filters = FilterExcludeLocalItems });
            Assert.Equal(expectedNotLocal, cloudPaged.Count);
        }

        [Theory]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibraryPagedNoLocalPlusLocal(string query, int expectedCount, int expectedNotLocal, int expectedPlusLocal, int expectedNotLocalPlusLocal)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var cloudPaged = await GetAllPaged(apiClient, new CloudLibFilterModel { Query = query, Cursor = null, Take = 5, Filters = FilterReplaceCloudLibWithLocal });
            Assert.Equal(expectedNotLocalPlusLocal, cloudPaged.Count);
        }

        [Theory]
        [MemberData(nameof(TestKeywords))]
        public async Task CloudLibraryPagedAddLocal(string query, int expectedCount, int expectedNotLocal, int expectedPlusLocal, int expectedNotLocalPlusLocal)
        {
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var cloudPaged = await GetAllPaged(apiClient, new CloudLibFilterModel { Query = query, Cursor = null, Take = 5, Filters = FilterAddLocalProfiles });
            Assert.Equal(expectedPlusLocal, cloudPaged.Count);
        }

        private async Task<ICollection<CloudLibProfileModel>> PagedVsNonPagedAsync(Client apiClient, CloudLibFilterModel filter)
        {
            var pagedFilter = new CloudLibFilterModel
            {
                Query = filter.Query,
                Filters = filter.Filters,
                Cursor = null,
                Take = 5,
            };

            List<CloudLibProfileModel> cloud = new();
            CloudLibProfileModelDALResult result;
            do
            {
                result = await apiClient.CloudlibraryAsync(filter);
                cloud.AddRange(result.Data);
                filter.Cursor = result.EndCursor;
            } while (result.HasNextPage == true);


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
                bComplete = page.HasNextPage != true;
                pagedFilter.Cursor = page.EndCursor;
            } while (!bComplete/* && paged.Count < 100*/);
            output.WriteLine($"Filter: {paged.Count}");
            return paged;
        }

        public static IEnumerable<object[]> TestKeywords()
        {
            return new List<object[]>
            {
                // string query, int expectedCount, int expectedNotLocal, int expectedPlusLocal, int expectedNotLocalPlusLocal 
                new object[ ]{ null, 64, 3, 123, 112, },
                new object[] { "BaseObjectType", 6, 0, 6, 0, },
                new object[] { "di", 62, 1, 69, 13, },
                new object[] { "robotics", 1, 0, 1, 1, },
                new object[] { "plastic", 15, 0, 32, 29, },
                new object[] { "pump", 6, 0, 7, 3,},
                new object[] { "abcdefg", 0, 0, 0, 0, },
                new object[] { "Interface", 24, 0, 24, 0, },
                new object[] { "Event", 23, 0, 23, 0, },
/*
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
*/
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
