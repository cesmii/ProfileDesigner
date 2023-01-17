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

    public abstract class TenantBaseDAL<TEntity, TModel> : BaseDAL<TEntity, TModel> where TEntity : AbstractEntityWithTenant, new() where TModel : AbstractModel
    {
        protected TenantBaseDAL(IRepository<TEntity> repo): base(repo)
        {
        }

        /// <summary>
        /// Get item by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override TModel GetById(int id, UserToken userToken)
        {
            var entity = _repo.FindByCondition(u => u.ID == id && (u.OwnerId == null || u.OwnerId == userToken.UserId)).FirstOrDefault();
            return MapToModel(entity);
        }

        public override List<TModel> GetAll(UserToken userToken, bool verbose = false)
        {
            var result = _repo.FindByCondition(u => u.OwnerId == null || u.OwnerId == userToken.UserId).ToList();
            return MapToModels(result, verbose);
        }

        public override DALResult<TModel> GetAllPaged(UserToken userToken, int? skip, int? take, bool returnCount = false, bool verbose = false)
        {
            var query = _repo.FindByCondition(u => u.OwnerId == null || u.OwnerId == userToken.UserId);
            var count = returnCount ? query.Count() : 0;
            if (skip.HasValue) query = query.Skip(skip.Value);
            if (take.HasValue) query = query.Take(take.Value);
            var result = new DALResult<TModel>
            {
                Count = count,
                Data = MapToModels(query.ToList()),
                SummaryData = null
            };
            return result;
        }

        protected override DALResult<TModel> Where(Expression<Func<TEntity, bool>> predicate, UserToken user, int? skip = null, int? take = null, bool returnCount = false, bool verbose = false,
            Func<IQueryable<TEntity>, IQueryable<TEntity>> additionalQuery = null)
        {
            var query = _repo.FindByCondition(predicate).Where(e => e.OwnerId == null || e.OwnerId == user.UserId);
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
                Count = count
            };
            int retryCount = 50;
            do
            {
                try
                {
                    result.Data = MapToModels(data.ToList(), verbose);
                    retryCount = 0;
                }
                catch (InvalidOperationException)
                {
                    // For some reason (likely on-demand data loading of data) this throws due to modified collection
                    retryCount--;
                    if (retryCount <= 0)
                    {
                        throw;
                    }
                }
            } while (retryCount > 0);
            result.SummaryData = null;
            return result;
        }
        public override DALResult<TModel> Where(List<Expression<Func<TEntity, bool>>> predicates, UserToken userToken, int? skip = null, int? take = null, bool returnCount = false, bool verbose = false, params OrderByExpression<TEntity>[] orderByExpressions)
        {
            return base.Where(
                new List<Expression<Func<TEntity, bool>>>
                {
                    u => u.OwnerId == null || u.OwnerId == userToken.UserId
                }
                .Concat(predicates).ToList(),
                userToken, skip, take, returnCount, verbose, orderByExpressions);
        }

        public override int Count(Expression<Func<TEntity, bool>> predicate, UserToken userToken)
        {
            return _repo.FindByCondition(predicate).Where(u => u.OwnerId == null || u.OwnerId == userToken.UserId).Count();
        }

        public override int Count(UserToken userToken)
        {
            return _repo.GetAll().Where(u => u.OwnerId == null || u.OwnerId == userToken.UserId).Count();
        }

        protected override IQueryable<TEntity> FindByCondition(UserToken userToken, Expression<Func<TEntity, bool>> predicate, bool cacheOnly = false)
        {
            return _repo.FindByCondition(predicate, cacheOnly).Where(u => u.OwnerId == null || u.OwnerId == userToken.UserId || userToken.UserId == -1);
        }
        protected override IQueryable<TEntity> GetAllEntities(UserToken userToken)
        {
            return _repo.GetAll().Where(u => u.OwnerId == null || u.OwnerId == userToken.UserId);
        }

        
        protected override Task<int?> AddAsync(TEntity entity, TModel model, UserToken userToken)
        {
            // For now: TargetTenantId 0 means write globally, otherwise write to user's scope
            entity.OwnerId = userToken.TargetTenantId == 0 ? null : userToken.UserId;
            return base.AddAsync(entity, model, userToken);
        }

        public override Task<int> DeleteManyAsync(List<int> ids, UserToken userToken)
        {
            throw new NotImplementedException();
        }

        public override TEntity CheckForExisting(TModel model, UserToken userToken, bool cacheOnly = false)
        {
            if (model.ID != 0 && model.ID != null)
            {
                return _repo.FindByCondition(u => u.ID == model.ID && (u.OwnerId == null || u.OwnerId == userToken.UserId), cacheOnly).FirstOrDefault();
            }
            return null;
        }

    }
}