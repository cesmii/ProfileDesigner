namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using NLog;

    using CESMII.ProfileDesigner.CloudLibClient;

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

        public async Task<CloudLibProfileModel> GetById(string id) {
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
            var matches = await _cloudLib.SearchAsync(limit, cursor, pageBackwards, keywords, exclude);
            if (matches == null) return new GraphQlResult<CloudLibProfileModel>();

            //TBD - exclude some nodesets which are core nodesets - list defined in appSettings

            return new GraphQlResult<CloudLibProfileModel>(matches)
            {
                Edges = MapToModelsNodesetResult(matches.Edges),
            };
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
                //map results to a format that is common with marketplace items
                return new GraphQlNodeAndCursor<CloudLibProfileModel>()
                {
                    Cursor = entityAndCursor.Cursor,
                    Node = new CloudLibProfileModel
                    {
                        ID = null,
                        CloudLibraryId = entity.Identifier.ToString(),
                        ContributorName = entity.Metadata?.Contributor?.Name, // TODO reconcile this with MarketPlace PublishedModel?
                                                                              //TBD
                                                                              //Description = "Description..." + entity.Title,
                        Description = entity.Metadata.Description,
                        //(string.IsNullOrEmpty(entity.Metadata.Description) ? "" : $"<p>{entity.Metadata.Description}</p>") +
                        //(entity.Metadata.DocumentationUrl == null ? "" : $"<p><a href='{entity.Metadata.DocumentationUrl.ToString()}' target='_blank' rel='noreferrer' >Documentation: {entity.Metadata.DocumentationUrl.ToString()}</a></p>") +
                        //(entity.Metadata.ReleaseNotesUrl == null ? "" : $"<p><a href='{entity.Metadata.ReleaseNotesUrl.ToString()}' target='_blank' rel='noreferrer' >Release Notes: {entity.Metadata.ReleaseNotesUrl.ToString()}</a></p>") +
                        //(entity.Metadata.LicenseUrl == null ? "" : $"<p><a href='{entity.Metadata.LicenseUrl.ToString()}' target='_blank' rel='noreferrer' >License Information: {entity.Metadata.LicenseUrl.ToString()}</a></p>") +
                        //(entity.Metadata.TestSpecificationUrl == null ? "" : $"<p><a href='{entity.Metadata.TestSpecificationUrl.ToString()}' target='_blank' rel='noreferrer' >Test Specification: {entity.Metadata.TestSpecificationUrl.ToString()}</a></p>") +
                        //(entity.Metadata.PurchasingInformationUrl == null ? "" : $"<p><a href='{entity.Metadata.PurchasingInformationUrl.ToString()}' target='_blank' rel='noreferrer' >Purchasing Information: {entity.Metadata.PurchasingInformationUrl.ToString()}</a></p>") +
                        //(string.IsNullOrEmpty(entity.Metadata.CopyrightText) ? "" : $"<p>{entity.Metadata.CopyrightText}</p>"),
                        Title = entity.Metadata?.Title,
                        Namespace = entity.NamespaceUri?.OriginalString,
                        PublishDate = entity.PublicationDate,
                        //Type = _smItemType,
                        Version = entity.Version,
                        //IsFeatured = false,
                        //ImagePortrait = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdPortrait)),
                        ////ImageSquare = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdSquare)),
                        //ImageLandscape = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdLandscape))

                        Keywords = entity.Metadata.Keywords?.ToList(),
                        DocumentationUrl = entity.Metadata.DocumentationUrl?.OriginalString,
                        AdditionalProperties = entity.Metadata.AdditionalProperties?.Select(p => new KeyValuePair<string, string>(p.Name, p.Value))?.ToList(),
                        CategoryName = entity.Metadata.Category?.Name,
                        CopyrightText = entity.Metadata.CopyrightText,
                        IconUrl = entity.Metadata.IconUrl?.OriginalString,
                        License = (ProfileLicenseEnum) entity.Metadata.License,
                        LicenseUrl = entity.Metadata.LicenseUrl?.OriginalString,
                        PurchasingInformationUrl = entity.Metadata.PurchasingInformationUrl?.OriginalString,
                        ReleaseNotesUrl = entity.Metadata.ReleaseNotesUrl?.OriginalString,
                        TestSpecificationUrl = entity.Metadata.TestSpecificationUrl?.OriginalString,
                        SupportedLocales = entity.Metadata.SupportedLocales?.ToList(),
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
                ProfileLicenseEnum? profileLicense = null;
                if (Enum.TryParse<ProfileLicenseEnum>(entity.License, out var parsed))
                {
                    profileLicense = parsed;
                }
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
                    License = profileLicense,
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
                    AdditionalProperties = entity.AdditionalProperties?.Select(p => new KeyValuePair<string, string>(p.Name, p.Value))?.ToList(),

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
                    License = (ProfileLicenseEnum) entity.License,
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
                    AdditionalProperties = entity.AdditionalProperties?.Select(p => new KeyValuePair<string, string>(p.Name, p.Value))?.ToList(),

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

        public virtual void Dispose()
        {
            if (_disposed) return;
            //clean up resources
            //set flag so we only run dispose once.
            _disposed = true;
        }
    }
}