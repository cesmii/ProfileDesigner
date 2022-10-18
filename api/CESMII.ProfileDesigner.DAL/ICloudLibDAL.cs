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
        Task<CloudLibProfileModel> DownloadAsync(string id);

        Task<GraphQlResult<TModel>> GetAll();

        /// <summary>
        /// Query is from a free form input box. - This will be appended to a single keywords list
        /// Processes is from a checkboxlist - This will be appended to a single keywords list
        /// Verticals is from a checkboxlist - This will be appended to a single keywords list
        /// </summary>
        /// <remarks>There is no concept in CloudLib of industry verts or process categories so all of these items
        /// are being sent into the generic keywords search which searches on lots of stuff. 
        /// </remarks>
        /// <param name="query"></param>
        /// <param name="processes"></param>
        /// <param name="verticals"></param>
        /// <param name="exclude">List of namespace uris to exclude from results</param>
        /// <returns></returns>
        Task<GraphQlResult<TModel>> Where(int limit, string cursor, List<string> keywords, List<string> exclude = null);

    }


}