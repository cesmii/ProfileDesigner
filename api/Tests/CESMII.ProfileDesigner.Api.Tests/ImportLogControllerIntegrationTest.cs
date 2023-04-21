using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using Xunit;
using Xunit.Abstractions;

using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Repositories;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.Data.Contexts;
using CESMII.ProfileDesigner.Api.Shared.Models;
using Opc.Ua.Export;

namespace CESMII.ProfileDesigner.Api.Tests.Int
{
    public class ImportLogControllerIntegrationTest : ControllerTestBase
    {
        private readonly ServiceProvider _serviceProvider;
        //for some tests, tie together a common guid so we can delete the created items at end of test. 
        private Guid _guidCommon = Guid.NewGuid();
        //set inside large nodeset import test and used in cleanup
        private KeyValuePair<string, List<ImportFileModel>> _currentImportData = new KeyValuePair<string, List<ImportFileModel>>();

        #region API constants
        private const string URL_GETBYID = "/api/importlog/getbyid";
        private const string URL_MINE = "/api/importlog/mine";
        private const string URL_DELETE = "/api/importlog/delete";
        private const string URL_PROFILE_DELETE = "/api/profile/delete";

        private const string URL_IMPORT_START = "/api/importlog/init";
        private const string URL_IMPORT_UPLOAD = "/api/importlog/uploadfiles";
        //Note - call admin instance of this endpoint so that we can allow for multiple versions of core
        //nodesets (ua and ua/di). There is no difference in the processing other than allowing import of
        //dependent core nodesets of multiple versions.
        private const string URL_IMPORT_PROCESS = "/api/importlog/admin/processfiles";
        #endregion

        #region data naming constants
        #endregion

        public ImportLogControllerIntegrationTest(
            CustomWebApplicationFactory<Api.Startup> factory, 
            ITestOutputHelper output):
            base(factory, output)
        {
            var services = new ServiceCollection();

            //wire up db context to be used by repo
            base.InitDBContext(services);
            
            // DI - directly inject repo so we can add some test data directly and then have API test against it.
            // when running search tests. 
            services.AddSingleton< IConfiguration>(factory.Config);
            services.AddScoped<IRepository<ImportLog>, BaseRepo<ImportLog, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<Profile>, BaseRepo<Profile, ProfileDesignerPgContext>>();
            //need to get user id of test user
            services.AddScoped<IRepository<User>, BaseRepo<User, ProfileDesignerPgContext>>();
            
            _serviceProvider = services.BuildServiceProvider();
        }

#pragma warning disable xUnit1026  // Stop warnings related to parameters not used in test cases. 

        /* future test
        [Theory]
        [InlineData(NAMESPACE_CLOUD_PATTERN, 8, 8, 2)]
        [InlineData(NAMESPACE_PATTERN, 4, 6, 4)]
        [InlineData(CATEGORY_PATTERN, 0, 5, 5)]
        public async Task DeleteItem(string query, int expectedCount, int numItemsToAdd, int numCloudItemsToAdd)
        {
            // ARRANGE
            //get api client
            var apiClient = base.ApiClient;

            //add some test rows to search against
            await InsertMockEntitiesForSearchTests(numItemsToAdd, false);
            await InsertMockEntitiesForSearchTests(numCloudItemsToAdd, true);

            //get stock filter
            var filter = base.ProfileFilter;
            filter.Take = 1000;  //make sure we get all items
            filter.Query = query;

            //get a partial list of items to delete, convert to list of ids
            var matches = (await apiClient.ApiGetManyAsync<ImportLogModel>(URL_LIBRARY, filter)).Data;
            var model = matches.Select(y => new Shared.Models.IdIntModel() { ID = y.ID.Value }).ToList();

            // ACT
            //delete the item
            var result = await apiClient.ApiExecuteAsync<ResultMessageModel>(URL_DELETE, model);

            //ASSERT
            //TODO: check the parent record and all the child records are deleted. 
            Assert.True(result.IsSuccess);
            Assert.Contains("deleted", result.Message.ToLower());

            //Try to get the remaining items and should equal expected count,
            //always add the extra where clause after the fact of _guidCommon in case another test is adding stuff in parallel. 
            filter.Query = _guidCommon.ToString();
            var itemsRemaining = (await apiClient.ApiGetManyAsync<ProfileModel>(URL_LIBRARY, filter)).Data
                .Where(x => x.Keywords != null && string.Join(",", x.Keywords).ToLower().Contains(_guidCommon.ToString())).ToList();
            Assert.Equal(expectedCount, itemsRemaining.Count);
        }
        */

