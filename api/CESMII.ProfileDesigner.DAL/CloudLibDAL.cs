namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using NLog;

    using CESMII.Common.CloudLibClient;

    using Opc.Ua.Cloud.Library.Client;
    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Common.Enums;

    /// <summary>
    /// Most lookup data is contained in this single entity and differntiated by a lookup type. 
    /// </summary>
    public class CloudLibDAL : ICloudLibDal<CloudLibProfileModel>
    {
        protected bool _disposed = false;
        protected static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly ICloudLibWrapper _cloudLib;
        //private readonly MarketplaceItemConfig _config;
        //private readonly LookupItemModel _smItemType;

        //supporting data
        //protected List<ImageItemModel> _images;

        public CloudLibDAL(ICloudLibWrapper cloudLib
            //IDal<LookupItem, LookupItemModel> dalLookup,
            //IDal<ImageItem, ImageItemModel> dalImages,
            //ConfigUtil configUtil
            )
        {
            _cloudLib = cloudLib;

            //init some stuff we will use during the mapping methods
            //_config = configUtil.MarketplaceSettings.SmProfile;

            //get SM Profile type
            //_smItemType = dalLookup.Where(
            //    x => x.LookupType.EnumValue.Equals(LookupTypeEnum.SmItemType) &&
            //    x.ID.Equals(_config.TypeId)
            //    , null, null, false, false).Data.FirstOrDefault();

            //get default images
            //_images = dalImages.Where(
            //    x => x.ID.Equals(_config.DefaultImageIdLandscape) ||
            //    x.ID.Equals(_config.DefaultImageIdPortrait) 
            //    //|| x.ID.Equals(_config.DefaultImageIdSquare)
            //    , null, null, false, false).Data;
        }

        public async Task<CloudLibProfileModel> GetById(string id)
        {
            var entity = await _cloudLib.DownloadAsync(id);
            if (entity == null) return null;
            return MapToModelNamespace(entity);
        }

        public async Task<CloudLibProfileModel> DownloadAsync(string id)
        {
            var entity = await _cloudLib.DownloadAsync(id);
            if (entity == null)
            {
                return null;
            }
            //return the whole thing because we also email some info to request info and use
            //other data in this entity.
            var result = MapToModelNamespace(entity);
            return result;
        }

        public async Task<string> UploadAsync(CloudLibProfileModel profile, string nodeSetXml)
        {
            var uaNamespace = MapToNamespace(profile, nodeSetXml);
            var error = await _cloudLib.UploadAsync(uaNamespace);
            return error;
        }

        public async Task<CloudLibProfileModel> GetAsync(string namespaceUri, DateTime? publicationDate, bool exactMatch)
        {
            var entity = await _cloudLib.GetAsync(namespaceUri, publicationDate, exactMatch);
            if (entity == null)
            {
                return null;
            }
            var result = MapToModelNamespace(entity);
            return result;
        }

        public async Task<GraphQlResult<CloudLibProfileModel>> Where(int limit, string cursor, bool pageBackwards, List<string> keywords, List<string> exclude = null)
        {
            var matches = await _cloudLib.SearchAsync(limit, cursor, pageBackwards, keywords, exclude, false);
            if (matches == null) return new GraphQlResult<CloudLibProfileModel>();

            //TBD - exclude some nodesets which are core nodesets - list defined in appSettings

            return new GraphQlResult<CloudLibProfileModel>(matches)
            {
                Edges = MapToModelsNodesetResult(matches.Edges),
            };
        }

        public async Task<GraphQlResult<CloudLibProfileModel>> GetNodeSetsPendingApprovalAsync(int limit, string cursor, bool pageBackwards, AdditionalProperty additionalProperty)
        {
            UAProperty uaProp = null;
            if (additionalProperty != null)
            {
                uaProp = new UAProperty
                {
                    Name = additionalProperty.Name,
                    Value = additionalProperty.Value,
                };
            }
            var matches = await _cloudLib.GetNodeSetsPendingApprovalAsync(limit, cursor, pageBackwards, prop: uaProp);
            if (matches == null) return new GraphQlResult<CloudLibProfileModel>();

            //TBD - exclude some nodesets which are core nodesets - list defined in appSettings

            return new GraphQlResult<CloudLibProfileModel>(matches)
            {
                Edges = MapToModelsNodesetResult(matches.Edges),
            };
        }

        public async Task<CloudLibProfileModel> UpdateApprovalStatusAsync(string cloudLibraryId, string newStatus, string statusInfo)
        {
            var uaNamespace = await _cloudLib.UpdateApprovalStatusAsync(cloudLibraryId, newStatus, statusInfo);

            var cloudLibProfile = MapToModelNamespace(uaNamespace);

            return cloudLibProfile;
        }


        protected List<GraphQlNodeAndCursor<CloudLibProfileModel>> MapToModelsNodesetResult(List<GraphQlNodeAndCursor<Nodeset>> entities)
        {
            var result = new List<GraphQlNodeAndCursor<CloudLibProfileModel>>();

            foreach (var item in entities)
            {
                result.Add(MapToModelNodesetResult(item));
            }
            return result;
        }

        /// <summary>
        /// This is called when searching a collection of items. 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected GraphQlNodeAndCursor<CloudLibProfileModel> MapToModelNodesetResult(GraphQlNodeAndCursor<Nodeset> entityAndCursor)
        {
            if (entityAndCursor != null && entityAndCursor.Node != null)
            {
                var entity = entityAndCursor.Node;
                return new GraphQlNodeAndCursor<CloudLibProfileModel>()
                {
                    Cursor = entityAndCursor.Cursor,
                    Node = new CloudLibProfileModel
                    {
                        ID = null,
                        CloudLibraryId = entity.Identifier.ToString(),
                        ContributorName = entity.Metadata?.Contributor?.Name,
                        Description = entity.Metadata.Description,
                        Title = entity.Metadata?.Title,
                        Namespace = entity.NamespaceUri?.OriginalString,
                        PublishDate = entity.PublicationDate,
                        Version = entity.Version,
                        Keywords = entity.Metadata.Keywords?.ToList(),
                        DocumentationUrl = entity.Metadata.DocumentationUrl?.OriginalString,
                        AdditionalProperties = entity.Metadata.AdditionalProperties?.Select(p => new AdditionalProperty { Name = p.Name, Value = p.Value })?.ToList(),
                        CategoryName = entity.Metadata.Category?.Name,
                        CopyrightText = entity.Metadata.CopyrightText,
                        IconUrl = entity.Metadata.IconUrl?.OriginalString,
                        License = entity.Metadata.License,
                        LicenseUrl = entity.Metadata.LicenseUrl?.OriginalString,
                        PurchasingInformationUrl = entity.Metadata.PurchasingInformationUrl?.OriginalString,
                        ReleaseNotesUrl = entity.Metadata.ReleaseNotesUrl?.OriginalString,
                        TestSpecificationUrl = entity.Metadata.TestSpecificationUrl?.OriginalString,
                        SupportedLocales = entity.Metadata.SupportedLocales?.ToList(),
                        CloudLibApprovalStatus = entity.Metadata.ApprovalStatus,
                        CloudLibApprovalDescription = entity.Metadata.ApprovalInformation,
                    }
                };
            }
            else
            {
                return null;
            }

        }


        protected List<CloudLibProfileModel> MapToModelsNodesetResult(List<UANodesetResult> entities)
        {
            var result = new List<CloudLibProfileModel>();

            foreach (var item in entities)
            {
                result.Add(MapToModelNodesetResult(item));
            }
            return result;
        }

        /// <summary>
        /// This is called when searching a collection of items. 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected CloudLibProfileModel MapToModelNodesetResult(UANodesetResult entity)
        {
            if (entity != null)
            {
                //map results to a format that is common with marketplace items
                return new CloudLibProfileModel()
                {
                    ID = null,
                    Namespace = entity.NameSpaceUri,
                    PublishDate = entity.PublicationDate,
                    //Type = _smItemType,
                    Version = entity.Version,
                    CloudLibraryId = entity.Id.ToString(),
                    //TBD
                    //Description = "Description..." + entity.Title,
                    Title = entity.Title,
                    License = entity.License,
                    LicenseUrl = entity.LicenseUrl?.OriginalString,
                    CopyrightText = entity.CopyrightText,
                    ContributorName = entity.Contributor, // TODO reconcile this with MarketPlace PublishedModel?
                    Description = entity.Description,
                    CategoryName = entity.Category?.Name,
                    DocumentationUrl = entity.DocumentationUrl?.OriginalString,
                    IconUrl = entity.IconUrl?.OriginalString,
                    Keywords = entity.Keywords?.ToList(),
                    PurchasingInformationUrl = entity.PurchasingInformationUrl?.OriginalString,
                    ReleaseNotesUrl = entity.ReleaseNotesUrl?.OriginalString,
                    TestSpecificationUrl = entity.TestSpecificationUrl?.OriginalString,
                    SupportedLocales = entity.SupportedLocales?.ToList(),
                    AdditionalProperties = entity.AdditionalProperties?.Select(p => new AdditionalProperty { Name = p.Name, Value = p.Value })?.ToList(),

                    //IsFeatured = false,
                    //ImagePortrait = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdPortrait)),
                    ////ImageSquare = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdSquare)),
                    //ImageLandscape = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdLandscape)),
                };
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// This is called when getting one nodeset.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        protected CloudLibProfileModel MapToModelNamespace(UANameSpace entity)
        {
            if (entity != null)
            {
                //map results to a format that is common with marketplace items
                return new CloudLibProfileModel()
                {
                    ID = null,
                    Namespace = entity.Nodeset.NamespaceUri?.OriginalString,
                    PublishDate = entity.Nodeset.PublicationDate,
                    //Type = _smItemType,
                    Version = entity.Nodeset.Version,
                    CloudLibraryId = entity.Nodeset.Identifier.ToString(),
                    //TBD
                    //Description = "Description..." + entity.Title,
                    Title = entity.Title,
                    License = entity.License,
                    LicenseUrl = entity.LicenseUrl?.OriginalString,
                    CopyrightText = entity.CopyrightText,
                    ContributorName = entity.Contributor?.Name, // TODO reconcile this with MarketPlace PublishedModel?
                    Description = entity.Description,
                    CategoryName = entity.Category?.Name,
                    DocumentationUrl = entity.DocumentationUrl?.OriginalString,
                    IconUrl = entity.IconUrl?.OriginalString,
                    Keywords = entity.Keywords?.ToList(),
                    PurchasingInformationUrl = entity.PurchasingInformationUrl?.OriginalString,
                    ReleaseNotesUrl = entity.ReleaseNotesUrl?.OriginalString,
                    TestSpecificationUrl = entity.TestSpecificationUrl?.OriginalString,
                    SupportedLocales = entity.SupportedLocales?.ToList(),
                    AdditionalProperties = entity.AdditionalProperties?.Select(p => new AdditionalProperty { Name = p.Name, Value = p.Value })?.ToList(),

                    //IsFeatured = false,
                    //ImagePortrait = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdPortrait)),
                    ////ImageSquare = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdSquare)),
                    //ImageLandscape = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdLandscape)),

                    NodesetXml = entity.Nodeset.NodesetXml,
                };
            }
            else
            {
                return null;
            }

        }

        protected UANameSpace MapToNamespace(CloudLibProfileModel model, string nodeSetXml)
        {
            UANameSpace uaNamespace = new()
            {
                Title = model.Title,
                Description = model.Description,
                License = model.License, // TODO change to string
                LicenseUrl =  !string.IsNullOrEmpty(model.LicenseUrl) ? new Uri(model.LicenseUrl) : null,
                Category = new Category { Name = model.CategoryName },
                Contributor = new Organisation { Name = model.ContributorName },
                CopyrightText = model.CopyrightText,
                DocumentationUrl = !string.IsNullOrEmpty(model.DocumentationUrl) ? new Uri(model.DocumentationUrl) : null,
                IconUrl = !string.IsNullOrEmpty(model.IconUrl) ? new Uri(model.IconUrl) : null,
                PurchasingInformationUrl = !string.IsNullOrEmpty(model.PurchasingInformationUrl) ? new Uri(model.PurchasingInformationUrl) : null,
                Keywords = model.Keywords?.ToArray(),
                ReleaseNotesUrl = !string.IsNullOrEmpty(model.ReleaseNotesUrl) ? new Uri(model.ReleaseNotesUrl) : null,
                TestSpecificationUrl = !string.IsNullOrEmpty(model.TestSpecificationUrl) ? new Uri(model.TestSpecificationUrl) : null,
                SupportedLocales = model.SupportedLocales?.ToArray(),
                AdditionalProperties = model.AdditionalProperties?.Select(kv => new UAProperty { Name = kv.Name, Value = kv.Value }).ToArray(),
                Nodeset = new Nodeset
                {
                    NamespaceUri = !string.IsNullOrEmpty(model.Namespace) ? new Uri(model.Namespace) : null,
                    PublicationDate = model.PublishDate ?? default,
                    Version = model.Version,
                    NodesetXml = nodeSetXml,
                },
            };
            return uaNamespace;
        }

        public virtual void Dispose()
        {
            if (_disposed) return;
            //clean up resources
            //set flag so we only run dispose once.
            _disposed = true;
        }

    }
}