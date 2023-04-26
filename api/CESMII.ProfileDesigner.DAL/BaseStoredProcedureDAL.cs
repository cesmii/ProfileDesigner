namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NLog;

    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Data.Entities;
    using CESMII.ProfileDesigner.Data.Repositories;


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TModel"></typeparam>
    public abstract class BaseStoredProcedureDAL<TEntity, TModel> where TEntity : AbstractEntity, new() where TModel : AbstractModel
    {
        protected bool _disposed = false;
        protected static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        protected readonly IRepositoryStoredProcedure<TEntity> _repo;

        protected BaseStoredProcedureDAL(IRepositoryStoredProcedure<TEntity> repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Map from entity retrieved from db to model used by front end. 
        /// </summary>
        protected virtual TModel MapToModel(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public virtual DALResult<TModel> GetItemsAll(string fnName, bool returnCount, List<OrderBySimple> orderBys, params object[] parameters)
        {
            var query = _repo.ExecStoredFunction(fnName, null, null, null, orderBys, parameters);
            var count = returnCount ? this.Count(fnName, parameters) : 0;
            return new DALResult<TModel>()
            {
                Data = MapToModels(query.ToList()),
                Count = count
            };
        }

        public virtual DALResult<TModel> GetItemsPaged(string fnName, int? skip, int? take,
            bool returnCount, List<OrderBySimple> orderBys, params object[] parameters)
        {
            var query = _repo.ExecStoredFunction(fnName, null, skip, take, orderBys, parameters);
            var count = returnCount ? this.Count(fnName, parameters) : 0;
            return new DALResult<TModel>()
            {
                Data = MapToModels(query.ToList()),
                Count = count
            };
        }

        public virtual int Count(string fnName, params object[] parameters)
        {
            //build up order bys to append as string to sql statement
            return _repo.ExecStoredFunctionCount(fnName, parameters);
        }

        /// <summary>
        /// Map from entity retrieved from db to model used by front end. 
        /// </summary>
        protected virtual List<TModel> MapToModels(List<TEntity> entities)
        {
            var result = new List<TModel>();

            foreach (var item in entities)
            {
                result.Add(MapToModel(item));
            }
            return result;
        }

        public virtual void Dispose()
        {
            if (_disposed) return;
            //clean up resources
            _repo.Dispose();
            //set flag so we only run dispose once.
            _disposed = true;
        }

    }

}