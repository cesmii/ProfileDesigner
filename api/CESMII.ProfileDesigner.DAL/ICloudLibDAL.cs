﻿namespace CESMII.ProfileDesigner.DAL
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
        /// <summary>
        /// Download the nodeset xml portion of the CloudLib profile
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TModel> GetAsync(string namespaceUri, DateTime? publicationDate, bool exactMatch);

        Task<GraphQlResult<TModel>> Where(int limit, string cursor, bool beforeCursor, List<string> keywords, List<string> exclude = null);

    }


}