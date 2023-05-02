using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CESMII.ProfileDesigner.Api.Controllers;
using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.DAL.Models;
using Microsoft.Extensions.DependencyInjection;
using NodeSetDiff;
using Opc.Ua;
using Opc.Ua.Export;
using Org.XmlUnit.Diff;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: TestCollectionOrderer("CESMII.ProfileDesigner.Api.Tests.CollectionOrderer", "CESMII.ProfileDesigner.Api.Tests")]

namespace CESMII.ProfileDesigner.Api.Tests
{
    [TestCaseOrderer("CESMII.ProfileDesigner.Api.Tests.ImportExportIntegrationTestCaseOrderer", "CESMII.ProfileDesigner.Api.Tests")]
    public partial class Integration
        : IClassFixture<CustomWebApplicationFactory<CESMII.ProfileDesigner.Api.Startup>>
    {
        #region API constants
        private const string URL_IMPORT_GETBYID = "/api/importlog/getbyid";
        private const string URL_IMPORT_START = "/api/importlog/init";
        private const string URL_IMPORT_UPLOAD = "/api/importlog/uploadfiles";
        //Note - there is an admin and non-admin flavor of the process call. In this case, the tests
        //are ordered so that dependency issues should not be encountered. Thus, going with the non-admin
        //flavor of this endpoint
        private const string URL_IMPORT_PROCESS = "/api/importlog/processfiles";
        #endregion

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
        public Task Export(string file)
        {
            return ExportInternal(file, false);
        }
        [Theory]
        [ClassData(typeof(AASXTestNodeSetFiles))]
        public Task ExportAASX(string file)
        {
            return ExportInternal(file, true);
        }
        internal async Task ExportInternal(string file, bool exportAASX)
        {
            file = Path.Combine(strTestNodeSetDirectory, file);
            output.WriteLine($"Testing {file}");
            // Arrange
            var apiClient = _factory.GetApiClientAuthenticated();

            var sw = Stopwatch.StartNew();

            // ACT
            await ExportNodeSets(apiClient, new string[] { file }, exportAASX);

            var exportTime = sw.Elapsed;
            output.WriteLine($"Export time: {exportTime}");

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

                OpcNodeSetXmlUnit.GenerateDiffSummary(d, out string diffControl, out string diffTest, out string diffSummary);

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
                    : $"Diff line {i - unexplainedLines} has no explanation in {expectedDiffFile}.";
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

        private List<ImportOPCModel> CheckAndDeleteExistingProfiles(bool deleteProfiles, MyNamespace.Client apiClient, string[] nodeSetFiles)
        {
            var profilesResultBefore = apiClient.LibraryAsync(new MyNamespace.PagerFilterSimpleModel { Query = "", Skip = 0, Take = 999 }).Result;
            var nodeSetFilesBefore = profilesResultBefore.Data.Select(p => GetFileNameFromNamespace(p.Namespace, p.Version, p.PublishDate)).ToList();

            var importRequest = new List<ImportOPCModel>();

            var profilesToDelete = new List<MyNamespace.ProfileModel>();

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
                        var profile = profilesResultBefore.Data.FirstOrDefault(p => GetFileNameFromNamespace(p.Namespace, p.Version, p.PublishDate) == fileName);
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

        private async Task ImportNodeSets(MyNamespace.Client apiClient, List<ImportOPCModel> importRequest)
        {
            if (importRequest.Any())
            {
                var orderedImportRequest = importRequest?.Count == 1 ? importRequest : OrderImportsByDependencies(importRequest);

                do
                {
                    var nextBatch = orderedImportRequest.Take(1).ToList();
                    int? importId = null;
                    try
                    {
                        var importFiles = PrepareMultiStepImportData(nextBatch);
                        importId = await ImportChunkedFiles(apiClient, importFiles[0].FileName, importFiles);
                        //result = await apiClient.ImportAsync(nextBatch);
                        //Assert.True(result.IsSuccess, $"Failed to import nodesets: {result.Message}");
                    }
                    catch (Exception ex)
                    {
                        Assert.True(false, $"Failed to import nodesets: {ex.Message}");
                    }
                    finally
                    { 
                    }

                    int timeLimit = 15;
                    ImportLogModel importLogItem;
                    var model = new IdIntModel { ID = importId.Value };
                    var sw = Stopwatch.StartNew();
                    do
                    {
                        System.Threading.Thread.Sleep(2000);
                        importLogItem = await apiClient.ApiGetItemAsync<ImportLogModel>(URL_IMPORT_GETBYID, model);
                        //importLogItem = await apiClient.GetByIDAsync(statusModel);
                    } while (sw.Elapsed < TimeSpan.FromMinutes(timeLimit) &&
                             ((int)importLogItem.Status == (int)TaskStatusEnum.InProgress
                             || (int)importLogItem.Status == (int)TaskStatusEnum.NotStarted));
                    
                    if ((int?)(importLogItem?.Status) != (int)TaskStatusEnum.Completed)
                    {
                        var errorText = $"Error importing nodeset {nextBatch.FirstOrDefault().FileName}: {importLogItem.Messages.FirstOrDefault().Message}";
                        output.WriteLine(errorText);
                        //show a clear message that the issue was we ran out of time rather than a failure
                        if (sw.Elapsed >= TimeSpan.FromMinutes(timeLimit))
                        {
                            output.WriteLine($"PollImportStatus||Time limit of {timeLimit} minutes exceeded");
                        }
                        Assert.True(false, errorText);
                    }
                    Assert.True((int?)(importLogItem?.Status) == (int)TaskStatusEnum.Completed);
                    //Assert.True(!status?.Messages?.Any());
                    orderedImportRequest.RemoveRange(0, nextBatch.Count);
                } while (orderedImportRequest.Any());
            }
        }

        private List<ImportFileModel> PrepareMultiStepImportData(List<ImportOPCModel> nextBatch)
        {
            //convert from old format into new format 
            if (nextBatch.Count == 0) return new List<ImportFileModel>();

            //make file name the key value
            var fileList = new List<ImportFileModel>();

            foreach (var item in nextBatch)
            {
                //make file name the key value
                var content = File.ReadAllText(item.FileName);

                fileList.Add( new ImportFileModel()
                {
                    FileName = item.FileName,
                    Chunks = new List<ImportFileChunkModel>() { new ImportFileChunkModel() { ChunkOrder = 1, Contents = content } },
                    TotalBytes = content.Length,
                    TotalChunks = 1
                });
            }

            return fileList;
        }

        /// <summary>
        /// This will perform all 3 import steps. This is shared by the large file import test as well as the 
        /// import/export integration tests.
        /// </summary>
        /// <param name="importFiles"></param>
        /// <returns></returns>
        protected async Task<int?> ImportChunkedFiles(MyNamespace.Client apiClient, string fileName, List<ImportFileModel> importFiles)
        {
            // ARRANGE

            //Note - files prepared and chunked in TestLargeNodeSetFiles class
            //capture info for comparison after the upload.
            //chunk size set below is 8mb
            var item = new ImportStartModel()
            {
                NotifyOnComplete = false,
                Items = importFiles.Select(fileInfo => new ImportFileModel()
                {
                    FileName = fileInfo.FileName,
                    TotalBytes = fileInfo.Chunks.Sum(x => x.Contents.Length),
                    TotalChunks = fileInfo.Chunks.Count
                }).ToList()
            };

            //ACT
            //Act 1 - call the first API endpoint to init the upload / import process
            var importItem = await apiClient.ApiGetItemAsync<ImportLogModel>(URL_IMPORT_START, item);
            //insert mock message to be used during cleanup
            //await InsertMockImportMessage(importItem.ID.Value);

            //ASSERT (Act 1)
            Assert.NotNull(importItem);
            Assert.NotNull(importItem.ID);
            foreach (var fileImport in importItem.Files)
            {
                var fileSource = importFiles.Find(f => f.FileName.ToLower().Equals(fileImport.FileName.ToLower()));
                Assert.NotNull(fileSource);
                Assert.Equal(fileSource.TotalBytes, fileImport.TotalBytes);
                Assert.Equal(fileSource.TotalChunks, fileImport.TotalChunks);
            }

            //Act 2 - call the multi-step upload process - uploading chunks
            var uploadChunkCalls = new List<Task<ResultMessageModel>>();  //using task.when all to get some parallel processing
            foreach (var fileImport in importItem.Files)
            {
                var fileSource = importFiles.Find(f => f.FileName.ToLower().Equals((object)fileImport.FileName.ToLower()));
                Assert.NotNull(fileSource);
                //loop over chunks and import
                foreach (var ch in fileSource.Chunks)
                {
                    var chunk = new ImportFileChunkProcessModel()
                    {
                        ImportActionId = importItem.ID.Value,
                        ImportFileId = fileImport.ID.Value,
                        FileName = fileSource.FileName,
                        ChunkOrder = ch.ChunkOrder,
                        //Contents = Encoding.Default.GetString(item.Contents)
                        Contents = ch.Contents
                    };
                    //item.ImportFileId = _guidCommon.ToString();
                    var msgTotalChunks = fileImport.TotalChunks == 1 ? "" : $", Chunk {ch.ChunkOrder} of {fileImport.TotalChunks}";
                    var chunkSize = Math.Round((ch.Contents.Length / Convert.ToDecimal(1024 * 1024)), 2);
                    var msgSize = $"{chunkSize} mb";
                    output.WriteLine($"Testing ImportChunkedFile: {fileSource.FileName} {msgTotalChunks}, Chunk Size: {msgSize}");
                    //add calls to collection of upload tasks so we can use .whenAll
                    uploadChunkCalls.Add(apiClient.ApiExecuteAsync<ResultMessageModel>(URL_IMPORT_UPLOAD, chunk));
                }
            }

            //now execute tasks and handle the outcomes and then proceed once all are completed
            await Task.WhenAll(uploadChunkCalls);
            foreach (var uploadChunkTask in uploadChunkCalls)
            {
                ResultMessageModel resultChunk = await uploadChunkTask;

                //ASSERT - act 2
                if (!resultChunk.IsSuccess) output.WriteLine(resultChunk.Message);
                Assert.True(resultChunk.IsSuccess);
            }

            //Act 3 - call the upload complete process which kicks off the import process
            var model = new IdIntModel() { ID = (int)importItem.ID.Value };
            var resultFinal = await apiClient.ApiExecuteAsync<ResultMessageWithDataModel>(URL_IMPORT_PROCESS, model);

            //ASSERT - import processing step
            if (!resultFinal.IsSuccess) output.WriteLine(resultFinal.Message);
            Assert.True(resultFinal.IsSuccess);

            //return the import id for use downstream
            return importItem.ID;
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
            }).OrderByDescending(imr => imr.importRequest.FileName).ToList();

            var orderedImports = new List<(ImportOPCModel, string, List<string>)>();
            var standalone = importsAndModels.Where(imr => !imr.requiredModels.Any()).ToList();
            orderedImports.AddRange(standalone);
            foreach (var imr in standalone)
            {
                importsAndModels.Remove(imr);
            }

            bool modelAdded;
            do
            {
                modelAdded = false;
                for (int i = 0; i < importsAndModels.Count;)
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
                    else
                    {
                        i++;
                    }
                }
            } while (importsAndModels.Count > 0 && modelAdded);

            orderedImports.AddRange(importsAndModels); // Add any remaining models (dependencies not satisfied)
            var orderedImportRequest = orderedImports.Select(irm => irm.Item1).ToList();
            return orderedImportRequest;
        }

