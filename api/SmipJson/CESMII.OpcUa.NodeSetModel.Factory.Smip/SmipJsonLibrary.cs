using CESMII.OpcUa.NodeSetModel.Export.Smip;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Export;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using SMIP.JsonIO.Model;

namespace CESMII.OpcUa.NodeSetModel.Factory.Smip
{
    public class NodeModelExportToSmip
    {
        public static SmipTypeSystem ExportToSmip(NodeSetModel modelToExport)
        {
            var namespaceToExport = modelToExport.ModelUri;
            var libraryName = namespaceToExport.ToLowerInvariant();
            var libraryVersion = NodeModelExportSmip<SmipNode, NodeModel>.Get3PartVersion(modelToExport.Version);
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
                new ObjectTypeModelExportSmip() { _model = objectType }.ExportNode(library);
            }

            return library;
        }
    }
}