//#define NODESETDBTEST
namespace CESMII.ProfileDesigner.OpcUa
{
    using CESMII.ProfileDesigner.DAL;
    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.OpcUa.NodeSetModel;
    using CESMII.OpcUa.NodeSetModel.Factory.Opc;
    using CESMII.ProfileDesigner.OpcUa.NodeSetModelFactory.Profile;
#if NODESETDBTEST
    using Microsoft.EntityFrameworkCore;
#endif
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Opc.Ua.Export;

    public class DalOpcContext : 
#if NODESETDBTEST
        DbOpcUaContext
#else
        DefaultOpcUaContext
#endif
        , IDALContext
    {
        private readonly OpcUaImporter _importer;
        private UserToken _authorToken;
        private UserToken _userToken;
        public DalOpcContext(OpcUaImporter importer, Dictionary<string, NodeSetModel> nodeSetModels, UserToken userToken, UserToken authorToken, bool UpdateExisting)
#if NODESETDBTEST
             : base(nodesetModels, importer.nsDBContext, importer.Logger)
#else
             : base(nodeSetModels, importer.Logger)
#endif
        {
            _importer = importer;
            _authorToken = authorToken;
            _userToken = userToken;
        }

        public void SetUser(UserToken userToken, UserToken authorToken)
        {
            _userToken = userToken;
            _authorToken = authorToken;
        }


        public UserToken authorId => _authorToken;

        public bool UpdateExisting { get; set; }
        public Dictionary<string, ProfileTypeDefinitionModel> profileItemsByNodeId { get; } = new Dictionary<string, ProfileTypeDefinitionModel>();

        public ILogger Logger => _importer.Logger;

        public LookupDataTypeModel GetDataType(string opcNamespace, string opcNodeId)
        {
            return _importer._dtDal.Where(dt => dt.CustomType.Profile.Namespace == opcNamespace && dt.CustomType.OpcNodeId == opcNodeId, _userToken, null, null)?.Data?.FirstOrDefault();
        }

        public ProfileTypeDefinitionModel GetProfileItemById(int? propertyTypeDefinitionId)
        {
            if (propertyTypeDefinitionId == null) return null;
            var item = _importer._dal.GetById(propertyTypeDefinitionId.Value, _userToken);
            return item;
        }

        public Task<(int?, bool)> UpsertAsync(ProfileTypeDefinitionModel profileItem, bool updateExisting)
        {
            return _importer._dal.UpsertAsync(profileItem, _userToken, updateExisting);
        }

        public async Task<int?> CreateCustomDataTypeAsync(LookupDataTypeModel customDataTypeLookup)
        {
            return (await _importer._dtDal.UpsertAsync(customDataTypeLookup, _userToken, false)).Item1;
        }

        public Task<LookupDataTypeModel> GetCustomDataTypeAsync(ProfileTypeDefinitionModel customDataTypeProfile)
        {
            var result = _importer._dtDal.Where(l =>
                (
                    ((customDataTypeProfile.ID ?? 0) != 0 && l.CustomTypeId == customDataTypeProfile.ID)
                    || ((customDataTypeProfile.ID ?? 0) == 0 && l.CustomType != null && l.CustomType.OpcNodeId == customDataTypeProfile.OpcNodeId && l.CustomType.Profile.Namespace == customDataTypeProfile.Profile.Namespace)
                )
                /*&& (l.OwnerId == null || l.OwnerId == _userId)*/,
            _userToken, null, 1, false, false);
            return Task.FromResult(result.Data.FirstOrDefault());
        }

        public object GetNodeSetCustomState(string uaNamespace)
        {
            if (_nodesetModels.TryGetValue(uaNamespace, out var nodesetModel))
            {
                return (ProfileModel)nodesetModel.CustomState;
            }
            return null;
        }

        public EngineeringUnitModel GetOrCreateEngineeringUnitAsync(EngineeringUnitModel engUnit)
        {
            engUnit.ID = _importer._euDal.UpsertAsync(engUnit, _userToken, false).Result.Item1;
            return engUnit;
        }

        public ProfileTypeDefinitionSimpleModel MapToModelProfileSimple(ProfileTypeDefinitionModel profileTypeDef)
        {
            return _importer._profileUtils.MapToModelProfileSimple(profileTypeDef);
        }

        public ProfileModel GetProfileForNamespace(string uaNamespace)
        {
            var result = _importer._profileDal.Where(ns => ns.Namespace == uaNamespace, _userToken, null, null, false, false);
            if (result?.Data?.Any() == true)
            {
                if (result.Data.Count > 1)
                {
                    // TODO handle multiple imported nodeset versions (compute highest etc.)
                    throw new Exception($"Found more than one version of {uaNamespace}");
                }
                var profile = result.Data.FirstOrDefault();
                return profile;
            }
            return null;
        }

        public ProfileTypeDefinitionModel CheckExisting(ProfileTypeDefinitionModel profileItem)
        {
            return _importer._dal.GetExistingAsync(profileItem, _userToken, true).Result;
        }

        public override NodeSetModel GetOrAddNodesetModel(ModelTableEntry model, bool createNew = true)
        {
            if (!_nodesetModels.TryGetValue(model.ModelUri, out var nodesetModel))
            {
                var profile = GetProfileForNamespace(model.ModelUri);
#if NODESETDBTEST
                var existingNodeSet = GetMatchingOrHigherNodeSetAsync(model.ModelUri, model.PublicationDateSpecified ? model.PublicationDate : null).Result;
                if (existingNodeSet != null)
                {
                    _nodesetModels.Add(existingNodeSet.ModelUri, existingNodeSet);
                    nodesetModel = existingNodeSet;
                    nodesetModel.CustomState = profile;
                    return nodesetModel;
                }
#endif
                if (profile != null)
                {
                    model.Version = profile.Version;
                    model.PublicationDate = profile.PublishDate ?? DateTime.MinValue;
                    model.PublicationDateSpecified = profile.PublishDate.HasValue;
                }
                if (model.PublicationDateSpecified && model.PublicationDate.Kind != DateTimeKind.Utc)
                {
                    model.PublicationDate = DateTime.SpecifyKind(model.PublicationDate, DateTimeKind.Utc);
                }

                nodesetModel = base.GetOrAddNodesetModel(model, createNew);
                if (profile != null)
                {
                    nodesetModel.CustomState = profile;
                }
#if NODESETDBTEST
                if (nodesetModel.PublicationDate == null)
                {
                    nodesetModel.PublicationDate = DateTime.MinValue;
                }
                nsDBContext.Add(nodesetModel);
#endif
            }
            return nodesetModel;
        }

#if NODESETDBTEST
        public Task<NodeSetModel> GetMatchingOrHigherNodeSetAsync(string modelUri, DateTime? publicationDate)
        {
            return GetMatchingOrHigherNodeSetAsync(nsDBContext, modelUri, publicationDate);
        }
        public static Task<NodeSetModel> GetMatchingOrHigherNodeSetAsync(NodeSetModelContext dbContext, string modelUri, DateTime? publicationDate)
        {
            var matchingNodeSet = dbContext.NodeSets.AsQueryable().Where(nsm => nsm.ModelUri == modelUri && (publicationDate == null || nsm.PublicationDate >= publicationDate)).OrderBy(nsm => nsm.PublicationDate).FirstOrDefaultAsync();
            return matchingNodeSet;
        }
#endif

    }
}