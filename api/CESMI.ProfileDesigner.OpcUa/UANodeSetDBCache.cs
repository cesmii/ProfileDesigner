/* Author:      Chris Muench, C-Labs
 * Last Update: 4/8/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2021
 */
using CESMII.OpcUa.NodeSetImporter;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Entities;
using Opc.Ua.Export;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CESMII.ProfileDesigner.Opc.Ua.NodeSetDBCache
{
    public class UANodeSetDBCache : IUANodeSetCache
    {
        private readonly IDal<NodeSetFile, NodeSetFileModel> _dalNodeSetFile;
        private readonly IDal<StandardNodeSet, StandardNodeSetModel> _dalLookupNodeSet;
        private readonly UserToken _userToken;

        public UANodeSetDBCache(IDal<NodeSetFile, NodeSetFileModel> dalNodeSetFile, IDal<StandardNodeSet, StandardNodeSetModel> dalLookupNodeSet, UserToken userToken)
        {
            _dalNodeSetFile = dalNodeSetFile;
            _dalLookupNodeSet = dalLookupNodeSet;
            _userToken = userToken;
        }

        public void DeleteNewlyAddedNodeSetsFromCache(UANodeSetImportResult results)
        {
            if (results?.Models?.Count > 0)
            {
                foreach (var tMod in results.Models)
                {
                    if (tMod.NewInThisImport)
                    {
                        //TODO: @Sean: If a nodeset is deleted from the cache table, some tables with references to the cache table might be broken.
                        _dalNodeSetFile.Delete((tMod.NameVersion.CCacheId as NodeSetFileModel)?.ID ?? 0, _userToken); //Must force delete as Author ID is not in the results
                    }
                }
            }
        }

        public ModelValue GetNodeSetByID(string id)
        {
            var tns = _dalNodeSetFile.GetById(int.Parse(id), _userToken);
            if (tns != null)
            {
                var tmav = new ModelNameAndVersion
                {
                    ModelUri = tns.FileName,
                    ModelVersion = tns.Version,
                    PublicationDate = tns.PublicationDate,
                    CCacheId = tns.ID
                };
                UANodeSetImportResult res = new UANodeSetImportResult();
                using (var nodesetStream = new MemoryStream(Encoding.UTF8.GetBytes(tns.FileCache)))
                {
                    AddNodeSet(res, nodesetStream, _userToken);
                }
                if (res?.Models?.Count > 0)
                    return res.Models[0];
            }
            return null;
        }

        public UANodeSetImportResult FlushCache()
        {
            UANodeSetImportResult ret = new UANodeSetImportResult();
            try
            {
                //TODO: @Sean: If a nodeset is deleted from the cache table, some tables with references to the cache table might be broken.
                var t = _dalNodeSetFile.GetAll(_userToken);
                foreach (var rec in t)
                    _dalNodeSetFile.Delete(rec.ID.Value, _userToken);
            }
            catch (Exception e)
            {
                ret.ErrorMessage = $"Flushing Cache failed: {e}";
            }
            return ret;
        }

        public string GetRawModelXML(ModelValue model)
        {
            return (model?.NameVersion?.CCacheId as NodeSetFileModel)?.FileCache;
        }

        public void AddNodeSet(UANodeSetImportResult results, string nodesetFileName, object TenantID)
        {
            //No return ever as files are not supported with db cache
        }

        public bool GetNodeSet(UANodeSetImportResult results, ModelNameAndVersion nameVersion, object AuthorID)
        {
            var authorToken = AuthorID as UserToken;
            NodeSetFileModel myModel = GetProfileModel(nameVersion, authorToken);

            if (myModel != null)
            {
                using (var nodeSetStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(myModel.FileCache)))
                {
                    //nodeSetStream.Write(Encoding.UTF8.GetBytes(myModel.FileCache));
                    //nodeSetStream.Position = 0;
                    UANodeSet nodeSet = UANodeSet.Read(nodeSetStream);
                    foreach (var ns in nodeSet.Models)
                    {
                        results.AddModelAndDependencies(nodeSet, ns, null, false);
                        foreach (var model in results.Models)
                        {
                            if (model.NameVersion.CCacheId == null)
                            {
                                if (model.NameVersion.ModelUri == nameVersion.ModelUri && model.NameVersion.PublicationDate == nameVersion.PublicationDate)
                                {
                                    model.NameVersion.CCacheId = myModel;
                                }
                                else
                                {
                                    GetProfileModel(model.NameVersion, authorToken);
                                }
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }

        private NodeSetFileModel GetProfileModel(ModelNameAndVersion nameVersion, object userId)
        {
            NodeSetFileModel myModel = (nameVersion.CCacheId as NodeSetFileModel);
            if (myModel != null)
            {
                return myModel;
            }

            var userToken = userId as UserToken;
            myModel = _dalNodeSetFile?.Where(s => s.FileName == nameVersion.ModelUri /*&& (TenantID == null || s.AuthorId == null || s.AuthorId == (int)TenantID)*/, userToken, verbose: true)?.Data?.OrderByDescending(s => s.PublicationDate)?.FirstOrDefault();
            if (myModel != null)
            {
                nameVersion.CCacheId = myModel;
            }
            return myModel;
        }

        public bool AddNodeSet(UANodeSetImportResult results, Stream nodesetStream, object authorId)
        {
            bool WasNewSet = false;
            bool bDisposeStream = false;
            try
            {
                if (!nodesetStream.CanSeek)
                {
                    var nodesetSeekable = new MemoryStream();
                    nodesetStream.CopyTo(nodesetSeekable);
                    nodesetStream = nodesetSeekable;
                    bDisposeStream = true;
                }

                nodesetStream.Position = 0;
                string nodeSetString;
                using(var sr = new StreamReader(nodesetStream, Encoding.UTF8, leaveOpen: true))
                {
                    nodeSetString = sr.ReadToEnd();
                }
                #region Comment Processing
                nodesetStream.Position = 0;
                var doc = XElement.Load(nodesetStream, LoadOptions.PreserveWhitespace);
                var comments = doc.DescendantNodes().OfType<XComment>();
                foreach (XComment comment in comments)
                {
                    //inline XML Commments are not showing here...only real XML comments (not file comments with /**/)
                    //Unfortunately all OPC UA License Comments are not using XML Comments but file-comments and therefore cannot be "preserved" 
                }
                #endregion

                // UANodeSet.Read disposes the stream, so do it when we are done with the stream
                nodesetStream.Position = 0;
                var nodeSet = UANodeSet.Read(nodesetStream);

                if (nodeSet.Models?.Any() != true)
                {
                    nodeSet.Models = new ModelTableEntry[] {
                        new ModelTableEntry { ModelUri = nodeSet.NamespaceUris?.FirstOrDefault(),
                         RequiredModel = new ModelTableEntry[] { new ModelTableEntry { ModelUri = "http://opcfoundation.org/UA/" } },
                         }
                    };
                }

                UANodeSet tOldNodeSet = null;
                foreach (var ns in nodeSet.Models)
                {
                    UserToken userToken = authorId as UserToken;
                    var authorToken = userToken;
                    bool isGlobalNodeSet = CESMII.ProfileDesigner.OpcUa.OpcUaImporter._coreNodeSetUris.Contains(ns.ModelUri);
                    if (isGlobalNodeSet)
                    {
                        userToken = UserToken.GetGlobalUser(userToken); // Write as a global node set shared acess user
                        authorToken = null;
                    }
                    NodeSetFileModel myModel = GetProfileModel(
                        new ModelNameAndVersion
                        {
                            ModelUri = ns.ModelUri,
                            ModelVersion = ns.Version,
                            PublicationDate = ns.PublicationDate,
                        },
                        userToken);
                    bool CacheNewerVersion = true;
                    if (myModel != null)
                    {
                        CacheNewerVersion = false;
                        using (var nodeSetStream = new MemoryStream(Encoding.UTF8.GetBytes(myModel.FileCache)))
                        {
                            if (tOldNodeSet == null)
                                tOldNodeSet = UANodeSet.Read(nodeSetStream);
                            var tns = tOldNodeSet.Models.Where(s => s.ModelUri == ns.ModelUri).OrderByDescending(s => s.PublicationDate).FirstOrDefault();
                            if (tns == null || ns.PublicationDate > tns.PublicationDate)
                                CacheNewerVersion = true; //Cache the new NodeSet if the old (file) did not contain the model or if the version of the new model is greater
                        }
                    }
                    int? cacheId = myModel != null ? myModel.ID : 0;
                    bool newInImport = false;
                    if (CacheNewerVersion) //Cache only newer version
                    {
                        if (myModel == null)
                        {
                            myModel = new NodeSetFileModel
                            {
                                ID = cacheId,
                                FileName = ns.ModelUri,
                                Version = ns.Version,
                                PublicationDate = ns.PublicationDate,
                                // TODO clean up the dependency
                                AuthorId = authorToken?.UserId,
                                FileCache = nodeSetString
                            };
                            // Defer Upsert until later to make it part of a transaction
                            // _dalNodeSetFile.Upsert(myModel, userToken, false);
                            newInImport = true;
                        }
                        // Defer the updates to the import transaction
                        //var resIns = _dalNodeSetFile.Upsert(nsModel, (AuthorID == null) ? 0 : (int)AuthorID, true).GetAwaiter().GetResult();

                        //cacheId = resIns.Item1;
                        //newInImport = resIns.Item2;
                        WasNewSet = true;
                    }
                    var tModel = results.AddModelAndDependencies(nodeSet, ns, null, WasNewSet);
                    if (tModel?.NameVersion != null && myModel != null)
                    {
                        tModel.NameVersion.CCacheId = myModel;
                        tModel.NewInThisImport = newInImport;
                    }
                    foreach (var model in results.Models)
                    {
                        if (model.NameVersion.CCacheId == null)
                        {
                            GetProfileModel(model.NameVersion, userToken);
                        }
                    }
                }
            }
            finally
            {
                if (bDisposeStream)
                {
                    try
                    {
                        nodesetStream?.Dispose();
                    }
                    catch { }
                }
            }
            return WasNewSet;
        }
    }
}
