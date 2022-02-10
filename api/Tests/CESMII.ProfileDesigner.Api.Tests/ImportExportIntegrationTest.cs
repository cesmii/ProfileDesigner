using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CESMII.ProfileDesigner.Api.Controllers;
using Microsoft.Extensions.DependencyInjection;
using MyNamespace;
using NodeSetDiff;
using Org.XmlUnit.Diff;
using Xunit;
using Xunit.Abstractions;

namespace CESMII.ProfileDesigner.Api.Tests.IntegrationTests
{
    public class IntegrationTests
        : IClassFixture<CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup>>
    {
        private readonly CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup> _factory;
        private readonly ITestOutputHelper output;

        public IntegrationTests(CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup> factory, ITestOutputHelper output)
        {
            _factory = factory;
            this.output = output;
        }

        public static bool _ImportExportPending = true;

        [Theory]
        [ClassData(typeof(TestNodeSetFiles))]
        public async Task ImportExportIntegrationTest(string file)
        {
            if (_ImportExportPending)
            {
                // TODO This should be in an IClassFixture, but there seems to be no way to use the TestHost from a class fixture
                _ImportExportPending = false;
                // Arrange
                var apiClient = _factory.GetApiClientAuthenticated();

                var nodeSetFiles = TestNodeSetFiles.GetFiles();
                Assert.NotEmpty(nodeSetFiles);

                // ACT
                List<ImportOPCModel> importRequest = CheckAndDeleteExistingProfiles(
                    true, // Set to false to only test export during development
                    apiClient, nodeSetFiles);

                await ImportNodeSets(apiClient, importRequest);
                await ExportNodeSets(apiClient, nodeSetFiles);
            }
            Assert.False(_ImportExportPending);

            // Assert
            {
                Diff d = OpcNodeSetXmlUnit.DiffNodeSetFiles(file, file.Replace("TestNodeSets", @"TestNodeSets\Exported"));

                string diffControl, diffTest, diffSummary;
                OpcNodeSetXmlUnit.GenerateDiffSummary(d, out diffControl, out diffTest, out diffSummary);

                var diffFileRoot = file.Replace("TestNodeSets", @"TestNodeSets\Diffs");

                if (!Directory.Exists(Path.GetDirectoryName(diffFileRoot)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(diffFileRoot));
                }

                var summaryDiffFile = $"{diffFileRoot}.summarydiff.log";
                File.WriteAllText(summaryDiffFile, diffSummary);
                File.WriteAllText($"{diffFileRoot}.controldiff.log", diffControl);
                File.WriteAllText($"{diffFileRoot}.testdiff.log", diffTest);

                var expectedDiffFile = file.Replace("TestNodeSets", @"TestNodeSets\ExpectedDiffs") + ".summarydiff.log";
                var expectedSummary = File.ReadAllText(expectedDiffFile);

                var expectedSummaryLines = File.ReadAllLines(expectedDiffFile);
                int i = 0;
                var issueCounts = new Dictionary<string, int>();
                while (i < expectedSummaryLines.Length)
                {
                    var line = expectedSummaryLines[i];
                    if (line.StartsWith("###"))
                    {
                        var parts = line.Substring("###".Length).Split("#", 4);
                        int count = 1;
                        if (parts.Length > 1)
                        {
                            count = int.Parse(parts[0]);
                            bool bIsTriaged = false;
                            if (parts.Length > 2)
                            {
                                issueCounts.TryGetValue(parts[1], out var previousCount);
                                issueCounts[parts[1]] =  previousCount + count;
                                if (parts[1].ToLowerInvariant() == "by design")
                                {
                                    bIsTriaged = true;
                                }
                                else
                                {
                                    if (parts.Length > 3)
                                    {
                                        var issueNumber =parts[3];
                                        if (!string.IsNullOrEmpty(issueNumber))
                                        {
                                            bIsTriaged = true;
                                        }
                                    }
                                }
                            }
                            if (!bIsTriaged)
                            {
                                output.WriteLine($"Not triaged: {expectedDiffFile}, line {i} {line}");
                                issueCounts.TryGetValue("Untriaged", out var previousCount);
                                issueCounts["Untriaged"] = previousCount + count;
                            }
                        }
                        i += count;
                    }
                    else
                    {
                        if (line.Length >= 2)
                        {
                            var message = $"Diff line {i} in {expectedDiffFile} has no explanation.";
                            output.WriteLine(message);
                            //Assert.True(false, message);
                        }
                    }
                    i++;
                }
                var diffCounts = issueCounts.Any() ? string.Join(", ", issueCounts.Select(kv => $"{kv.Key}: {kv.Value}")) : "none";

                expectedSummary = Regex.Replace(expectedSummary, "^###.*$", "", RegexOptions.Multiline);
                // Ignore CR/LF difference in the diff files (often git induced) 
                expectedSummary = expectedSummary.Replace("\r", "").Replace("\n", "");
                diffSummary = diffSummary.Replace("\r", "").Replace("\n", "");
                Assert.True(expectedSummary == diffSummary, $"Diffs in {Path.GetFullPath(summaryDiffFile)} are not as expected in {Path.GetFullPath(expectedDiffFile)}");
                output.WriteLine($"Verified export {file}. Diffs: {diffCounts}");
            }

        }


        private List<ImportOPCModel> CheckAndDeleteExistingProfiles(bool deleteProfiles, Client apiClient, string[] nodeSetFiles)
        {
            var profilesResultBefore = apiClient.LibraryAsync(new PagerFilterSimpleModel { Query = "", Skip = 0, Take = 999 }).Result;
            var nodeSetFilesBefore = profilesResultBefore.Data.Select(p => GetFileNameFromNamespace(p.Namespace)).ToList();

            var importRequest = new List<MyNamespace.ImportOPCModel>();

            var profilesToDelete = new List<ProfileModel>();

            foreach (var file in nodeSetFiles)
            {
                var fileName = Path.GetFileName(file);
                if (!nodeSetFilesBefore.Contains(fileName))
                {
                    importRequest.Add(new ImportOPCModel { FileName = file, Data = File.ReadAllText(file), });
                }
                else
                {
                    output.WriteLine($"NodeSet {file} was already imported.");
                    if (deleteProfiles)
                    {
                        var profile = profilesResultBefore.Data.FirstOrDefault(p => GetFileNameFromNamespace(p.Namespace) == fileName);
                        if (profile != null)
                        {
                            profilesToDelete.Add(profile);
                            importRequest.Add(new ImportOPCModel { FileName = file, Data = File.ReadAllText(file), });
                        }
                    }
                }
            }
            if (profilesToDelete.Any())
            {
                using (var scope = _factory.Server.Services.CreateScope())
                {
                    var profileController = scope.ServiceProvider.GetRequiredService<ProfileController>();
                    bool bDeleted;
                    do
                    {
                        bDeleted = false;
                        foreach (var profile in profilesToDelete)
                        {
                            try
                            {
                                profileController.DeleteInternalTestHook(new Shared.Models.IdIntModel { ID = profile.Id.Value });
                                bDeleted = true;
                                profilesToDelete.Remove(profile);
                                //var deleteResult = await apiClient.Delete2Async(new IdIntModel { Id = profile.Id.Value });
                                output.WriteLine($"Deleted existing profile {profile.Id} - {profile.Namespace}");
                                break;

                            }
                            catch (Exception)
                            {
                                // Assume the exception is caused by another profile referencing the profile to be deleted
                                // Instead of trying to order the profiles just retry until no more profiles can be deleted
                            }
                        }
                    } while (bDeleted); // Keep trying until we can't delete any more profiles
                    Assert.True(!profilesToDelete.Any(), $"Unable to delete {string.Join(", ", profilesToDelete.Select(p => p.Namespace + " (Id: " + p.Id + ")"))}");
                }

            }

            return importRequest;
        }

        private static async Task ImportNodeSets(Client apiClient, List<ImportOPCModel> importRequest)
        {
            ImportLogModel status = null;
            if (importRequest.Any())
            {
                var result = await apiClient.ImportAsync(importRequest);
                Assert.True(result.IsSuccess, $"Failed to import nodesets: {result.Message}");

                var statusModel = new MyNamespace.IdIntModel { Id = Convert.ToInt32(result.Data), };
                var sw = Stopwatch.StartNew();
                do
                {
                    System.Threading.Thread.Sleep(5000);
                    status = await apiClient.GetByIDAsync(statusModel);
                } while (sw.Elapsed < TimeSpan.FromMinutes(5) &&
                         ((int)status.Status == (int)CESMII.ProfileDesigner.Common.Enums.TaskStatusEnum.InProgress
                         || (int)status.Status == (int)CESMII.ProfileDesigner.Common.Enums.TaskStatusEnum.NotStarted));
                Assert.True((int?)(status?.Status) == (int)CESMII.ProfileDesigner.Common.Enums.TaskStatusEnum.Completed);
            }
        }

        private static async Task ExportNodeSets(Client apiClient, string[] nodeSetFiles)
        {
            var nodeSetResult = apiClient.LibraryAsync(new PagerFilterSimpleModel { Query = "", Skip = 0, Take = 999 }).Result;
            foreach (var profile in nodeSetResult.Data)
            {
                var nodeSetFileName = GetFileNameFromNamespace(profile.Namespace);

                if (nodeSetFiles.Where(f => Path.GetFileName(f) == nodeSetFileName).Any())
                {
                    var exportResult = await apiClient.ExportAsync(new IdIntModel { Id = profile.Id ?? 0 });
                    Assert.True(exportResult.IsSuccess, $"Failed to export {profile.Namespace}");

                    var exportedNodeSet = exportResult.Data.ToString();
                    var nodeSetPath = Path.Combine("TestNodeSets", "Exported", nodeSetFileName);
                    if (!Directory.Exists(Path.GetDirectoryName(nodeSetPath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(nodeSetPath));
                    }
                    File.WriteAllText(nodeSetPath, exportedNodeSet);
                }
            }
        }


        static private string GetFileNameFromNamespace(string namespaceUri)
        {
            return namespaceUri.Replace("http://", "").Replace("/", ".") + "NodeSet2.xml";
        }

    }

    internal class TestNodeSetFiles : IEnumerable<object[]>
    {
        internal static string[] GetFiles()
        {
            var nodeSetFiles = Directory.GetFiles("TestNodeSets");
            return nodeSetFiles;
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            IntegrationTests._ImportExportPending = true;
            var files = GetFiles();
            return files.Select(f => new object[] { f }).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