        [Theory]
        [ClassData(typeof(TestLargeNodeSetFiles))]
        public async Task ImportLargeFiles(KeyValuePair<string, List<ImportFileModel>> importData)
        {
            // ARRANGE
            //get admin version of api client which makes user an admin
            var apiClient = base.ApiClientAdmin;

            //clean up any previously existing import for this file.
            _currentImportData = importData;
            CleanupEntities(importData).Wait();

            await ImportChunkedFiles(apiClient, importData);
        }

        /// <summary>
        /// This will perform all 3 import steps. This is shared by the large file import test as well as the 
        /// import/export integration tests.
        /// </summary>
        /// <param name="importData"></param>
        /// <returns></returns>
        protected async Task ImportChunkedFiles(MyNamespace.Client apiClient, KeyValuePair<string, List<ImportFileModel>> importData)
        {
            // ARRANGE
            //Note - files prepared and chunked in TestLargeNodeSetFiles class
            //capture info for comparison after the upload.
            //chunk size set below is 8mb
            var itemsToImport = importData.Value.Select(fileInfo => new ImportFileModel()
            {
                FileName = fileInfo.FileName,
                TotalBytes = fileInfo.Chunks.Sum(x => x.Contents.Length),
                TotalChunks = fileInfo.Chunks.Count
            }).ToList();

            //ACT
            //Act 1 - call the first API endpoint to init the upload / import process
            var importItem = await apiClient.ApiGetItemAsync<ImportLogModel>(URL_IMPORT_START, itemsToImport);
            //insert mock message to be used during cleanup
            await InsertMockImportMessage(importItem.ID.Value);

            //ASSERT (Act 1)
            Assert.NotNull(importItem);
            Assert.NotNull(importItem.ID);
            foreach (var fileImport in importItem.Files)
            {
                var fileSource = importData.Value.Find(f => f.FileName.ToLower().Equals(fileImport.FileName.ToLower()));
                Assert.NotNull(fileSource);
                Assert.Equal(fileSource.TotalBytes, fileImport.TotalBytes);
                Assert.Equal(fileSource.TotalChunks, fileImport.TotalChunks);
            }

            //Act 2 - call the multi-step upload process - uploading chunks
            var uploadChunkCalls = new List<Task<ResultMessageModel>>();  //using task.when all to get some parallel processing
            foreach (var fileImport in importItem.Files)
            {
                var fileSource = importData.Value.Find(f => f.FileName.ToLower().Equals((object)fileImport.FileName.ToLower()));
                Assert.NotNull(fileSource);
                //loop over chunks and import
                foreach (var item in fileSource.Chunks)
                {
                    var chunk = new ImportFileChunkProcessModel()
                    {
                        ImportActionId = importItem.ID.Value,
                        ImportFileId = fileImport.ID.Value,
                        FileName = fileSource.FileName,
                        ChunkOrder = item.ChunkOrder,
                        //Contents = Encoding.UTF8.GetString(item.Contents),
                        Contents = item.Contents
                    };
                    //item.ImportFileId = _guidCommon.ToString();
                    var msgTotalChunks = fileImport.TotalChunks == 1 ? "" : $", Chunk {item.ChunkOrder} of {fileImport.TotalChunks}";
                    var msgSize = $"{Math.Round((double)(item.Contents.Length / (1024 * 1024)), 1)} mb";
                    output.WriteLine($"Testing ImportChunkedFile: {fileSource} {msgTotalChunks}, Chunk Size: {msgSize}");
                    //var resultChunk = await apiClient.ApiExecuteAsync<ResultMessageModel>(URL_IMPORT_UPLOAD, chunk);
                    //add calls to collection of upload tasks so we can use .whenAll
                    uploadChunkCalls.Add(apiClient.ApiExecuteAsync<ResultMessageModel>(URL_IMPORT_UPLOAD, chunk));

                    //ASSERT - act 2
                    //if (!resultChunk.IsSuccess) output.WriteLine(resultChunk.Message);
                    //Assert.True(resultChunk.IsSuccess);
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

            //compare the pre-import file matches the post import file - need specific knowledge of where the controller puts the file parts
            AssertCompareFiles(importData.Value);

            //Track import messages and poll for import completion.
            await PollImportStatus(apiClient, importItem.ID.Value);

        }

        #region Helper Methods
        /// <summary>
        /// Delete profiles created during each test
        /// User <_guidCommon> as way to find items to delete 
        /// </summary>
        /// <returns></returns>
        private async Task InsertMockImportMessage(int id)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                //add a message that we can use to tie back at the end and delete the data associated with this test run.
                var repo = scope.ServiceProvider.GetService<IRepository<ImportLog>>();
                var entity = repo.FindByCondition(x =>
                    x.ID.Equals(id)).FirstOrDefault();
                if (entity != null)
                {
                    entity.Messages.Add(new ImportLogMessage() { Message = $"Import-Test_{_guidCommon}", ImportLogId = id });
                    await repo.UpdateAsync(entity);
                    await repo.SaveChangesAsync(); 
                }
            }
        }


