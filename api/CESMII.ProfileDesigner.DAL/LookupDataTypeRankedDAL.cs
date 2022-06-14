namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Data.Entities;
    using CESMII.ProfileDesigner.Data.Repositories;

    public class LookupDataTypeRankedDAL : TenantBaseDAL<LookupDataTypeRanked, LookupDataTypeRankedModel>, IDal<LookupDataTypeRanked, LookupDataTypeRankedModel>
    {
        public LookupDataTypeRankedDAL(IRepository<LookupDataTypeRanked> repo) : base(repo)
        {
        }

        public override Task<int?> AddAsync(LookupDataTypeRankedModel model, UserToken userToken)
        {
            throw new NotSupportedException("This DAL is for getting data only. Use the LookupDataTypeDAL for adding/editing/deleting data types.");
        }



        public override Task<int?> UpdateAsync(LookupDataTypeRankedModel model, UserToken userToken)
        {
            throw new NotSupportedException("This DAL is for getting data only. Use the LookupDataTypeDAL for adding/editing/deleting data types.");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override LookupDataTypeRankedModel GetById(int id, UserToken userToken)
        {
            var entity = base.FindByCondition(userToken, x => x.ID == id)
                .FirstOrDefault();
            return MapToModel(entity, true);
        }

        /// <summary>
        /// Get all lookup items (no paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override List<LookupDataTypeRankedModel> GetAll(UserToken userToken, bool verbose = false)
        {
            DALResult<LookupDataTypeRankedModel> result = GetAllPaged(userToken, verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override DALResult<LookupDataTypeRankedModel> GetAllPaged(UserToken userToken, int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var result = base.Where(l => l.IsActive, userToken,skip, take, returnCount, verbose, q => q
                .OrderByDescending(l => l.PopularityLevel)
                //.OrderByDescending(l => l.PopularityIndex)
                //.ThenByDescending(l => l.UsageCount)
                .ThenBy(l => l.DisplayOrder)
                .ThenBy(l => l.Name)
                );
            return result;
        }

        /// <summary>
        /// This should be used when getting all sites and the calling code should pass in the where clause.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<LookupDataTypeRankedModel> Where(Expression<Func<LookupDataTypeRanked, bool>> predicate, UserToken user, int? skip, int? take, 
            bool returnCount = true, bool verbose = false)
        {
            return base.Where(predicate, user, skip, take, returnCount, verbose, q => q
            ////put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            //var query = _repo.FindByCondition(predicate)
                .Where(l => l.IsActive)
                .OrderByDescending(l => l.PopularityLevel)
                //.OrderByDescending(l => l.PopularityIndex)
                //.ThenByDescending(l => l.UsageCount)
                .ThenBy(l => l.DisplayOrder)
                .ThenBy(l => l.Name)
                );
        }

        public Task<int?> DeleteAsync(int id, UserToken userToken)
        {
            throw new NotSupportedException("This DAL is for getting data only. Use the LookupDataTypeDAL for adding/editing/deleting data types.");
        }


        protected override LookupDataTypeRankedModel MapToModel(LookupDataTypeRanked entity, bool verbose = true)
        {
            if (entity != null)
            {
                return new LookupDataTypeRankedModel
                {
                    ID = entity.ID,
                    Name = entity.Name,
                    Code = entity.Code,
                    IsNumeric = entity.IsNumeric,
                    DisplayOrder = entity.DisplayOrder,
                    UseMinMax = entity.UseMinMax,
                    UseEngUnit = entity.UseEngUnit, 
                    CustomTypeId = entity.CustomTypeId,
                    OwnerId = entity.OwnerId,
                    PopularityIndex = entity.PopularityIndex,
                    ManualRank = entity.ManualRank,
                    UsageCount = entity.UsageCount,
                    PopularityLevel = entity.PopularityLevel,
                };
            }
            else
            {
                return null;
            }

        }

        public void MapToEntityPublic(ref LookupDataTypeRanked entity, LookupDataTypeRankedModel model, UserToken userToken)
        {
            MapToEntity(ref entity, model, userToken);
        }
        protected override void MapToEntity(ref LookupDataTypeRanked entity, LookupDataTypeRankedModel model, UserToken userToken)
        {
            throw new NotSupportedException("This DAL is for getting data only. Use the LookupDataTypeDAL for adding/editing/deleting data types.");
        }
    }
}