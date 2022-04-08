/* Author:      Chris Muench, C-Labs
 * Last Update: 4/8/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2021
 */

using Opc.Ua.Export;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OPCUAHelpers
{
    //Glossary of Terms:
    //-----------------------------------
    //NodeSet - Container File of one or more Models
    //Model - a unique OPC UA Model identified with a unique NameSpaceUri/ModelUri. A model can be spanned across multiple NodeSet (files)
    //NameSpace - the unique identifier of a Model (also called ModelUri)
    //UAStandardModel - A Model that has been standardized by the OPC UA Foundation and can be found in the official schema store: https://files.opcfoundation.org/schemas/
    //UANodeSetImporter - Imports one or more OPC UA NodeSets resulting in a "NodeSetImportResult" containing all found Models and a list of missing dependencies

    public class NewNodeSetInfo : ModelNameAndVersion
    {
        /// <summary>
        /// Friendly Name of the NodeSet
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Owning organization
        /// </summary>
        public string Organization { get; set; }

        /// <summary>
        /// Pointers to Profile IDs that need to be resolved into Dependencies
        /// </summary>
        public List<string> DependencyIDs { get; set; }
        /// <summary>
        /// List of all Model URI (Namespace) dependencies of the Model
        /// </summary>
        public List<ModelNameAndVersion> Dependencies { get; set; } = new List<ModelNameAndVersion>();
    }

    /// <summary>
    /// Simplified class containing all important information of a NodeSet
    /// </summary>
    public class ModelNameAndVersion
    {
        /// <summary>
        /// The main Model URI (Namespace) 
        /// </summary>
        public string ModelUri { get; set; }
        /// <summary>
        /// Version of the NodeSet
        /// </summary>
        public string ModelVersion { get; set; }
        /// <summary>
        /// Publication date of the NodeSet
        /// </summary>
        public DateTime PublicationDate { get; set; }
        /// <summary>
        /// This is not a valid OPC UA Field and might be hidden inside the "Extensions" node - not sure if its the best way to add this here
        /// </summary>
        public string Author { get; set; }
        /// <summary>
        /// Set to !=0 if this Model is an official OPC Foundation Model and points to an index in a lookup table or cloudlib id
        /// This requires a call to the CloudLib or another Model validation table listing all officially released UA Models
        /// </summary>
        public int? UAStandardModelID { get; set; }
        /// <summary>
        /// Key into the Cache Table
        /// </summary>
        public object CCacheId { get; set; }

        /// <summary>
        /// Compares two NodeSetNameAndVersion using ModelUri and Version. 
        /// </summary>
        /// <param name="thanThis">Compares this to ThanThis</param>
        /// <returns></returns>
        public bool IsNewerOrSame(ModelNameAndVersion thanThis)
        {
            if (thanThis == null)
                return false;
            return ModelUri == thanThis.ModelUri && PublicationDate >= thanThis.PublicationDate;
        }

        /// <summary>
        /// Compares this NameAndVersion to incoming Name and Version prarameters
        /// </summary>
        /// <param name="ofModelUri">ModelUri of version</param>
        /// <param name="ofPublishDate">Publish Date of NodeSet</param>
        /// <returns></returns>
        public bool HasNameAndVersion(string ofModelUri, DateTime ofPublishDate)
        {
            if (string.IsNullOrEmpty(ofModelUri))
                return false;
            return ModelUri == ofModelUri && PublicationDate >= ofPublishDate;
        }

        public override string ToString()
        {
            string uaStandardIdLabel = UAStandardModelID.HasValue ? $", UA-ID: {UAStandardModelID.Value}" : "";
            return $"{ModelUri} (Version: {ModelVersion}, PubDate: {PublicationDate.ToShortDateString()}{uaStandardIdLabel})";
        }
    }


    /// <summary>
    /// Result-Set of this Importer
    /// Check "ErrorMessage" for issues during the import such as missing dependencies
    /// Check "MissingModels" as a list of Models that could not be resolved 
    /// </summary>
    public class UANodeSetImportResult
    {
        /// <summary>
        /// Error Message in case the import was not successful or is missing dependencies
        /// </summary>
        public string ErrorMessage { get; set; } = "";
        /// <summary>
        /// All Imported Models - sorted from least amount of dependencies to most dependencies
        /// </summary>
        public List<ModelValue> Models { get; set; } = new List<ModelValue>();
        /// <summary>
        /// List if missing models listed as ModelUri strings
        /// </summary>
        public List<ModelNameAndVersion> MissingModels { get; set; } = new List<ModelNameAndVersion>();
        /// <summary>
        /// A NodeSet author might add custom "Extensions" to a NodeSet. 
        /// TODO: Make a decision how we handle Comments. xml Comments (xcomment tags) are preserved but inline file comments // or /* */ are not preserved
        /// TODO: Do we need to use this dictionary at all? The Extensions are in The "Models/NodeSet/Extensions" anyway. 
        ///       We could use this to add our own (Profile Editor) extensions that the UA Exporter would write back to the Models/NodeSet/Extension fields
        /// </summary>
        public Dictionary<string, string> Extensions { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Model Value containing all important fast access datapoints of a model
    /// </summary>
    public class ModelValue
    {
        /// <summary>
        /// The imported NodeSet - use this in your subsequent code
        /// </summary>
        public UANodeSet NodeSet { get; set; }
        /// <summary>
        /// File Path to the XML file cache of the NodeSet on the Server
        /// </summary>
        public string FilePath { get; set; }
        /// <summary>
        /// List of all Model URI (Namespace) dependencies of the Model
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();
        /// <summary>
        /// Name and Version of NodeSet
        /// </summary>
        public ModelNameAndVersion NameVersion { get; set; }
        /// <summary>
        /// a Flag telling the consumer that this model was just found and new to this import
        /// </summary>
        public bool NewInThisImport { get; set; }

        public override string ToString()
        {
            return $"{NameVersion}";
        }
    }

    /// <summary>
    /// Main Importer class importing NodeSets 
    /// </summary>
    public static class UANodeSetImporter
    {
        public static byte[] CreateNewUANodeSetAndImport(IUANodeSetCache NodeSetCacheSystem, NewNodeSetInfo tInPara, UserToken TenantID = null)
        {
            var tNs = new UANodeSet();
            tNs.Models = new ModelTableEntry[1];
            tNs.Models[0] = new ModelTableEntry();
            tNs.LastModified = DateTime.Now;
            tNs.Models[0].ModelUri = tInPara.ModelUri;
            tNs.Models[0].PublicationDate = tInPara.PublicationDate == DateTime.MinValue ? DateTime.Now : tInPara.PublicationDate;
            tNs.Models[0].Version = tInPara.ModelVersion ?? "1.00";
            tNs.NamespaceUris = new string[1];
            tNs.NamespaceUris[0] = tInPara.ModelUri;
            tNs.Aliases = new NodeIdAlias[1];
            tNs.Items = new UANode[1];
            if (tInPara.DependencyIDs?.Count > 0)
            {
                foreach (var pId in tInPara.DependencyIDs)
                {
                    var tNS = NodeSetCacheSystem.GetNodeSetByID(pId);
                    if (tNS != null)
                    {
                        tInPara.Dependencies.Add(tNS.NameVersion);
                        if (tNS.Dependencies?.Any() == true)
                        {
                            foreach (var tDep in tNS.NodeSet.Models)
                            {
                                foreach (var tRequ in tDep.RequiredModel)
                                {
                                    if (!tInPara.Dependencies.Any(s => s.HasNameAndVersion(tRequ.ModelUri, tRequ.PublicationDate)))
                                        tInPara.Dependencies.Add(new ModelNameAndVersion { ModelUri = tRequ.ModelUri, ModelVersion = tRequ.Version, PublicationDate = tRequ.PublicationDate });
                                }
                            }
                        }
                    }
                }
            }
            if (tInPara.Dependencies?.Count > 0)
            {
                tNs.Models[0].RequiredModel = new ModelTableEntry[tInPara.Dependencies.Count];
                int i = 0;
                foreach (var tReq in tInPara.Dependencies)
                {
                    tNs.Models[0].RequiredModel[i] = new ModelTableEntry();
                    tNs.Models[0].RequiredModel[i].ModelUri = tReq.ModelUri;
                    tNs.Models[0].RequiredModel[i].Version = tReq.ModelVersion;
                    tNs.Models[0].RequiredModel[i].PublicationDate = tReq.PublicationDate;
                    i++;
                }
            }

            byte[] nsBytes;
            using (var tm = new MemoryStream())
            {
                tNs.Write(tm);
                nsBytes = tm.ToArray();
            }
            //var tTest = System.Text.Encoding.UTF8.GetString(nsBytes);
            return nsBytes;
            //return ImportNodeSets(NodeSetCacheSystem, results, null, new List<byte[]> { nsBytes }, true, TenantID);
            //return new UANodeSetImportResult { Models = new List<ModelValue> { new ModelValue { NameVersion=tInPara, NewInThisImport=true, NodeSet=tNs } } };
        }
        /// <summary>
        /// Imports NodeSets from Files resolving dependencies using already uploaded NodeSets
        /// </summary>
        /// <param name="NodeSetCacheSystem">This interface can be used to override the default file cache of the Importer, i.e with a Database cache</param>
        /// <param name="results">If null, a new resultset will be created. If not null already uploaded NodeSets can be augmented with New NodeSets referred in the FileNames</param>
        /// <param name="nodeSetFilenames">List of full paths to uploaded NodeSets</param>
        /// <param name="nodeSetStreams">List of streams containing NodeSets</param>
        /// <param name="FailOnExisting">Default behavior is that all Models in NodeSets are returned even if they have been imported before. If set to true, the importer will fail if it has imported a nodeset before and does not cache nodeset if they have missing dependencies</param>
        /// <param name="TenantID">If the import has Multi-Tenant Cache, the tenant ID has to be set here</param>
        /// <returns></returns>
        public static UANodeSetImportResult ImportNodeSets(IUANodeSetCache NodeSetCacheSystem, UANodeSetImportResult results, List<string> nodeSetFilenames, List<byte[]> nodeSetStreams, bool FailOnExisting = false, UserToken TenantID = null)
        {
            if (results == null)
                results = new UANodeSetImportResult();
            if (NodeSetCacheSystem == null)
                NodeSetCacheSystem = new UANodeSetFileCache();
            results.ErrorMessage = "";

            try
            {
                if (nodeSetFilenames?.Count > 0)
                {
                    foreach (string nodesetFile in nodeSetFilenames)
                    {
                        NodeSetCacheSystem.LoadNodeSet(results, nodesetFile);
                    }
                }
                bool NewNodeSetFound = false;
                if (nodeSetStreams?.Count > 0)
                {
                    foreach (var nodeStream in nodeSetStreams)
                    {
                        var JustFoundNewNodeSet = NodeSetCacheSystem.LoadNodeSet(results, nodeStream, TenantID);
                        if (!NewNodeSetFound && JustFoundNewNodeSet)
                            NewNodeSetFound = JustFoundNewNodeSet;
                    }
                }
                if (!NewNodeSetFound && FailOnExisting)
                {
                    string names = "";
                    foreach (var mod in results.Models)
                    {
                        if (!string.IsNullOrEmpty(names)) names += ", ";
                        names += mod.NameVersion;
                    }
                    results.ErrorMessage = $"All selected NodeSets or newer versions of them ({names}) have already been imported";
                    return results;
                }
                if (results.Models.Count == 0)
                {
                    results.ErrorMessage = "No Nodesets specified in either nodeSetFilenames or nodeSetStreams";
                    return results;
                }
                FindDependencies(results);

                if (results?.MissingModels?.Count > 0)
                {
                    foreach (var t in results.MissingModels.ToList())
                    {
                        if (NodeSetCacheSystem.LoadNodeSet(results, t, TenantID))
                            continue;
                        //========================================================================================
                        //TODO: Here we try to load the missing models from the CloudLib - for now we print error
                        //========================================================================================
                        if (results.ErrorMessage.Length > 0) results.ErrorMessage += ", ";
                        results.ErrorMessage += t;
                    }
                    FindDependencies(results);
                    if (!string.IsNullOrEmpty(results.ErrorMessage))
                    {
                        results.ErrorMessage = $"The following NodeSets are required: " + results.ErrorMessage;
                        //We must delete newly cached models as they need to be imported again into the backend
                        if (FailOnExisting)
                            NodeSetCacheSystem.DeleteNewlyAddedNodeSetsFromCache(results);
                    }
                }

                results.Models = results.Models.OrderBy(s => s.Dependencies.Count).ToList();
            }
            catch (Exception ex)
            {
                results.ErrorMessage = ex.Message;
            }

            return results;
        }


        #region internal helpers
        /// <summary>
        /// Finds dependencies of NodesSets
        /// </summary>
        /// <param name="results"></param>
        internal static void FindDependencies(UANodeSetImportResult results)
        {
            if (results?.Models?.Count > 0 && results?.MissingModels?.Count > 0)
            {
                for (int i = results.MissingModels.Count - 1; i >= 0; i--)
                {
                    if (results.Models.Any(s => s.NameVersion.IsNewerOrSame(results.MissingModels[i])))
                        results.MissingModels.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Parses Dependencies and creates the Models- and MissingModels collection
        /// </summary>
        /// <param name="results"></param>
        /// <param name="nodeSet"></param>
        /// <param name="ns"></param>
        /// <param name="filePath"></param>
        /// <param name="WasNewSet"></param>
        /// <returns>The ModelValue created or found in the results</returns>
        internal static ModelValue ParseDependencies(UANodeSetImportResult results, UANodeSet nodeSet, ModelTableEntry ns, string filePath, bool WasNewSet)
        {
            var tModel = results.Models.Where(s => s.NameVersion.ModelUri == ns.ModelUri).OrderByDescending(s => s.NameVersion.PublicationDate).FirstOrDefault();
            if (tModel == null)
                results.Models.Add(tModel = new ModelValue { NodeSet = nodeSet, NameVersion = new ModelNameAndVersion { ModelUri = ns.ModelUri, ModelVersion = ns.Version, PublicationDate = ns.PublicationDate }, FilePath = filePath, NewInThisImport = WasNewSet });
            if (ns.RequiredModel?.Any() == true)
            {
                foreach (var tDep in ns.RequiredModel)
                {
                    tModel.Dependencies.Add(tDep.ModelUri);
                    if (!results.MissingModels.Any(s => s.HasNameAndVersion(tDep.ModelUri, tDep.PublicationDate)))
                        results.MissingModels.Add(new ModelNameAndVersion { ModelUri = tDep.ModelUri, ModelVersion = tDep.Version, PublicationDate = tDep.PublicationDate });
                }
            }
            return tModel;
        }
        #endregion
    }
}