        private void AssertCompareFiles(List<ImportFileModel> sourceFiles)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                //find the import record using the guid common which was placed as a message in the import log.messages collection
                var repo = scope.ServiceProvider.GetService<IRepository<ImportLog>>();
                var entity = repo.FindByCondition(x => x.Messages.Any(y => y.Message.Contains(_guidCommon.ToString()))).FirstOrDefault();
                if (entity != null)
                {
                    //ASSERT - counts are equal
                    Assert.Equal(sourceFiles.Count, entity.Files.Count);

                    //ASSERT - all files made it
                    foreach (var f in sourceFiles)
                    {
                        //ASSERT - check this file made it
                        Assert.Contains(entity.Files, x => x.FileName.ToLower().Equals(f.FileName.ToLower()));

                        //ASSERT - check this file size is same
                        var fileDestination = entity.Files.Find(x => x.FileName.ToLower().Equals(f.FileName.ToLower()));
                        Assert.Equal(f.Chunks.Sum(x => x.Contents.Length), fileDestination.Chunks.Sum(x => x.Contents.Length));

                        //ASSERT - check this original complete file from disk can be reassembled from server version and is equal
                        //original is either in large files folder or possibly parent folder - check for this
                        var pathParent = System.IO.Path.Combine(Integration.strTestNodeSetDirectory, f.FileName);
                        var pathLargeFiles = System.IO.Path.Combine(Integration.strTestNodeSetDirectory, "LargeFiles", f.FileName);
                        var sourceFileName = System.IO.File.Exists(pathLargeFiles) ? pathLargeFiles : pathParent;
                        //AssertCompareFile(sourceFileName,MergeChunks(fileDestination));
                        //AssertCompareFileContent(sourceFileName, (new ASCIIEncoding()).GetBytes(MergeChunks(fileDestination)));
                        AssertCompareFileContent(sourceFileName, MergeChunks(fileDestination));
                    }
                }
            }
        }

        /*
        /// <summary>
        /// Compare two files and determine if they are equal using MD5
        /// </summary>
        /// <param name="sourceFileName"></param>
        /// <param name="uploadedFileName"></param>
        private void AssertCompareFile(string sourceFileName, byte[] destinationContents) //string uploadedFileName)
        {
            var hash = string.Empty;
            var hash2 = string.Empty;

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = System.IO.File.OpenRead(sourceFileName))
                {
                    hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
                //using (var stream = System.IO.File.OpenRead(uploadedFileName))
                //{
                //    hash2 = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                //}
                hash2 = BitConverter.ToString(md5.ComputeHash(destinationContents)).Replace("-", "").ToLower();
            }

            //compare the 2 files are same
            Assert.True(!string.IsNullOrEmpty(hash));
            if (hash.Equals(hash2)) output.WriteLine($"File comparison success: {sourceFileName}");
            Assert.True(hash.Equals(hash2));
        }
        */

        /// <summary>
        /// Compare two files and determine if they are equal using text
        /// </summary>
        private void AssertCompareFileContent(string sourceFileName, string destinationContents)
        {
            //compare the 2 files are same
            var sourceContents = System.IO.File.ReadAllText(sourceFileName);
            var isEqual = sourceContents.Equals(destinationContents);
            Assert.True(!string.IsNullOrEmpty(sourceContents));
            if (isEqual) output.WriteLine($"File comparison success: {sourceFileName}");
            Assert.True(isEqual);
        }

        private string MergeChunks(ImportFile file)
        {
            return string.Join("",
                file.Chunks
                .OrderBy(x => x.ChunkOrder)
                //.ToList()
                .Select(x => x.Contents));
        }

        /*
        private byte[] MergeChunks(ImportFile file)
        {
            //if one chunk, return it as is
            if (file.Chunks.Count == 1)
            {
                return file.Chunks[0].Contents;
            }

            //multiple files - merge into single byte array in proper order.
            file.Chunks = file.Chunks.OrderBy(x => x.ChunkOrder).ToList();
            byte[] result = null;
            foreach (var chunk in file.Chunks)
            {
                result = result == null ? chunk.Contents : result.Concat(chunk.Contents).ToArray();
            }
            return result;
        }
        */

        /// <summary>
        /// Delete profiles, import data created during each test
        /// User <_guidCommon> as way to find items to delete 
        /// </summary>
        /// <returns></returns>
        private async Task CleanupEntities(KeyValuePair<string, List<ImportFileModel>> importData)
        {
            if (importData.Key == null) return;

            using (var scope = _serviceProvider.CreateScope())
            {
                //Cleanup imported profiles - loop over items with collection of nodesets
                //open nodeset file and extract namespace
                //then find profile in db and delete
                var repoProfile = scope.ServiceProvider.GetService<IRepository<Profile>>();

                //clean out multiple profiles
                List<string> profileIds = new List<string>();
                foreach (var item in importData.Value)
                {
                    var models = GetModels(item.FileName);
                    foreach (var model in models)
                    {
                        var matches = repoProfile.FindByCondition(x =>
                            x.Namespace.ToLower().Equals(model.ModelUri.ToLower()) &&
                            (string.IsNullOrEmpty(model.Version) || x.Version.ToLower().Equals(model.Version.ToLower())));
                        if (matches.Any())
                        {
                            var ids = matches.ToList().Select(x => x.ID.Value.ToString());
                            profileIds = profileIds.Union(ids).ToList();
                        }
                    }
                }

                //get the ids into a string list for deleting - this will allow for dependencies to 
                //be deleted in proper order as everything is deleted at once 
                if (profileIds.Any())
                {
                    var ids = string.Join(",", profileIds);
                    await repoProfile.ExecStoredProcedureAsync("call public.sp_nodeset_delete({0})", ids);
                    await repoProfile.SaveChangesAsync(); // SP does not get executed until SaveChanges
                }

                //clean out the import log data
                //find the import record using the guid common which was placed as a message in the import log.messages collection
                var repo = scope.ServiceProvider.GetService<IRepository<ImportLog>>();
                var entity = repo.FindByCondition(x => x.Messages.Any(y => y.Message.Contains(_guidCommon.ToString()))).FirstOrDefault();
                //get the ids into a string list for deleting
                if (entity != null)
                {
                    //cascade delete not working...so deleting each part, then deleting entity
                    foreach (var f in entity.Files)
                    {
                        f.Chunks.Clear();
                    }
                    entity.Files.Clear();
                    entity.Messages.Clear();
                    entity.ProfileWarnings.Clear();
                    await repo.UpdateAsync(entity);

                    //just delete the whole import log record and associated data
                    await repo.DeleteAsync(entity);
                    await repo.SaveChangesAsync();
                }
            }
        }

        private static List<ModelTableEntry> GetModels(string fileName)
        {
            //file is either in TestNodesets or TestNodesets\LargeFiles
            var pathParent = System.IO.Path.Combine(Integration.strTestNodeSetDirectory, fileName);
            var pathLargeFiles = System.IO.Path.Combine(Integration.strTestNodeSetDirectory, "LargeFiles", fileName);
            var sourceFileName = System.IO.File.Exists(pathLargeFiles) ? pathLargeFiles : pathParent;

            var contents = System.IO.File.ReadAllText(sourceFileName).Replace("<Value/>", "<Value xsi:nil='true' />");

            using (var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(contents)))
            {
                var nodeSet = UANodeSet.Read(ms);
                var result = new List<ModelTableEntry>();
                if (nodeSet.Models.Any()) result.Add(nodeSet.Models[0]);
                var requiredModels = nodeSet.Models?.SelectMany(m => m.RequiredModel?.Select(rm => rm) ?? new List<ModelTableEntry>());
                return result.Union(requiredModels).ToList();
            }
        }

        private async Task PollImportStatus(MyNamespace.Client apiClient, int itemId)
        {
            var lastMessage = string.Empty;
            var model = new IdIntModel() { ID = itemId };
            ImportLogModel importItem;
            var sw = Stopwatch.StartNew();
            do
            {
                //polling action
                System.Threading.Thread.Sleep(10000);
                importItem = await apiClient.ApiGetItemAsync<ImportLogModel>(URL_GETBYID, model);
                var msgRecent = importItem.Messages.OrderByDescending(x => x.Created).FirstOrDefault();
                //output message if it is different than last
                if (!lastMessage.Equals(msgRecent.Message))
                {
                    output.WriteLine(msgRecent.Message);
                    lastMessage = msgRecent.Message;
                }

            } while (sw.Elapsed < TimeSpan.FromMinutes(15) &&
                     (importItem.Status == TaskStatusEnum.InProgress
                     || importItem.Status == TaskStatusEnum.NotStarted));
            
            if ((importItem?.Status) != TaskStatusEnum.Completed)
            {
                var msgRecent = importItem.Messages.OrderByDescending(x => x.Created).FirstOrDefault();
                output.WriteLine(msgRecent.Message);
            }
            Assert.Equal(TaskStatusEnum.Completed, importItem?.Status);
        }
        #endregion

 
        /// <summary>
        /// do any post test cleanup here.
        /// </summary>
        /// <remarks>this will run after each test. So, if AddItem has 10 iterations of data, this will run once for each iteration.</remarks>
        public override void Dispose()
        {
            ////delete the imported items
            //base.ApiClient.ApiExecuteAsync<Shared.Models.ResultMessageModel>(URL_DELETE_MANY, model.Result).Wait();
            CleanupEntities(_currentImportData).Wait();
            _currentImportData = new KeyValuePair<string, List<ImportFileModel>>();
        }
    }

    /// <summary>
    /// Test data for large nodesets. This is using actual nodesets that are below, just above or well above the allowable 
    /// upload size. Note the chunk size is below the allowable 30mb limit.
    /// </summary>
    internal class TestLargeNodeSetFiles : IEnumerable<object[]>
    {
        const int CHUNK_SIZE = 8 * 1024 * 1024;

        //this is the path and location of the test files and their dependent nodesets (if any)
        //the large nodeset files are named so we can use the file name, replace -SLASH- and then find the profile by namespace
        //also note the large nodeset files are edited to make sure namespace and version follow a certain convention so that
        //we can do clean up after we run the test. 
        internal static List<List<string>> TEST_FILES = new List<List<string>>()
        {
            //10mb - takes about 26 seconds for full import
            new List<string>(){
                $"{Integration.strTestNodeSetDirectory}/LargeFiles/www.Equinor.com.EntTypes.LARGE_NODESET_TEST.xml",
                $"{Integration.strTestNodeSetDirectory}/opcfoundation.org.UA.1.04.2020-07-15.NodeSet2.xml",
                $"{Integration.strTestNodeSetDirectory}/www.OPCFoundation.org.UA.2013.01.ISA95.2013-11-06.NodeSet2.xml"
            }
            //20mb - takes about 2.8 min for full import
            ,new List<string>(){
                $"{Integration.strTestNodeSetDirectory}/LargeFiles/siemens.com.opcua.simatic-s7.LARGE_NODESET_TEST.xml",
                $"{Integration.strTestNodeSetDirectory}/opcfoundation.org.UA.1.05.2022-11-01.NodeSet2.xml",
                $"{Integration.strTestNodeSetDirectory}/opcfoundation.org.UA.DI.2022-11-03.NodeSet2.xml"
            }
            //72mb - takes about 3.6 min for full import
            ,new List<string>(){
                $"{Integration.strTestNodeSetDirectory}/LargeFiles/siemens.com.opcua.LARGE_NODESET_TEST.xml",
                $"{Integration.strTestNodeSetDirectory}/opcfoundation.org.UA.1.05.2022-11-01.NodeSet2.xml",
                $"{Integration.strTestNodeSetDirectory}/opcfoundation.org.UA.DI.2022-11-03.NodeSet2.xml"
            }
        };

        internal static Dictionary<string, List<ImportFileModel>> GetImportFiles()
        {
            var result = new Dictionary<string, List<ImportFileModel>>();

            //get large file which requires chunking AND exceeds max upload size 30mb. 
            //get large file which requires chunking AND is less than max upload size CHUNK_SIZE. 
            //get large file which does NOT require chunking AND is less than max upload size. 

            foreach (var fileSet in TEST_FILES)
            {
                //make 1st file item the key value
                result.Add(System.IO.Path.GetFileName(fileSet[0]),
                    fileSet.Select(x => PrepareImportFile(x)).ToList());
            }

            return result;
        }

        private static ImportFileModel PrepareImportFile(string file)
        {
            var content = System.IO.File.ReadAllText(file);
            int i = 1;
            var fileName = System.IO.Path.GetFileName(file);
            var contentChunked = ChunkContents(content, CHUNK_SIZE);
            var items = new List<ImportFileChunkModel>();
            foreach (var chunk in contentChunked)
            {
                items.Add(new ImportFileChunkModel
                { ChunkOrder = i, Contents = chunk });
                i++;
            }
            return new ImportFileModel()
            {
                FileName = fileName,
                Chunks = items.OrderBy(x => x.ChunkOrder).ToList(),
                TotalBytes = content.Length,
                TotalChunks = items.Count,
            };
        }

        /*
        private static List<byte[]> ChunkContents(byte[] contents, int chunkSize)
        {
            var result = new List<byte[]>();

            if (contents.Length < chunkSize)
            {
                result.Add(contents);
                return result;
            }

            return contents.Chunk(chunkSize).ToList();
        }
        */

        private static List<string> ChunkContents(string contents, int chunkSize)
        {
            var result = new List<string>();

            //set first chunk boundary
            var size = contents.Length;
            var chunkStart = 0;

            while (contents.Length > chunkStart)
            {
                var chunkLength = Math.Min(chunkSize, size - chunkStart);
                //slice current chunk
                var chunk = contents.Substring(chunkStart, chunkLength);
                result.Add(chunk);
                //increment the next chunk boundaries and counter
                chunkStart += chunkSize;

            }

            return result;
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            var files = GetImportFiles();
            var result = new List<object[]>();
            result.Add(new object[] { files });
            return files.Select(f => new object[] { f }).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
