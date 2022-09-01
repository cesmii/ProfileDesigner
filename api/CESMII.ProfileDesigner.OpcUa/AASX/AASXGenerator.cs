using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using System;
using System.IO.Packaging;
using UANodesetWebViewer.Models;
using System.Collections.Generic;
using Opc.Ua.Export;
using System.Linq;
using System.Reflection;

namespace CESMII.ProfileDesigner.OpcUa.AASX
{
    public class AASXGenerator
    {
        public static byte[] GenerateAAS(List<(UANodeSet nodeSet, string nodeSetXml)> nodeSets)
        {
            try
            {
                //string packagePath = Path.Combine(Directory.GetCurrentDirectory(), "UANodeSet.aasx");
                byte[] aasxPackage;
                using (var aasxPackageStream = new MemoryStream())
                {
                    using (Package package = Package.Open(aasxPackageStream, FileMode.Create))
                    {
                        // add package origin part
                        PackagePart origin = package.CreatePart(new Uri("/aasx/aasx-origin", UriKind.Relative), MediaTypeNames.Text.Plain, CompressionOption.Maximum);
                        using (Stream fileStream = origin.GetStream(FileMode.Create))
                        {
                            var bytes = Encoding.ASCII.GetBytes("Intentionally empty.");
                            fileStream.Write(bytes, 0, bytes.Length);
                        }
                        package.CreateRelationship(origin.Uri, TargetMode.Internal, "http://www.admin-shell.io/aasx/relationships/aasx-origin");
                        var aasxDirectory = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "AASX");
                        // create package spec part
                        string packageSpecPath = Path.Combine(aasxDirectory, "aasenv-with-no-id.aas.xml");
                        using (StringReader reader = new StringReader(System.IO.File.ReadAllText(packageSpecPath)))
                        {
                            XmlSerializer aasSerializer = new XmlSerializer(typeof(AasEnv));
                            AasEnv aasEnv = (AasEnv)aasSerializer.Deserialize(reader);

                            aasEnv.AssetAdministrationShells.AssetAdministrationShell.SubmodelRefs.Clear();
                            aasEnv.Submodels.Clear();

                            foreach (var nodeSet in nodeSets)
                            {
                                var filename = GetFileNameFromModelUri(nodeSet.nodeSet.Models?.FirstOrDefault().ModelUri);
                                string submodelPath = Path.Combine(aasxDirectory, "submodel.aas.xml");
                                using (StringReader reader2 = new StringReader(System.IO.File.ReadAllText(submodelPath)))
                                {
                                    XmlSerializer aasSubModelSerializer = new XmlSerializer(typeof(AASSubModel));
                                    AASSubModel aasSubModel = (AASSubModel)aasSubModelSerializer.Deserialize(reader2);

                                    SubmodelRef nodesetReference = new SubmodelRef();
                                    nodesetReference.Keys = new Keys();
                                    nodesetReference.Keys.Key = new Key
                                    {
                                        IdType = "URI",
                                        Local = true,
                                        Type = "Submodel",
                                        Text = "http://www.opcfoundation.org/type/opcua/" + filename.Replace(".", "").ToLower()
                                    };

                                    aasEnv.AssetAdministrationShells.AssetAdministrationShell.SubmodelRefs.Add(nodesetReference);

                                    aasSubModel.Identification.Text += filename.Replace(".", "").ToLower();
                                    aasSubModel.SubmodelElements.SubmodelElement.SubmodelElementCollection.Value.SubmodelElement.File.Value =
                                        aasSubModel.SubmodelElements.SubmodelElement.SubmodelElementCollection.Value.SubmodelElement.File.Value.Replace("TOBEREPLACED", filename);
                                    aasEnv.Submodels.Add(aasSubModel);
                                }

                                // add nodeset file
                                var strippedFileName = Path.GetFileNameWithoutExtension(filename);
                                PackagePart supplementalDoc = package.CreatePart(new Uri("/aasx/" + strippedFileName, UriKind.Relative), MediaTypeNames.Text.Xml);
                                byte[] nodeSetXmlBytes;
                                if (nodeSet.nodeSetXml == null)
                                {
                                    using (var nodeSetStream = new MemoryStream())
                                    {
                                        nodeSet.nodeSet.Write(nodeSetStream);
                                        nodeSetXmlBytes = nodeSetStream.ToArray();
                                    }
                                }
                                else
                                {
                                    nodeSetXmlBytes = Encoding.UTF8.GetBytes(nodeSet.nodeSetXml);
                                }
                                var nodeSetStream2 = new MemoryStream(nodeSetXmlBytes);
                                CopyStream(nodeSetStream2, supplementalDoc.GetStream());

                                package.CreateRelationship(supplementalDoc.Uri, TargetMode.Internal, "http://www.admin-shell.io/aasx/relationships/aas-suppl");
                            }

                            using (var packageSpecStream = new MemoryStream())
                            {
                                using (XmlTextWriter aasWriter = new XmlTextWriter(packageSpecStream, Encoding.UTF8))
                                {
                                    aasSerializer.Serialize(aasWriter, aasEnv);
                                    aasWriter.Flush();
                                    packageSpecStream.Seek(0, SeekOrigin.Begin);

                                    // add package spec part
                                    PackagePart spec = package.CreatePart(new Uri("/aasx/aasenv-with-no-id/aasenv-with-no-id.aas.xml", UriKind.Relative), MediaTypeNames.Text.Xml);
                                    CopyStream(packageSpecStream, spec.GetStream());

                                    origin.CreateRelationship(spec.Uri, TargetMode.Internal, "http://www.admin-shell.io/aasx/relationships/aas-spec");
                                }
                            }
                        }
                    }
                    aasxPackage = aasxPackageStream.ToArray();
                }
                return aasxPackage;
            }
            catch (Exception ex)
            {
                // TODO figure out error reporting
                throw;
            }
            return null;
        }
        
        private static string GetFileNameFromModelUri(string modelUri)
        {
            string fileName = modelUri.Replace("http://", "");
            fileName = fileName.Replace('/', '.');
            if (!fileName.EndsWith("."))
            {
                fileName += ".";
            }
            return fileName;
        }

        private static void CopyStream(Stream source, Stream target)
        {
            const int bufSize = 0x1000;
            byte[] buf = new byte[bufSize];
            int bytesRead = 0;
            while ((bytesRead = source.Read(buf, 0, bufSize)) > 0)
            {
                target.Write(buf, 0, bytesRead);
            }
        }

    }

}