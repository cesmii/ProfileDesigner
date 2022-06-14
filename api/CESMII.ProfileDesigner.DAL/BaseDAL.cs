namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    using NLog;

    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Data.Entities;
    using CESMII.ProfileDesigner.Data.Repositories;
    using System.Threading.Tasks;

    public abstract class BaseDAL<TEntity, TModel> where TEntity : AbstractEntity, new() where TModel : AbstractModel
    {
        protected bool _disposed = false;
        protected static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        protected readonly IRepository<TEntity> _repo;
        //TBD - temp mock repo to allow us to load data from static JSON files. 
        protected readonly IMockRepository<TEntity> _repoMock;
        protected readonly bool _useMock = false;

        protected BaseDAL(IRepository<TEntity> repo)
        {
            _repo = repo;
            if (_useMock)
            {
                _repoMock = new MockRepo<TEntity>();
            }
        }

        public void StartTransaction()
        {
            this._repo.StartTransaction();
        }

        public Task CommitTransactionAsync()
        {
            return this._repo.CommitTransactionAsync();
        }

        public void RollbackTransaction()
        {
            this._repo.RollbackTransaction();
        }

        /// <summary>
        /// Get item by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual TModel GetById(int id, UserToken userToken)
        {
            var entity = _repo.FindByCondition(u => u.ID == id).FirstOrDefault();
            return MapToModel(entity);
        }

        public virtual List<TModel> GetAll(UserToken userToken, bool verbose = false)
        {
            var result = _repo.GetAll().ToList();
            return MapToModels(result, verbose);
        }

        public virtual DALResult<TModel> GetAllPaged(UserToken userToken, int? skip, int? take, bool returnCount = false, bool verbose = false)
        {
            var query = _repo.GetAll();
            var count = returnCount ? query.Count() : 0;
            if (skip.HasValue) query = query.Skip(skip.Value);
            if (take.HasValue) query = query.Take(take.Value);
            var result = new DALResult<TModel>
            {
                Count = count,
                Data = MapToModels(query.ToList(), verbose),
                SummaryData = null
            };
            return result;
        }

        public virtual DALResult<TModel> Where(Expression<Func<TEntity, bool>> predicate, UserToken user, int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            return Where(predicate, user, skip, take, returnCount, verbose, null);
        }
        protected virtual DALResult<TModel> Where(Expression<Func<TEntity, bool>> predicate, UserToken user, int? skip = null, int? take = null, bool returnCount = false, bool verbose = false,
            Func<IQueryable<TEntity>, IQueryable<TEntity>> additionalQuery = null)
        {
            var query = _repo.FindByCondition(predicate);
            if (additionalQuery != null)
            {
                query = additionalQuery(query);
            }
            var count = returnCount ? query.Count() : 0;

            //CODE REVIEW From another class: is this optimization really needed?
            //query returns IincludableQuery. Jump through the following to find right combo of skip and take
            //Goal is to have the query execute and not do in memory skip/take
            IQueryable<TEntity> data;
            if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            else if (skip.HasValue) data = query.Skip(skip.Value);
            else if (take.HasValue) data = query.Take(take.Value);
            else data = query;
            var result = new DALResult<TModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;
        }

        public virtual DALResult<TModel> Where(List<Expression<Func<TEntity, bool>>> predicates, UserToken user, int? skip = null, int? take = null, 
            bool returnCount = false, bool verbose = false,
            params OrderByExpression<TEntity>[] orderByExpressions)
        {
            if (predicates == null) predicates = new List<Expression<Func<TEntity, bool>>>();

            //build up a query and append n predicates
            var query = _repo.GetAll().AsQueryable<TEntity>();
            foreach (var p in predicates)
            {
                query = query.Where(p).AsQueryable<TEntity>();
            }
            var count = returnCount ? query.Count() : 0;

            //append order by
            ApplyOrderByExpressions(ref query, orderByExpressions);

            //query returns IincludableQuery. Jump through the following to find right combo of skip and take
            //Goal is to have the query execute and not do in memory skip/take
            IQueryable<TEntity> data;
            if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            else if (skip.HasValue) data = query.Skip(skip.Value);
            else if (take.HasValue) data = query.Take(take.Value);
            else data = query;

            //put together the result
            var result = new DALResult<TModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;
        }

        public virtual int Count(Expression<Func<TEntity, bool>> predicate, UserToken userToken)
        {
            return _repo.FindByCondition(predicate).Count();
        }

        public virtual int Count(UserToken userToken)
        {
            return _repo.GetAll().Count();
        }

        protected virtual IQueryable<TEntity> FindByCondition(UserToken userToken, Expression<Func<TEntity, bool>> predicate, bool cacheOnly = false)
        {
            return _repo.FindByCondition(predicate, cacheOnly);
        }
        protected virtual IQueryable<TEntity> GetAllEntities(UserToken userToken)
        {
            return _repo.GetAll();
        }

        /// <summary>
        /// If the item is present in the DB, then update the existing item. Otherwise insert a new record
        /// </summary>
        /// <remarks>The entity specific DAL class will implement its own CheckForExisting method to determine how to check for existing.</remarks>
        /// <param name="model"></param>
        /// <param name="orgId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<(int?, bool)> UpsertAsync(TModel model, UserToken userToken, bool updateExisting = true)
        {
            TEntity entity = CheckForExisting(model, userToken);
            if (entity == null)
            {
                return (await AddAsync(model, userToken), true);
            }
            else
            {
                model.ID = entity.ID; //assign the id so update knows which row to update. 
                if (updateExisting)
                {
                    return (await UpdateAsync(model, userToken), true);
                }
                return (model.ID, false);
            }
        }

        public virtual Task<int?> UpdateAsync(TModel model, UserToken userToken)
        {
            throw new NotImplementedException();
        }

        public virtual Task<int?> AddAsync(TModel model, UserToken userToken)
        {
            throw new NotImplementedException();
        }

        protected virtual async Task<int?> AddAsync(TEntity entity, TModel model, UserToken userToken)
        {
            entity.ID = null;
            this.MapToEntity(ref entity, model, userToken);

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            // TODO: Have repo return Id of newly created entity
            return entity.ID;
        }

        public virtual Task<int> DeleteManyAsync(List<int> ids, UserToken userToken)
        {
            throw new NotImplementedException();
        }

        public virtual TEntity CheckForExisting(TModel model, UserToken userToken, bool cacheOnly = false)
        {
            if (model.ID != 0 && model.ID != null)
            {
                return _repo.FindByCondition(u => u.ID == model.ID, cacheOnly).FirstOrDefault();
            }
            return null;
        }

        public virtual Task<TModel> GetExistingAsync(TModel model, UserToken userToken, bool cacheOnly)
        {
            var entity = CheckForExisting(model, userToken, cacheOnly);
            if (entity == null) return Task.FromResult<TModel>(null);

            return Task.FromResult(MapToModel(entity, true));
        }

        public Task LoadIntoCacheAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return _repo.LoadIntoCacheAsync(predicate);
        }

        /// <summary>
        /// Map from entity retrieved from db to model used by front end. 
        /// </summary>
        /// <remarks>Verbose is intended to map more of the related data. Each DAL 
        /// can determine how much is enough</remarks>
        protected virtual TModel MapToModel(TEntity entity, bool verbose = true)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Map from entity retrieved from db to model used by front end. 
        /// </summary>
        /// <remarks>Verbose is intended to map more of the related data. Each DAL 
        /// can determine how much is enough. Other DALs may choose to not use and keep the 
        /// mapping the same between getById and GetAll/Where calls.</remarks>
        protected virtual List<TModel> MapToModels(List<TEntity> entities, bool verbose = false)
        {
            var result = new List<TModel>();

            foreach (var item in entities)
            {
                result.Add(MapToModel(item, verbose));
            }
            return result;
        }

        protected virtual void MapToEntity(ref TEntity entity, TModel model, UserToken userToken)
        {
            throw new NotImplementedException();
        }

        #region Helper functions
        protected void ApplyOrderByExpressions(ref IQueryable<TEntity> query, params OrderByExpression<TEntity>[] orderByExpressions)
        {
            //append order bys
            if (orderByExpressions == null) return;

            IOrderedQueryable<TEntity> queryOrdered = null;
            var isFirstExpr = true;
            foreach (var obe in orderByExpressions)
            {
                if (isFirstExpr)
                {
                    queryOrdered = obe.IsDescending ?
                            query.OrderByDescending(obe.Expression) :
                            query.OrderBy(obe.Expression);
                    isFirstExpr = false;
                }
                else
                {
                    queryOrdered = obe.IsDescending ?
                            queryOrdered.ThenByDescending(obe.Expression) :
                            queryOrdered.ThenBy(obe.Expression);
                }
            }
            //now convert it back to iqueryable
            query = queryOrdered.AsQueryable<TEntity>();
        }

        #endregion

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