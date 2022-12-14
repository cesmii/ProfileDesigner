using CESMII.OpcUa.NodeSetModel.Export.ThinkIQ;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Export;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using SMIP.JsonIO.Model;

namespace CESMII.OpcUa.NodeSetModel.Factory.ThinkIQ
{
    //public class ThinkIQLibrary : TiqTypePackage
    //{
    //    [JsonProperty("attribute_types")]
    //    public List<TiqAttributeType> AttributeTypes { get; set; }
    //    [JsonProperty("libraries")]
    //    public List<LibraryMeta> Libraries { get; set; }
    //}

    //public class LibraryMeta : TiqBase
    //{
    //    [JsonProperty("version")]
    //    public string Version { get; internal set; }
    //}

    //public class AttributeType : TiqAttributeType
    //{
    //    [JsonProperty("is_required")]
    //    public bool IsRequired { get; set; }

    //    [JsonProperty("default_value")]
    //    public string DefaultValue { get; internal set; }

    //    [JsonProperty("attribute_type_fqn")]
    //    public string[] AttributeTypeFqn { get; internal set; }
        
    //    // importance
    //    // edit_status
    //}

    //public class TiqType : TiqEquipmentType
    //{
    //    //public List<string> fqn;
    //    //public string description { get; internal set; }
    //    //public string display_name { get; internal set; }
    //    //public string relative_name { get; internal set; }

    //    [JsonProperty("sub_type_of_fqn")]
    //    public List<string> SubtypeOfFqn { get; set; }

    //    [JsonProperty("updated_timestamp")]
    //    public DateTime? UpdatedTimestamp { get; set; }

    //    [JsonProperty("unlink_relative_name")]
    //    public bool UnlinkRelativeName { get; set; }
    //    //public List<EquipmentType> child_equipment { get; internal set; }
    //    // opcua_methods
    //    // classification
    //    // edit_status
    //}

    //public class ChildEquipmentType : TiqChildEquipmentType
    //{
    //    [JsonProperty("is_required")]
    //    public bool IsRequired { get; set; }
    //}

    //public class Fqn : List<string>
    //{
    //    public Fqn(string[] fqn)
    //    {
    //        this.Clear();
    //        this.AddRange(fqn);
    //    }
    //    public static implicit operator Fqn(string[] fqn) => new Fqn(fqn);
    //}



    public class NodeModelExportOpc
    {
        public static SmipTypeSystem ExportToThinkIQ(List<(UANodeSet nodeSet, string xml, NodeSetModel model, Dictionary<string, NodeSetModel> requiredModels)> exportedNodeSets)
        {
            var modelToExport = exportedNodeSets.FirstOrDefault().model;
            var namespaceToExport = modelToExport.ModelUri;
            var libraryName = namespaceToExport.ToLowerInvariant();
            var libraryVersion = NodeModelExportThinkIQ<SmipNode, NodeModel>.Get3PartVersion(modelToExport.Version);
            var library = new SmipTypeSystem();
            library.Meta = new SmipMeta
            {
                ExportLibraryFqn = new List<string> { libraryName },
                ExportTimestamp = modelToExport.PublicationDate,
                FileVersion = "4.0.1", // File format 
            };

            if (library.Libraries == null)
            {
                library.Libraries = new List<SmipLibrary>();
            }

            var libMeta = new SmipLibrary
            {
                Fqn = new List<string> { libraryName },
                Version = libraryVersion,
                RelativeName = libraryName,
            };
            library.Libraries.Add(libMeta);
            foreach (var objectType in modelToExport.ObjectTypes)
            {
                new ObjectTypeModelExportThinkIQ() { _model = objectType }.ExportNode(library);
            }
            return library;
        }

    }
}