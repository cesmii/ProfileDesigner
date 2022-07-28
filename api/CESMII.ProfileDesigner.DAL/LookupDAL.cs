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
    /// Most looku dat is contained in this single entity and differntiated by a lookup type. 
    /// </summary>
    public class LookupDAL : BaseDAL<LookupItem, LookupItemModel>, IDal<LookupItem, LookupItemModel>
    {
        public LookupDAL(IRepository<LookupItem> repo) : base(repo)
        {
        }

        public override async Task<int?> AddAsync(LookupItemModel model, UserToken userToken)
        {
            var entity = new LookupItem
            {
                ID = null
            };

            this.MapToEntity(ref entity, model, userToken);
            //do this after mapping to enforce isactive is true on add
            entity.IsActive = true;

            //this will add and call saveChanges
            await base.AddAsync(entity, model, userToken);
            model.ID = entity.ID;
            // Return id for newly added user
            return entity.ID;
        }

        public override async Task<int?> UpdateAsync(LookupItemModel model, UserToken userToken)
        {
            LookupItem entity = base.FindByCondition(userToken, x => x.ID == model.ID).FirstOrDefault();
            this.MapToEntity(ref entity, model, userToken);

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();
            return entity.ID;
        }

        public override LookupItem CheckForExisting(LookupItemModel model, UserToken userToken, bool cacheOnly = false)
        {
            var existing = base.FindByCondition(userToken, l =>
                (model.ID != 0 && model.ID != null && l.ID == model.ID)
                || l.Code == model.Code, cacheOnly).FirstOrDefault();
            return existing;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override LookupItemModel GetById(int id, UserToken userToken)
        {
            var entity = base.FindByCondition(userToken, x => x.ID == id)
                .Include(l => l.LookupType)
                .FirstOrDefault();
            return MapToModel(entity);
        }

        /// <summary>
        /// Get all lookup items (no paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override List<LookupItemModel> GetAll(UserToken userToken, bool verbose = false)
        {
            DALResult<LookupItemModel> result = GetAllPaged(userToken, null, null, verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override DALResult<LookupItemModel> GetAllPaged(UserToken userToken, int? skip, int? take, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var result = base
                .Where(l => l.IsActive, userToken, skip, take, returnCount, verbose, q => q
                    .Include(l => l.LookupType)
                    .OrderBy(l => l.LookupType.Name)
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
        public override DALResult<LookupItemModel> Where(Expression<Func<LookupItem, bool>> predicate, UserToken user, int? skip = null, int? take = null, 
            bool returnCount = false, bool verbose = false)
        {
            return base.Where(predicate, user, skip, take, returnCount, verbose, q => q
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
                .Where(l => l.IsActive)
                .Include(l => l.LookupType)
                .OrderBy(l => l.LookupType.Name)
                .ThenBy(l => l.DisplayOrder)
                .ThenBy(l => l.Name)
                );
        }

        public async Task<int?> DeleteAsync(int id, UserToken userToken)
        {
            LookupItem entity = base.FindByCondition(userToken, x => x.ID == id).FirstOrDefault();
            entity.IsActive = false;

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();
            return entity.ID;
        }


        protected override LookupItemModel MapToModel(LookupItem entity, bool verbose = true)
        {
            if (entity != null)
            {
                return new LookupItemModel
                {
                    ID = entity.ID,
                    Name = entity.Name,
                    Code = entity.Code,
                    DisplayOrder = entity.DisplayOrder,
                    TypeId = entity.TypeId,
                    LookupType = (CESMII.ProfileDesigner.Common.Enums.LookupTypeEnum)entity.TypeId
                };
            }
            else
            {
                return null;
            }

        }

        protected override void MapToEntity(ref LookupItem entity, LookupItemModel model, UserToken userToken)
        {
            MapToEntityStatic(ref entity, model, userToken);
        }
        public static void MapToEntityStatic(ref LookupItem entity, LookupItemModel model, UserToken userToken)
        {
            entity.Name = model.Name;
            entity.Code = model.Code;
            entity.DisplayOrder = model.DisplayOrder;
            entity.TypeId = model.TypeId;
        }
    }
}