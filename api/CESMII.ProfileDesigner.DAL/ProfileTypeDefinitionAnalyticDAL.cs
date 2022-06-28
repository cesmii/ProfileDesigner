namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;

    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Data.Entities;
    using CESMII.ProfileDesigner.Data.Repositories;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// </summary>
    public class ProfileTypeDefinitionAnalyticDAL : BaseDAL<ProfileTypeDefinitionAnalytic, ProfileTypeDefinitionAnalyticModel>, IDal<ProfileTypeDefinitionAnalytic, ProfileTypeDefinitionAnalyticModel>
    {
        public ProfileTypeDefinitionAnalyticDAL(IRepository<ProfileTypeDefinitionAnalytic> repo) : base(repo)
        {
        }

        public override async Task<int?> AddAsync(ProfileTypeDefinitionAnalyticModel model, UserToken userToken)
        {
            var entity = new ProfileTypeDefinitionAnalytic
            {
                ID = null
                //,Created = DateTime.UtcNow
                //,CreatedBy = userId
            };

            this.MapToEntity(ref entity, model, userToken);

            //this will add and call saveChanges
            await base.AddAsync(entity, model, userToken);
            model.ID = entity.ID;
            // Return id for newly added user
            return entity.ID;
        }

        public override async Task<int?> UpdateAsync(ProfileTypeDefinitionAnalyticModel model, UserToken userToken)
        {
            ProfileTypeDefinitionAnalytic entity = _repo.FindByCondition(x => x.ID == model.ID)
                .FirstOrDefault();
            //we don't need or want to cause an update to profile type def entity
            //the ProfileTypeDefinitionId field preserves the relationship.
            entity.ProfileTypeDefinition = null;  
            this.MapToEntity(ref entity, model, userToken);

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();
            return entity.ID;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override ProfileTypeDefinitionAnalyticModel GetById(int id, UserToken userToken)
        {
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();
            return MapToModel(entity);
        }

        /// <summary>
        /// Get all lookup items (no paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override List<ProfileTypeDefinitionAnalyticModel> GetAll(UserToken userToken, bool verbose = false)
        {
            DALResult<ProfileTypeDefinitionAnalyticModel> result = GetAllPaged(userToken, null, null, verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <returns></returns>
        public override DALResult<ProfileTypeDefinitionAnalyticModel> GetAllPaged(UserToken userToken, int? skip, int? take, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var query = _repo.GetAll();
            var count = returnCount ? query.Count() : 0;

            IQueryable<ProfileTypeDefinitionAnalytic> data;
            if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            else if (skip.HasValue) data = query.Skip(skip.Value);
            else if (take.HasValue) data = query.Take(take.Value);
            else data = query;
            var result = new DALResult<ProfileTypeDefinitionAnalyticModel>() {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;
        }

        /// <summary>
        /// This should be used when getting all and the calling code should pass in the where clause.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<ProfileTypeDefinitionAnalyticModel> Where(Expression<Func<ProfileTypeDefinitionAnalytic, bool>> predicate, UserToken user, int? skip = null, int? take = null, 
            bool returnCount = false, bool verbose = false)
        {
            var query = _repo.FindByCondition(predicate);
            var count = returnCount ? query.Count() : 0;

            IQueryable<ProfileTypeDefinitionAnalytic> data;
            if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            else if (skip.HasValue) data = query.Skip(skip.Value);
            else if (take.HasValue) data = query.Take(take.Value);
            else data = query;
            var result = new DALResult<ProfileTypeDefinitionAnalyticModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;
        }

        public async Task<int?> DeleteAsync(int id, UserToken userToken)
        {
            ProfileTypeDefinitionAnalytic entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();
            await _repo.DeleteAsync(entity);
            await _repo.SaveChangesAsync();
            return entity.ID;
        }


        protected override ProfileTypeDefinitionAnalyticModel MapToModel(ProfileTypeDefinitionAnalytic entity, bool verbose = true)
        {
            if (entity != null)
            {
                return new ProfileTypeDefinitionAnalyticModel
                {
                    ID = entity.ID,
                    ProfileTypeDefinitionId = entity.ProfileTypeDefinitionId,
                    PageVisitCount = entity.PageVisitCount,
                    ExtendCount = entity.ExtendCount, 
                    ManualRank = entity.ManualRank
                };
            }
            else
            {
                return null;
            }

        }

        protected override void MapToEntity(ref ProfileTypeDefinitionAnalytic entity, ProfileTypeDefinitionAnalyticModel model, UserToken userToken)
        {
            MapToEntityStatic(ref entity, model, userToken);
        }
        public static void MapToEntityStatic(ref ProfileTypeDefinitionAnalytic entity, ProfileTypeDefinitionAnalyticModel model, UserToken userToken)
        {
            entity.ProfileTypeDefinitionId = model.ProfileTypeDefinitionId;
            entity.PageVisitCount = model.PageVisitCount;
            entity.ExtendCount = model.ExtendCount;
            entity.ManualRank = model.ManualRank;
        }
    }
}