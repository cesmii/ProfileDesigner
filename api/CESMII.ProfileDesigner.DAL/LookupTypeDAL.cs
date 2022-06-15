namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Linq.Expressions;

    using Microsoft.EntityFrameworkCore;
    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Data.Entities;
    using CESMII.ProfileDesigner.Data.Repositories;

    public class LookupTypeDAL : BaseDAL<LookupType, LookupTypeModel>, IDal<LookupType, LookupTypeModel>
    {
        public LookupTypeDAL(IRepository<LookupType> repo) : base(repo)
        {
        }

        public override Task<int?> AddAsync(LookupTypeModel model, UserToken userToken)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get One
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override LookupTypeModel GetById(int id, UserToken userToken)
        {
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();
            return MapToModel(entity, true);
        }

        /// <summary>
        /// Get All
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override List<LookupTypeModel> GetAll(UserToken userToken, bool verbose = false)
        {
            var result = _repo.GetAll()
                //.Include(p => p.UpdatedBy)
                .OrderBy(s => s.Name)
                .ToList();
            return MapToModels(result, verbose);
        }

        /// <summary>
        /// Get all paged
        /// </summary>
        /// <returns></returns>
        public override DALResult<LookupTypeModel> GetAllPaged(UserToken userToken, int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var result = base.Where(l => true, userToken, skip, take, returnCount, verbose, q => q
                .OrderBy(l => l.Name));
            return result;
        }

        /// <summary>
        /// This should be used when getting all sites and the calling code should pass in the where clause.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<LookupTypeModel> Where(Expression<Func<LookupType, bool>> predicate, UserToken user, int? skip, int? take, 
            bool returnCount = true, bool verbose = false)
        {
            return base.Where(predicate, user, skip, take, returnCount, verbose, q => q
                //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
                .OrderBy(l => l.Name)
                );
        }

        public override Task<int?> UpdateAsync(LookupTypeModel model, UserToken userToken)
        {
            throw new NotSupportedException();
        }

        public Task<int?> DeleteAsync(int id, UserToken userToken)
        {
            throw new NotSupportedException();
        }


        protected override LookupTypeModel MapToModel(LookupType entity, bool verbose = false)
        {
            if (entity != null)
            {
                return new LookupTypeModel
                {
                    ID = entity.ID,
                    Name = entity.Name
                };
            }
            else
            {
                return null;
            }

        }

        protected override void MapToEntity(ref LookupType entity, LookupTypeModel model, UserToken userToken)
        {
            throw new NotSupportedException();
        }
    }
}