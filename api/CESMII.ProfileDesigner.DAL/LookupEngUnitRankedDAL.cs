namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Data.Entities;
    using CESMII.ProfileDesigner.Data.Repositories;

    public class LookupEngUnitRankedDAL : BaseDAL<EngineeringUnitRanked, EngineeringUnitRankedModel>, IDal<EngineeringUnitRanked, EngineeringUnitRankedModel>
    {
        public LookupEngUnitRankedDAL(IRepository<EngineeringUnitRanked> repo) : base(repo)
        {
        }

        public override Task<int?> AddAsync(EngineeringUnitRankedModel model, UserToken userToken)
        {
            throw new NotSupportedException("This DAL is for getting data only. Use the EngineeringUnitDAL for adding/editing/deleting eng units.");
        }



        public override Task<int?> UpdateAsync(EngineeringUnitRankedModel model, UserToken userToken)
        {
            throw new NotSupportedException("This DAL is for getting data only. Use the EngineeringUnitDAL for adding/editing/deleting eng units.");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override EngineeringUnitRankedModel GetById(int id, UserToken userToken)
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
        public override List<EngineeringUnitRankedModel> GetAll(UserToken userToken, bool verbose = false)
        {
            DALResult<EngineeringUnitRankedModel> result = GetAllPaged(userToken, verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override DALResult<EngineeringUnitRankedModel> GetAllPaged(UserToken userToken, int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var result = base.Where(l => l.IsActive, userToken,skip, take, returnCount, verbose, q => q
                .OrderByDescending(l => l.PopularityLevel)
                //.OrderByDescending(l => l.PopularityIndex)
                //.ThenByDescending(l => l.UsageCount)
                .ThenBy(l => l.DisplayName)
                );
            return result;
        }

        /// <summary>
        /// This should be used when getting all sites and the calling code should pass in the where clause.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<EngineeringUnitRankedModel> Where(Expression<Func<EngineeringUnitRanked, bool>> predicate, UserToken user, int? skip, int? take, 
            bool returnCount = true, bool verbose = false)
        {
            return base.Where(predicate, user, skip, take, returnCount, verbose, q => q
            ////put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            //var query = _repo.FindByCondition(predicate)
                .Where(l => l.IsActive)
                .OrderByDescending(l => l.PopularityLevel)
                //.OrderByDescending(l => l.PopularityIndex)
                //.ThenByDescending(l => l.UsageCount)
                .ThenBy(l => l.DisplayName)
                );
        }

        public Task<int?> DeleteAsync(int id, UserToken userToken)
        {
            throw new NotSupportedException("This DAL is for getting data only. Use the EngineeringUnitDAL for adding/editing/deleting eng units.");
        }


        protected override EngineeringUnitRankedModel MapToModel(EngineeringUnitRanked entity, bool verbose = true)
        {
            if (entity != null)
            {
                return new EngineeringUnitRankedModel
                {
                    ID = entity.ID,
                    Description = entity.Description,
                    DisplayName = entity.DisplayName,
                    NamespaceUri = entity.NamespaceUri, 
                    UnitId = entity.UnitId,
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

        public void MapToEntityPublic(ref EngineeringUnitRanked entity, EngineeringUnitRankedModel model, UserToken userToken)
        {
            MapToEntity(ref entity, model, userToken);
        }
        protected override void MapToEntity(ref EngineeringUnitRanked entity, EngineeringUnitRankedModel model, UserToken userToken)
        {
            throw new NotSupportedException("This DAL is for getting data only. Use the EngineeringUnitDAL for adding/editing/deleting eng units.");
        }
    }
}