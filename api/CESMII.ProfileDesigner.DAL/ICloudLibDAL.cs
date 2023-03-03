namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using CESMII.ProfileDesigner.DAL.Models;
    using Opc.Ua.Cloud.Library.Client;

    public interface ICloudLibDal<TModel> : IDisposable where TModel : AbstractModel
    {
        Task<TModel> GetById(string id);

        /// <summary>
        /// Download the nodeset xml portion of the CloudLib profile
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TModel> DownloadAsync(string id);
        Task<string> UploadAsync(TModel profile, string nodeSetXml);
        /// <summary>
        /// Download the nodeset xml portion of the CloudLib profile
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TModel> GetAsync(string namespaceUri, DateTime? publicationDate, bool exactMatch);

        Task<GraphQlResult<TModel>> Where(int limit, string cursor, bool beforeCursor, List<string> keywords, List<string> exclude = null);
        Task<GraphQlResult<CloudLibProfileModel>> GetNodeSetsPendingApprovalAsync(int limit, string cursor, bool pageBackwards, AdditionalProperty additionalProperty);
        Task<CloudLibProfileModel> UpdateApprovalStatusAsync(string cloudLibraryId, string newStatus, string statusInfo);
        /// <summary>
        /// Name of the "AdditionalProperty" that is used to keep user info in the cloud library
        /// </summary>
        // TODO hide the need for this in a method?
        public const string strCESMIIUserInfo = "CESMIIUserInfo";
    }


}