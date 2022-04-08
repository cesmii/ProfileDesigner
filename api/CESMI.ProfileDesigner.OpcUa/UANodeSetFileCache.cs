/* Author:      Chris Muench, C-Labs
 * Last Update: 4/8/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2021
 */
using Opc.Ua.Export;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace OPCUAHelpers
{
    public class UserToken
    {
        public int UserId { get; set; }
        public int? TargetTenantId { get; set; } // For now: set to 0 to to write globally, otherwise write to user's scope
        public static UserToken GetGlobalUser(UserToken userToken)
        {
            return new UserToken { UserId = userToken.UserId, TargetTenantId = 0, };
        }
    }

    public interface IUANodeSetCache
    {
        public void LoadNodeSet(UANodeSetImportResult results, string nodesetFileName);
        public bool LoadNodeSet(UANodeSetImportResult results, ModelNameAndVersion nameVersion, object TenantID);
        public bool LoadNodeSet(UANodeSetImportResult results, byte[] nodesetArray, object TenantID);
        public string GetRawModelXML(ModelValue model);
        public void DeleteNewlyAddedNodeSetsFromCache(UANodeSetImportResult results);
        public UANodeSetImportResult FlushCache();
        public ModelValue GetNodeSetByID(string id);
    }

    /// <summary>
    /// Implementation of File Cache - can be replaced with Database cache if necessary
    /// </summary>
    public class UANodeSetFileCache : IUANodeSetCache
    {
        public UANodeSetFileCache()
        {
            RootFolder = Directory.GetCurrentDirectory();
        }

        public UANodeSetFileCache(string pRootFolder)
        {
            RootFolder = pRootFolder;
        }
        static string RootFolder = null;
        /// <summary>
        /// Not Supported on File Cache
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ModelValue GetNodeSetByID(string id)
        {
            return null;
        }

        /// <summary>
        /// By default the Imporater caches all imported NodeSets in a directory called "/NodeSets" under the correct bin directory
        /// This function can be called to flush this cache (for debugging and development purpose only!)
        /// </summary>
        /// <returns></returns>
        public UANodeSetImportResult FlushCache()
        {
            UANodeSetImportResult ret = new UANodeSetImportResult();
            string tPath = Path.Combine(RootFolder, "NodeSets");
            try
            {
                var tFiles = Directory.GetFiles(tPath);
                foreach (var tfile in tFiles)
                {
                    File.Delete(tfile);
                }
            }
            catch (Exception e)
            {
                ret.ErrorMessage = $"Flushing Cache failed: {e}";
            }
            return ret;
        }

        /// <summary>
        /// After the NodeSets were returned by the Importer the succeeding code might fail during processing.
        /// This function allows to remove NodeSets from the cache if the succeeding call failed
        /// </summary>
        /// <param name="results">Set to the result-set coming from the ImportNodeSets message to remove newly added NodeSets from the cache</param>
        public void DeleteNewlyAddedNodeSetsFromCache(UANodeSetImportResult results)
        {
            if (results?.Models?.Count > 0)
            {
                foreach (var tMod in results.Models)
                {
                    if (tMod.NewInThisImport)
                        File.Delete(tMod.FilePath);
                }
            }
        }

        /// <summary>
        /// Returns the content of a cached NodeSet
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public string GetRawModelXML(ModelValue model)
        {
            if (!File.Exists(model?.FilePath))
                return null;
            return File.ReadAllText(model.FilePath);
        }

        /// <summary>
        /// Loads a NodeSet From File.
        /// </summary>
        /// <param name="results"></param>
        /// <param name="nodesetFileName"></param>
        public void LoadNodeSet(UANodeSetImportResult results, string nodesetFileName)
        {
            if (!File.Exists(nodesetFileName))
                return;
            byte[] nsBytes = null;
            using (Stream stream = new FileStream(nodesetFileName, FileMode.Open))
            {
                using (var tm = new MemoryStream())
                {
                    stream.CopyTo(tm);
                    nsBytes = tm.ToArray();
                }
            }
            if (nsBytes?.Length > 0)
                LoadNodeSet(results, nsBytes, null);
            else
            {
                if (results == null)
                    results = new UANodeSetImportResult();
                results.ErrorMessage = "Error during NodeSet Loading";
            }
        }

        public bool LoadNodeSet(UANodeSetImportResult results, ModelNameAndVersion nameVersion, object TenantID)
        {
            //Try to find already uploaded NodeSets using cached NodeSets in the "NodeSets" Folder.
            string tFileName = GetCacheFileName(nameVersion, TenantID);
            if (File.Exists(tFileName))
            {
                LoadNodeSet(results, tFileName);
                return true;
            }
            return false;
        }

        private static string GetCacheFileName(ModelNameAndVersion nameVersion, object TenantID)
        {
            string tPath = Path.Combine(RootFolder, "NodeSets");
            if (!Directory.Exists(tPath))
                Directory.CreateDirectory(tPath);
            if (TenantID != null && (int)TenantID > 0)
            {
                tPath = Path.Combine(tPath, $"{(int)TenantID}");
                if (!Directory.Exists(tPath))
                    Directory.CreateDirectory(tPath);
            }
            string tFile = nameVersion.ModelUri.Replace("http://", "");
            tFile = tFile.Replace('/', '.');
            if (!tFile.EndsWith(".")) tFile += ".";
            string filePath = Path.Combine(tPath, $"{tFile}NodeSet2.xml");
            return filePath;
        }

        /// <summary>
        /// Loads NodeSets from a given byte array and saves new NodeSets to the cache
        /// </summary>
        /// <param name="results"></param>
        /// <param name="nodesetArray"></param>

        /// <returns></returns>
        public bool LoadNodeSet(UANodeSetImportResult results, byte[] nodesetArray, object TenantID)
        {
            bool WasNewSet = false;
            if (nodesetArray?.Length > 0)
            {
                var tStream = new MemoryStream();
                tStream.Write(nodesetArray, 0, nodesetArray.Length);
                tStream.Position = 0;
                UANodeSet nodeSet = UANodeSet.Read(tStream);

                #region Comment processing
                tStream = new MemoryStream();
                tStream.Write(nodesetArray, 0, nodesetArray.Length);
                tStream.Position = 0;
                var doc = XElement.Load(tStream);
                var comments = doc.DescendantNodes().OfType<XComment>();
                foreach (XComment comment in comments)
                {
                    //inline XML Commments are not showing here...only real XML comments (not file comments with /**/)
                    //Unfortunately all OPC UA License Comments are not using XML Comments but file-comments and therefore cannot be "preserved" 
                }
                #endregion

                UANodeSet tOldNodeSet = null;
                if (nodeSet?.Models == null)
                {
                    results.ErrorMessage = $"No Nodeset found in bytes";
                    return false;
                }
                foreach (var ns in nodeSet.Models)
                {
                    //Caching the streams to a "NodeSets" subfolder using the Model Name
                    //Even though "Models" is an array, most NodeSet files only contain one model.
                    //In case a NodeSet stream does contain multiple models, the same file will be cached with each Model Name 
                    string filePath = GetCacheFileName(new ModelNameAndVersion { ModelUri = ns.ModelUri, ModelVersion = ns.Version, PublicationDate = ns.PublicationDate }, TenantID);
                    // TODO How do we update a nodeset with a new version (or if a user keeps editing one and wants to import it to a different editor)?
                    bool CacheNewerVersion = true;
                    if (File.Exists(filePath))
                    {
                        CacheNewerVersion = false;
                        using (Stream nodeSetStream = new FileStream(filePath, FileMode.Open))
                        {
                            if (tOldNodeSet == null)
                                tOldNodeSet = UANodeSet.Read(nodeSetStream);
                            var tns = tOldNodeSet.Models.Where(s => s.ModelUri == ns.ModelUri).OrderByDescending(s => s.PublicationDate).FirstOrDefault();
                            if (tns == null || ns.PublicationDate > tns.PublicationDate)
                                CacheNewerVersion = true; //Cache the new NodeSet if the old (file) did not contain the model or if the version of the new model is greater
                        }
                    }
                    if (CacheNewerVersion) //Cache only newer version
                    {
                        File.WriteAllBytes(filePath, nodesetArray);
                        WasNewSet = true;
                    }
                    UANodeSetImporter.ParseDependencies(results, nodeSet, ns, filePath, WasNewSet);
                }
            }
            return WasNewSet;
        }
    }
}