        private static async Task ExportNodeSets(MyNamespace.Client apiClient, string[] nodeSetFiles, bool exportAASX)
        {
            var nodeSetResult = apiClient.LibraryAsync(new MyNamespace.PagerFilterSimpleModel { Query = "", Skip = 0, Take = 999 }).Result;
            foreach (var nodeSetFile in nodeSetFiles)
            {
                bool bExportFound = false;
                foreach (var profile in nodeSetResult.Data)
                {
                    var nodeSetFileName = GetFileNameFromNamespace(profile.Namespace, profile.Version, profile.PublishDate);

                    if (string.Equals(Path.GetFileName(nodeSetFile), nodeSetFileName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var exportResult = await apiClient.ExportAsync(new MyNamespace.ExportRequestModel { Id = profile.Id ?? 0, ForceReexport = true, Format = exportAASX ? "AASX" : null });
                        Assert.True(exportResult.IsSuccess, $"Failed to export {profile.Namespace}: {exportResult.Message}");

                        string exportedNodeSet;
                        if (exportAASX)
                        {
                            using (var aasxPackageStream = new MemoryStream(Convert.FromBase64String(exportResult.Data.ToString())))
                            {
                                using (Package package = Package.Open(aasxPackageStream, FileMode.Open))
                                {
                                    var partName = GetFileNameFromNamespace(profile.Namespace, null, null).Replace(".NodeSet2.xml", "");
                                    var partUri = new Uri("/aasx/" + partName, UriKind.Relative);
                                    var xmlPart = package.GetPart(partUri);
                                    using (var xmlStream = xmlPart.GetStream())
                                    {
                                        var xmlBytes = new byte[xmlStream.Length];
                                        xmlStream.Read(xmlBytes, 0, xmlBytes.Length);
                                        exportedNodeSet = Encoding.UTF8.GetString(xmlBytes);
                                    }
                                }
                            }
                        }
                        else
                        {
                            exportedNodeSet = exportResult.Data.ToString();
                        }
                        var nodeSetPath = Path.Combine(strTestNodeSetDirectory, "Exported", nodeSetFileName);
                        if (!Directory.Exists(Path.GetDirectoryName(nodeSetPath)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(nodeSetPath));
                        }
                        File.WriteAllText(nodeSetPath, exportedNodeSet);
                        bExportFound = true;
                    }
                }
                Assert.True(bExportFound, $"Export for {nodeSetFile} not found.");
            }
        }


        static private string GetFileNameFromNamespace(string namespaceUri, string version, DateTime? publicationDate)
        {
            var fileName = namespaceUri.Replace("http://", "").Replace("/", ".");
            if (!fileName.EndsWith("."))
            {
                fileName += ".";
            }
            //var legacyFileName = fileName + "NodeSet2.xml";
            if (namespaceUri == Namespaces.OpcUa && version != null)
            {
                // special versioning rules: must consider version family
                var versionParts = version.Split(".");
                if (versionParts.Length >= 2)
                {
                    var versionFamily = $"{versionParts[0]}.{versionParts[1]}";
                    fileName = $"{fileName}{versionFamily}.";
                }
                else
                {
                    throw new Exception($"Unexpeced version number for OPC core nodeset: must have at least a two-part version number");
                }
            }
            //var legacyFileName = fileName;
            if (publicationDate != null && publicationDate.Value != default)
            {
                //legacyFileName = $"{fileName}{publicationDate:yyyyMMdd}.";
                fileName = $"{fileName}{publicationDate:yyyy-MM-dd}.";
            }
            fileName += "NodeSet2.xml";
            //legacyFileName += "NodeSet2.xml";
            //if (fileName != legacyFileName)
            //{
            //    var legacyPath = Path.Combine(strTestNodeSetDirectory, legacyFileName);
            //    var path = Path.Combine(strTestNodeSetDirectory, fileName);
            //    if (File.Exists(legacyPath) && !File.Exists(path))
            //    {
            //        File.Move(legacyPath, path);
            //    }
            //    var legacyPath2 = Path.Combine(strTestNodeSetDirectory, "ExpectedDiffs", legacyFileName + ".summarydiff.difflog");
            //    var path2 = Path.Combine(strTestNodeSetDirectory, "ExpectedDiffs", fileName + ".summarydiff.difflog");
            //    if (File.Exists(legacyPath2) && !File.Exists(path2))
            //    {
            //        File.Move(legacyPath2, path2);
            //    }
            //}
            return fileName;
        }

    }

