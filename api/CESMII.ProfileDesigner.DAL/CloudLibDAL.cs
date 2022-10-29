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

        public async Task<GraphQlResult<CloudLibProfileModel>> Where(int limit, string cursor, List<string> keywords, List<string> exclude = null)
        {
            var matches = await _cloudLib.SearchAsync(limit, cursor, keywords, exclude);
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
                        Name = entity.Identifier.ToString(),  //in marketplace items, name is used for navigation in friendly url
                        ExternalAuthor = entity.Metadata?.Contributor?.Name,
                        Contributor = entity.Metadata?.Contributor?.Name, // TODO reconcile this with MarketPlace PublishedModel?
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
                    DisplayName = entity.Metadata?.Title,
                    Namespace = entity.NamespaceUri?.OriginalString,
                    PublishDate = entity.PublicationDate,
                    //Type = _smItemType,
                    Version = entity.Version,
                    //IsFeatured = false,
                    //ImagePortrait = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdPortrait)),
                    ////ImageSquare = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdSquare)),
                    //ImageLandscape = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdLandscape))

                    MetaTags = entity.Metadata.Keywords?.ToList(),
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
                    CloudLibraryId = entity.Id.ToString(),
                    Name = entity.Id.ToString(),  //in marketplace items, name is used for navigation in friendly url
                    ExternalAuthor = entity.Contributor,
                    Contributor = entity.Contributor, // TODO reconcile this with MarketPlace PublishedModel?
                    //TBD
                    //Description = "Description..." + entity.Title,
                    DisplayName = entity.Title,
                    Namespace = entity.NameSpaceUri.ToString(),
                    PublishDate = entity.PublicationDate,
                    //Type = _smItemType,
                    Version = entity.Version,
                    //IsFeatured = false,
                    //ImagePortrait = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdPortrait)),
                    ////ImageSquare = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdSquare)),
                    //ImageLandscape = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdLandscape))
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
                //metatags
                var metatags = entity.Keywords.ToList();
                //if (entity.Category != null) metatags.Add(entity.Category.Name);

                //map results to a format that is common with marketplace items
                return new CloudLibProfileModel()
                {
                    ID = (int) entity.Nodeset.Identifier,//.ToString(),
                    Name = entity.Nodeset.Identifier.ToString(),  //in marketplace items, name is used for navigation in friendly url
                    //Abstract = ns.Title,
                    ExternalAuthor = entity.Contributor.Name,
                    Contributor = entity.Contributor.Name,
                    //TBD
                    Description =
                        (string.IsNullOrEmpty(entity.Description) ? "" : $"<p>{entity.Description}</p>") +
                        (entity.DocumentationUrl == null ? "" : $"<p><a href='{entity.DocumentationUrl.ToString()}' target='_blank' rel='noreferrer' >Documentation: {entity.DocumentationUrl.ToString()}</a></p>") +
                        (entity.ReleaseNotesUrl == null ? "" : $"<p><a href='{entity.ReleaseNotesUrl.ToString()}' target='_blank' rel='noreferrer' >Release Notes: {entity.ReleaseNotesUrl.ToString()}</a></p>") +
                        (entity.LicenseUrl == null ? "" : $"<p><a href='{entity.LicenseUrl.ToString()}' target='_blank' rel='noreferrer' >License Information: {entity.LicenseUrl.ToString()}</a></p>") +
                        (entity.TestSpecificationUrl == null ? "" : $"<p><a href='{entity.TestSpecificationUrl.ToString()}' target='_blank' rel='noreferrer' >Test Specification: {entity.TestSpecificationUrl.ToString()}</a></p>") +
                        (entity.PurchasingInformationUrl == null ? "" : $"<p><a href='{entity.PurchasingInformationUrl.ToString()}' target='_blank' rel='noreferrer' >Purchasing Information: {entity.PurchasingInformationUrl.ToString()}</a></p>") +
                        (string.IsNullOrEmpty(entity.CopyrightText) ? "" : $"<p>{entity.CopyrightText}</p>"),
                    DisplayName = entity.Title,
                    Namespace = entity.Nodeset.NamespaceUri?.OriginalString,
                    MetaTags = metatags,
                    PublishDate = entity.Nodeset.PublicationDate,
                    //Type = _smItemType,
                    Version = entity.Nodeset.Version,
                    //ImagePortrait = entity.IconUrl == null ? 
                    //    _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdPortrait)) :
                    //    new ImageItemModel() { Src= entity.IconUrl.ToString()},
                    ////ImageSquare = _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdSquare)),
                    //ImageLandscape = entity.IconUrl == null ?
                    //    _images.FirstOrDefault(x => x.ID.Equals(_config.DefaultImageIdLandscape)) :
                    //    new ImageItemModel() { Src = entity.IconUrl.ToString() },
                    Updated = entity.Nodeset.LastModifiedDate,
                    NodesetXml = entity.Nodeset.NodesetXml,
                    CloudLibraryId = entity.Nodeset.Identifier.ToString(),
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