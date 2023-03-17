//#define NODESETDBTEST
namespace CESMII.ProfileDesigner.OpcUa
{
    using CESMII.ProfileDesigner.DAL;
    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.OpcUa.NodeSetModel;
    using CESMII.OpcUa.NodeSetModel.Factory.Opc;
    using CESMII.ProfileDesigner.OpcUa.NodeSetModelFactory.Profile;
    using CESMII.OpcUa.NodeSetModel.Opc.Extensions;
#if NODESETDBTEST
    using Microsoft.EntityFrameworkCore;
#endif
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Opc.Ua.Export;
    using CESMII.ProfileDesigner.DAL.Utils;

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

        Dictionary<ProfileTypeDefinitionModel, LookupDataTypeModel> _resolvedDataTypes = new();

        public LookupDataTypeModel GetDataType(string opcNamespace, string opcNodeId)
        {
            List<LookupDataTypeModel> dataTypes;
            if (_nodesetModels.TryGetValue(opcNamespace, out var nodeSetModel))
            {
                dataTypes = _importer._dtDal.Where(dt => 
                    dt.CustomType.Profile.Namespace == nodeSetModel.ModelUri 
                    && dt.CustomType.Profile.PublishDate == nodeSetModel.PublicationDate
                    && dt.CustomType.Profile.Version == nodeSetModel.Version
                    && dt.CustomType.OpcNodeId == opcNodeId, _userToken, null, null)?.Data;
            }
            else
            {
                dataTypes = _importer._dtDal.Where(dt => dt.CustomType.Profile.Namespace == opcNamespace && dt.CustomType.OpcNodeId == opcNodeId, _userToken, null, null)?.Data;
            }
            return dataTypes?.FirstOrDefault();
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
            if (_resolvedDataTypes.ContainsKey(customDataTypeLookup.CustomType))
            {
                // ignore
            }
            _resolvedDataTypes[customDataTypeLookup.CustomType] = customDataTypeLookup;
            return (await _importer._dtDal.UpsertAsync(customDataTypeLookup, _userToken, false)).Item1;
        }

        public Task<LookupDataTypeModel> GetCustomDataTypeAsync(ProfileTypeDefinitionModel customDataTypeProfile, bool cacheOnly = false)
        {
            if (_resolvedDataTypes.TryGetValue(customDataTypeProfile, out LookupDataTypeModel lookupDataTypeModel))
            {
                return Task.FromResult(lookupDataTypeModel);
            }
            if (cacheOnly)
            {
                return Task.FromResult<LookupDataTypeModel>(null);
            }

            var result = _importer._dtDal.Where(
                l => (
                    ((customDataTypeProfile.ID ?? 0) != 0 && l.CustomTypeId == customDataTypeProfile.ID)
                    || ((customDataTypeProfile.ID ?? 0) == 0 && l.CustomType != null && l.CustomType.OpcNodeId == customDataTypeProfile.OpcNodeId
                        && ((l.CustomType.Profile.ID != null && l.CustomType.Profile.ID == customDataTypeProfile.Profile.ID)
                            || (
                               l.CustomType.Profile.Namespace == customDataTypeProfile.Profile.Namespace
                            && l.CustomType.Profile.PublishDate == customDataTypeProfile.Profile.PublishDate
                            && l.CustomType.Profile.Version == customDataTypeProfile.Profile.Version
                            && l.CustomType.Profile.OwnerId == customDataTypeProfile.Profile.AuthorId
                            )
                        ))
                    ),
                _userToken, null, 1, false, false);
            var dtLookupModel = result.Data.FirstOrDefault();
            if (result.Data.Count > 1)
            {
                throw new Exception($"Internal error: more than one ({result.Data.Count}) LookupDataTypes {dtLookupModel} for type {customDataTypeProfile} exists in the database or EF cache.");
            }
            if (dtLookupModel !=null)
            {
                if (!_resolvedDataTypes.TryAdd(customDataTypeProfile, dtLookupModel))
                {
                    throw new Exception($"Internal error: LookupDataType {dtLookupModel} for type {customDataTypeProfile} already exists in resolver cache.");
                }
            }
            return Task.FromResult(dtLookupModel);
        }

        public bool RegisterCustomTypePlaceholder(LookupDataTypeModel dtLookupModel)
        {
            return _resolvedDataTypes.TryAdd(dtLookupModel.CustomType, dtLookupModel);
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
            return ProfileMapperUtil.MapToModelProfileSimple(profileTypeDef);
        }

        public ProfileModel GetProfileForNamespace(string uaNamespace, DateTime? publicationDate, string version)
        {
            var result = _importer._profileDal.Where(ns => ns.Namespace == uaNamespace && ns.PublishDate == publicationDate && ns.Version == version, _userToken, null, null, false, false);
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
                var profile = GetProfileForNamespace(model.ModelUri, model.GetNormalizedPublicationDate(), model.Version);
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
                else
                {
                    if (string.IsNullOrEmpty(model.Version))
                    {
                        Logger.LogWarning($"Requested NodeSet {model.ModelUri} ({model.PublicationDate}) has no Version");
                    }
                    if (!model.PublicationDateSpecified)
                    {
                        Logger.LogWarning($"Requested NodeSet {model.ModelUri} ({model.Version}) has no publication date");
                    }
                }
                if (model.PublicationDateSpecified && model.PublicationDate.Kind != DateTimeKind.Utc)
                {
                    model.PublicationDate = model.GetNormalizedPublicationDate();
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