    internal class TestNodeSetFiles : IEnumerable<object[]>
    {
        internal static string[] GetFiles()
        {
            var nodeSetFiles = Directory.GetFiles(Integration.strTestNodeSetDirectory);

            var importRequest = new List<ImportOPCModel>();
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
    internal class AASXTestNodeSetFiles : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            Integration._ImportExportPending = true;
            var files = TestNodeSetFiles.GetFiles().Skip(3).Take(3);
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
                    if (!IsImportExportTest(t))
                    {
                        return true;
                    }
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

            var remainingOrdered = orderedImportRequests.Select(ir => remainingTestCaseList.FirstOrDefault(tc => IsImportExportTest(tc) && Path.Combine(Integration.strTestNodeSetDirectory, tc.TestMethodArguments[0].ToString()) == ir.FileName)).Where(tc => tc != null).ToList();
            var excludedTestCases = new List<TTestCase>();
            string[] unstableTests = new string[0];

            var unstableFileName = Path.Combine(Integration.strTestNodeSetDirectory, "ExpectedDiffs", "unstable.txt");
            if (File.Exists(unstableFileName))
            {
                File.ReadAllLines(Path.Combine(Integration.strTestNodeSetDirectory, "ExpectedDiffs", "unstable.txt"));
            }
            foreach (var remaining in remainingOrdered)
            {
                var file = remaining.TestMethodArguments[0].ToString();
                if (unstableTests.Contains(file))
                {
                    Console.WriteLine($"Not testing export for {file} because it is listed as unstable / pending investigation.");
                    excludedTestCases.Add(remaining);
                    continue;
                }
                var index = orderedTestCases.FindIndex(tc => tc.TestMethodArguments[0].ToString() == file);
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
            var remainingUnorderedTests = testCasesWithExpectedDiff.Except(orderedTestCases).Except(excludedTestCases).ToList();

            return orderedTestCases.Concat(remainingUnorderedTests).ToList();
        }

        static bool IsImportExportTest(ITestCase t)
        {
            return new[] { nameof(Integration.Import), nameof(Integration.Export), nameof(Integration.ExportAASX) }.Contains(t.TestMethod.Method.Name);
        }
    }

    public class CollectionOrderer : ITestCollectionOrderer
    {
        public CollectionOrderer(object ignored)
        {
        }

        public IEnumerable<ITestCollection> OrderTestCollections(
            IEnumerable<ITestCollection> testCollections) =>
            testCollections
                .OrderByDescending(collection => collection.DisplayName.Contains("Integration"))
                .ThenBy(collection => collection.DisplayName)
            ;
    }


}
