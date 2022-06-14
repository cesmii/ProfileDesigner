using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CESMII.ProfileDesigner.Api.Controllers;
using Microsoft.Extensions.DependencyInjection;
using MyNamespace;
using NodeSetDiff;
using Opc.Ua.Export;
using Org.XmlUnit.Diff;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: CollectionBehavior(DisableTestParallelization = true) ]

namespace CESMII.ProfileDesigner.Api.Tests
{
    [TestCaseOrderer("CESMII.ProfileDesigner.Api.Tests.ImportExportIntegrationTestCaseOrderer", "CESMII.ProfileDesigner.Api.Tests")]
    public class Integration
        : IClassFixture<CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup>>
    {
        private readonly CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup> _factory;
        private readonly ITestOutputHelper output;

        public Integration(CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup> factory, ITestOutputHelper output)
        {
            _factory = factory;
            this.output = output;
        }

        public static bool _ImportExportPending = true;

        public const string strTestNodeSetDirectory = "TestNodeSets";

        [Theory]
        [ClassData(typeof(TestNodeSetFiles))]

        public async Task Import(string file)
        {
            file = Path.Combine(strTestNodeSetDirectory, file);
            output.WriteLine($"Testing {file}");
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            var importRequest = new List<ImportOPCModel> { new ImportOPCModel { FileName = file, Data = File.ReadAllText(file) } };
            await ImportNodeSets(apiClient, importRequest);
        }

        [Theory]
        [ClassData(typeof(TestNodeSetFiles))]
        public async Task Export(string file)
        {
            file = Path.Combine(strTestNodeSetDirectory, file);
            output.WriteLine($"Testing {file}");
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            // ACT
            await ExportNodeSets(apiClient, new string[] { file });

            //if (_ImportExportPending)
            //{
            //    // TODO This should be in an IClassFixture, but there seems to be no way to use the TestHost from a class fixture
            //    _ImportExportPending = false;
            //    // Arrange
            //    var apiClient = _factory.GetApiClientAuthenticated();

            //    var nodeSetFiles = TestNodeSetFiles.GetFiles();
            //    Assert.NotEmpty(nodeSetFiles);

            //    // ACT
            //    List<ImportOPCModel> importRequest = CheckAndDeleteExistingProfiles(
            //        false, // Set to false to only test export during development
            //        apiClient, nodeSetFiles);

            //    await ImportNodeSets(apiClient, importRequest);
            //    await ExportNodeSets(apiClient, nodeSetFiles);
            //}
            //Assert.False(_ImportExportPending);

            // Assert
            {
                Diff d = OpcNodeSetXmlUnit.DiffNodeSetFiles(file, file.Replace(strTestNodeSetDirectory, Path.Combine(strTestNodeSetDirectory, "Exported")));

                string diffControl, diffTest, diffSummary;
                OpcNodeSetXmlUnit.GenerateDiffSummary(d, out diffControl, out diffTest, out diffSummary);

                var diffFileRoot = file.Replace(strTestNodeSetDirectory, Path.Combine(strTestNodeSetDirectory, "Diffs"));

                if (!Directory.Exists(Path.GetDirectoryName(diffFileRoot)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(diffFileRoot));
                }

                var summaryDiffFile = $"{diffFileRoot}.summarydiff.difflog";
                File.WriteAllText(summaryDiffFile, diffSummary);
                File.WriteAllText($"{diffFileRoot}.controldiff.difflog", diffControl);
                File.WriteAllText($"{diffFileRoot}.testdiff.difflog", diffTest);

                string expectedDiffFile = GetExpectedDiffFile(file);
                if (!File.Exists(expectedDiffFile))
                {
                    File.WriteAllText(expectedDiffFile, diffSummary);
                }

                var expectedSummary = File.ReadAllText(expectedDiffFile);

                var expectedSummaryLines = File.ReadAllLines(expectedDiffFile);
                int i = 0;
                var issueCounts = new Dictionary<string, int>();
                int unexplainedLines = 0;
                while (i < expectedSummaryLines.Length)
                {
                    var line = expectedSummaryLines[i];
                    if (line.StartsWith("###"))
                    {
                        unexplainedLines = ReportUnexplainedLines(expectedDiffFile, i, issueCounts, unexplainedLines);
                        var parts = line.Substring("###".Length).Split("#", 4);
                        int count = 1;
                        if (parts.Length > 1)
                        {
                            count = int.Parse(parts[0]);
                            bool bIsTriaged = false;
                            if (parts.Length > 2)
                            {
                                issueCounts.TryGetValue(parts[1], out var previousCount);
                                issueCounts[parts[1]] = previousCount + count;
                                if (parts[1].ToLowerInvariant() == "by design")
                                {
                                    bIsTriaged = true;
                                }
                                else
                                {
                                    if (parts.Length > 3)
                                    {
                                        var issueNumber = parts[3];
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
                        unexplainedLines++;
                    }
                    i++;
                }
                unexplainedLines = ReportUnexplainedLines(expectedDiffFile, i, issueCounts, unexplainedLines);

                var diffCounts = issueCounts.Any() ? string.Join(", ", issueCounts.Select(kv => $"{kv.Key}: {kv.Value}")) : "none";

                expectedSummary = Regex.Replace(expectedSummary, "^###.*$", "", RegexOptions.Multiline);
                // Ignore CR/LF difference in the diff files (often git induced) 
                expectedSummary = expectedSummary.Replace("\r", "").Replace("\n", "");
                diffSummary = diffSummary.Replace("\r", "").Replace("\n", "");
                Assert.True(expectedSummary == diffSummary, $"Diffs not as expected {Path.GetFullPath(summaryDiffFile)} expected {Path.GetFullPath(expectedDiffFile)}");
                output.WriteLine($"Verified export {file}. Diffs: {diffCounts}");
                if (issueCounts.TryGetValue("Untriaged", out var untriagedIssues) && untriagedIssues > 0)
                {
                    var message = $"Failed due to {untriagedIssues} untriaged issues: {diffCounts}";
                    output.WriteLine(message);
                    //Ignore for now: ideally would make as warning/yellow, but XUnit doesn't seem to allow that
                    //Assert.True(0 == untriagedIssues, message);
                }
            }

        }

        private int ReportUnexplainedLines(string expectedDiffFile, int i, Dictionary<string, int> issueCounts, int unexplainedLines)
        {
            if (unexplainedLines > 0)
            {
                var message = unexplainedLines > 1 ?
                    $"Diff lines {i - unexplainedLines + 1} to {i} have no explanation in {expectedDiffFile}."
                    : $"Diff lines {i - unexplainedLines} has no explanation in {expectedDiffFile}.";
                output.WriteLine(message);
                issueCounts.TryGetValue("Untriaged", out var previousCount);
                issueCounts["Untriaged"] = previousCount + unexplainedLines;
                //Assert.True(false, message);
                unexplainedLines = 0;
            }

            return unexplainedLines;
        }

        internal static string GetExpectedDiffFile(string file)
        {
            return file.Replace(strTestNodeSetDirectory, Path.Combine(strTestNodeSetDirectory, "ExpectedDiffs")) + ".summarydiff.difflog";
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

        private async Task ImportNodeSets(Client apiClient, List<ImportOPCModel> importRequest)
        {
            ImportLogModel status = null;
            if (importRequest.Any())
            {
                var orderedImportRequest = importRequest?.Count == 1 ? importRequest : OrderImportsByDependencies(importRequest);

                do
                {
                    var nextBatch = orderedImportRequest.Take(1).ToList();
                    ResultMessageWithDataModel result = null;
                    try
                    {
                        result = await apiClient.ImportAsync(nextBatch);
                        Assert.True(result.IsSuccess, $"Failed to import nodesets: {result.Message}");
                    }
                    catch (Exception ex)
                    {
                        Assert.True(false, $"Failed to import nodesets: {ex.Message}");
                    }

                    var statusModel = new MyNamespace.IdIntModel { Id = Convert.ToInt32(result.Data), };
                    var sw = Stopwatch.StartNew();
                    do
                    {
                        System.Threading.Thread.Sleep(2000);
                        status = await apiClient.GetByIDAsync(statusModel);
                    } while (sw.Elapsed < TimeSpan.FromMinutes(15) &&
                             ((int)status.Status == (int)CESMII.ProfileDesigner.Common.Enums.TaskStatusEnum.InProgress
                             || (int)status.Status == (int)CESMII.ProfileDesigner.Common.Enums.TaskStatusEnum.NotStarted));
                    if ((int?)(status?.Status) != (int)CESMII.ProfileDesigner.Common.Enums.TaskStatusEnum.Completed)
                    {
                        var errorText = $"Error importing nodeset {nextBatch.FirstOrDefault().FileName}: {status.Messages.FirstOrDefault().Message}";
                        output.WriteLine(errorText);
                        Assert.True(false, errorText);
                    }
                    Assert.True((int?)(status?.Status) == (int)CESMII.ProfileDesigner.Common.Enums.TaskStatusEnum.Completed);
                    //Assert.True(!status?.Messages?.Any());
                    orderedImportRequest.RemoveRange(0, nextBatch.Count);
                } while (orderedImportRequest.Any());
            }
        }

        public static List<ImportOPCModel> OrderImportsByDependencies(List<ImportOPCModel> importRequest)
        {
            var importsAndModels = importRequest.Select(importRequest =>
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(importRequest.Data.Replace("<Value/>", "<Value xsi:nil='true' />"))))
                {
                    var nodeSet = UANodeSet.Read(ms);
                    var modelUri = nodeSet.Models?[0].ModelUri;
                    var requiredModels = nodeSet.Models?.SelectMany(m => m.RequiredModel?.Select(rm => rm.ModelUri) ?? new List<string>())?.ToList();
                    return (importRequest, modelUri, requiredModels);
                }
            }).ToList();

            var orderedImports = new List<(ImportOPCModel, string, List<string>)>();
            var standalone = importsAndModels.Where(imr => imr.requiredModels.Any() != true).ToList();
            orderedImports.AddRange(standalone);
            foreach (var imr in standalone)
            {
                importsAndModels.Remove(imr);
            }

            bool modelAdded;
            do
            {
                modelAdded = false;
                for (int i = importsAndModels.Count - 1; i >= 0; i--)
                {
                    var imr = importsAndModels[i];
                    bool bDependenciesSatisfied = true;
                    foreach (var dependency in imr.requiredModels)
                    {
                        if (!orderedImports.Any(imr => imr.Item2 == dependency))
                        {
                            bDependenciesSatisfied = false;
                            continue;
                        }
                    }
                    if (bDependenciesSatisfied)
                    {
                        orderedImports.Add(imr);
                        importsAndModels.RemoveAt(i);
                        modelAdded = true;
                    }
                }
            } while (importsAndModels.Count > 0 && modelAdded);

            //Assert.True(modelAdded, $"{importsAndModels.Count} nodesets require models not in the list.");
            orderedImports.AddRange(importsAndModels);
            var orderedImportRequest = orderedImports.Select(irm => irm.Item1).ToList();
            return orderedImportRequest;
        }

        private static async Task ExportNodeSets(Client apiClient, string[] nodeSetFiles)
        {
            var nodeSetResult = apiClient.LibraryAsync(new PagerFilterSimpleModel { Query = "", Skip = 0, Take = 999 }).Result;
            foreach (var profile in nodeSetResult.Data)
            {
                var nodeSetFileName = GetFileNameFromNamespace(profile.Namespace);

                if (nodeSetFiles.Where(f => string.Equals(Path.GetFileName(f), nodeSetFileName, StringComparison.InvariantCultureIgnoreCase)).Any())
                {
                    var exportResult = await apiClient.ExportAsync(new IdIntModel { Id = profile.Id ?? 0 });
                    Assert.True(exportResult.IsSuccess, $"Failed to export {profile.Namespace}: {exportResult.Message}");

                    var exportedNodeSet = exportResult.Data.ToString();
                    var nodeSetPath = Path.Combine(strTestNodeSetDirectory, "Exported", nodeSetFileName);
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
            var fileName = namespaceUri.Replace("http://", "").Replace("/", ".");
            if (!fileName.EndsWith("."))
            {
                fileName += ".";
            }
            fileName += "NodeSet2.xml";
            return fileName;
        }

    }

    internal class TestNodeSetFiles : IEnumerable<object[]>
    {
        internal static string[] GetFiles()
        {
            var nodeSetFiles = Directory.GetFiles(Integration.strTestNodeSetDirectory);

            var importRequest = new List<MyNamespace.ImportOPCModel>();
            foreach (var file in nodeSetFiles)
            {
                importRequest.Add(new ImportOPCModel { FileName = Path.GetFileName(file), Data = File.ReadAllText(file), });
            }
            var orderedImportRequest = Integration.OrderImportsByDependencies(importRequest);
            var orderedNodeSetFiles = orderedImportRequest.Select(r => r.FileName).ToArray();

            return orderedNodeSetFiles;
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            Integration._ImportExportPending = true;
            var files = GetFiles();
            return files.Select(f => new object[] { f }).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class ImportExportIntegrationTestCaseOrderer : ITestCaseOrderer
    {
        public ImportExportIntegrationTestCaseOrderer()
        {
        }
        public ImportExportIntegrationTestCaseOrderer(object ignored)
        {

        }
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
        {
            bool ignoreTestsWithoutExpectedOutcome = true;
            var testCasesWithExpectedDiff = testCases.ToList();
            if (ignoreTestsWithoutExpectedOutcome)
            {
                testCasesWithExpectedDiff = testCases.Where(t =>
                {
                    var file = t.TestMethodArguments[0].ToString();
                    var diffFile = Integration.GetExpectedDiffFile(Path.Combine(Integration.strTestNodeSetDirectory, file));
                    var bHasDiff = File.Exists(diffFile);
                    if (!bHasDiff)
                    {
                        Console.WriteLine($"Ignoring {file} because it has no expected diff file {diffFile}.");
                    }
                    return bHasDiff;
                }).ToList();
            }
            var importTestCaseList = testCasesWithExpectedDiff.Where(t => t.TestMethod.Method.Name == nameof(Integration.Import)).ToList();
            var testFiles = importTestCaseList.Select(t => t.TestMethodArguments[0].ToString()).ToList();
            var importRequests = testFiles.Select(file =>
            {
                var filePath = Path.Combine(Integration.strTestNodeSetDirectory, file);
                return new ImportOPCModel { FileName = filePath, Data = File.ReadAllText(filePath), };
            }).ToList();
            var orderedImportRequests = Integration.OrderImportsByDependencies(importRequests);
            // Run import tests first, in dependency order
            var orderedTestCases = orderedImportRequests.Select(ir => importTestCaseList.FirstOrDefault(tc => Path.Combine(Integration.strTestNodeSetDirectory, tc.TestMethodArguments[0].ToString()) == ir.FileName)).ToList();

            var remainingTestCaseList = testCasesWithExpectedDiff.Except(orderedTestCases).ToList();

            var remainingOrdered = orderedImportRequests.Select(ir => remainingTestCaseList.FirstOrDefault(tc => Path.Combine(Integration.strTestNodeSetDirectory, tc.TestMethodArguments[0].ToString()) == ir.FileName)).Where(tc => tc != null).ToList();
            foreach (var remaining in remainingOrdered)
            {
                var index = orderedTestCases.FindIndex(tc => tc.TestMethodArguments[0].ToString() == remaining.TestMethodArguments[0].ToString());
                if (index >= 0)
                {
                    orderedTestCases.Insert(index + 1, remaining);
                }
                else
                {
                    orderedTestCases.Add(remaining);
                }
            }
            //orderedTestCases.AddRange(remainingOrdered);

            // Then all other tests
            var remainingUnorderedTests = testCasesWithExpectedDiff.Except(orderedTestCases).ToList();

            return orderedTestCases.Concat(remainingUnorderedTests).ToList();
        }
    }
}
