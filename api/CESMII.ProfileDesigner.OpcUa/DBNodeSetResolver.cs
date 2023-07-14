//#define NODESETDBTEST
using CESMII.OpcUa.NodeSetImporter;
using CESMII.OpcUa.NodeSetModel;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Entities;
using Opc.Ua.Cloud.Library.Client;
using Opc.Ua.Export;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CESMII.ProfileDesigner.OpcUa
{
    internal class DBNodeSetResolver : IUANodeSetResolverWithPending
    {
        private readonly IUANodeSetResolverWithPending cloudLibResolver;
        private readonly IDal<Profile, ProfileModel> profileDal;
        private readonly UserToken userToken;
        private readonly Func<ProfileModel, string> exportProfile;

        public DBNodeSetResolver(IUANodeSetResolverWithPending cloudLibResolver, IDal<Profile, ProfileModel> profileDal, UserToken userToken, Func<ProfileModel, string> exportProfile)
        {
            this.cloudLibResolver = cloudLibResolver;
            this.profileDal = profileDal;
            this.userToken = userToken;
            this.exportProfile = exportProfile;
        }

        public async Task<IEnumerable<string>> ResolveNodeSetsAsync(List<ModelNameAndVersion> missingModels)
        {
            List<string> nodesetXmls = new ();
            var missingNamespaces = missingModels.Select(m => m.ModelUri).ToList();
            var profilesInDb = profileDal.Where(p => missingNamespaces.Contains(p.Namespace), userToken, verbose: true).Data;
            profilesInDb = profilesInDb.Where(p => 
                missingModels.Any(m => 
                    m.ModelUri == p.Namespace
                    && (m.PublicationDate == null
                       || (p.PublishDate != null && m.PublicationDate.Value >= p.PublishDate.Value)))).ToList();
                        
            foreach (var profile in profilesInDb)
            {
                var xml = profile.NodeSetFiles.FirstOrDefault()?.FileCache;
                if (string.IsNullOrEmpty(xml))
                {
                    // Export profile to xml
                    xml =  exportProfile(profile);
                }
                if (!string.IsNullOrEmpty(xml))
                {
                    missingModels.RemoveAll(m => m.ModelUri == profile.Namespace && m.PublicationDate <= profile.PublishDate);
                    nodesetXmls.Add(xml);
                }
            }
            var cloudLibXmls = await cloudLibResolver.ResolveNodeSetsAsync(missingModels);
            nodesetXmls.AddRange(cloudLibXmls);
            return nodesetXmls;
        }

        public Func<Nodeset, bool> FilterPendingNodeSet { get => cloudLibResolver.FilterPendingNodeSet; set => cloudLibResolver.FilterPendingNodeSet = value; }
        public OnResolveNodeSets OnResolveNodeSets { get => cloudLibResolver.OnResolveNodeSets; set => cloudLibResolver.OnResolveNodeSets = value; }
        public OnNodeSet OnDownloadNodeSet { get => cloudLibResolver.OnDownloadNodeSet; set => cloudLibResolver.OnDownloadNodeSet = value; }
        public OnNodeSet OnNodeSetFound { get => cloudLibResolver.OnNodeSetFound; set => cloudLibResolver.OnNodeSetFound = value; }
        public OnNodeSet OnNodeSetNotFound { get => cloudLibResolver.OnNodeSetNotFound; set => cloudLibResolver.OnNodeSetNotFound = value; }

    }
}