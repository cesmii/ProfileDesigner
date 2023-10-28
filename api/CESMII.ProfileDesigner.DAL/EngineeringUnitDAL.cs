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

    public class EngineeringUnitDAL : TenantBaseDAL<EngineeringUnit, EngineeringUnitModel>, IDal<EngineeringUnit, EngineeringUnitModel>
    {
        public EngineeringUnitDAL(IRepository<EngineeringUnit> repo) : base(repo)
        {
        }

        public override async Task<int?> AddAsync(EngineeringUnitModel model, UserToken userToken)
        {
            var entity = new EngineeringUnit
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
        public override EngineeringUnit CheckForExisting(EngineeringUnitModel model, UserToken userToken, bool cacheOnly = false)
        {
            var entity = base.FindByCondition(userToken, dt =>
                (
                  (model.ID != 0 && model.ID != null && dt.ID == model.ID)
                  // UnitId matches within unit namespace
                  || (!string.IsNullOrEmpty(model.NamespaceUri) && model.UnitId != null && dt.NamespaceUri == model.NamespaceUri && model.UnitId == dt.UnitId)
                  // No unit id: match on displayname
                  || (model.UnitId == null && !string.IsNullOrEmpty(model.DisplayName) && dt.DisplayName == model.DisplayName && dt.NamespaceUri == model.NamespaceUri)
                  // everthing matches
                  || (model.UnitId == dt.UnitId && model.NamespaceUri == dt.NamespaceUri && model.DisplayName == dt.DisplayName && model.Description == dt.Description)
                )
                , cacheOnly).FirstOrDefault();
            return entity;
        }

        public override async Task<int?> UpdateAsync(EngineeringUnitModel model, UserToken userToken)
        {
            EngineeringUnit entity = _repo.FindByCondition(x => x.ID == model.ID).FirstOrDefault();
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
        public override EngineeringUnitModel GetById(int id, UserToken userToken)
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
        public override List<EngineeringUnitModel> GetAll(UserToken userToken, bool verbose = false)
        {
            DALResult<EngineeringUnitModel> result = GetAllPaged(userToken, null, null, verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override DALResult<EngineeringUnitModel> GetAllPaged(UserToken userToken, int? skip, int? take, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var result = base.Where(l => l.IsActive, userToken,skip, take, returnCount, verbose, q => q
                    .OrderBy(l => l.NamespaceUri)
                    .ThenBy(l => l.DisplayName)
                    );
            return result;
        }

        /// <summary>
        /// This should be used when getting all sites and the calling code should pass in the where clause.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<EngineeringUnitModel> Where(Expression<Func<EngineeringUnit, bool>> predicate, UserToken user, int? skip = null, int? take = null, 
            bool returnCount = false, bool verbose = false)
        {
            return base.Where(predicate, user, skip, take, returnCount, verbose, q => q
            ////put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
                .Where(l => l.IsActive)
                .OrderBy(l => l.NamespaceUri)
                .ThenBy(l => l.DisplayName)
                );
        }

        public async Task<int?> DeleteAsync(int id, UserToken userToken)
        {
            EngineeringUnit entity = base.FindByCondition(userToken, x => x.ID == id).FirstOrDefault();
            entity.IsActive = false;

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();
            return entity.ID;
        }


        protected override EngineeringUnitModel MapToModel(EngineeringUnit entity, bool verbose = true)
        {
            return MapToModelPublic(entity, verbose);
        }
        public EngineeringUnitModel MapToModelPublic(EngineeringUnit entity, bool verbose = true)
        {
            if (entity == null) return null;
            
            var result = new EngineeringUnitModel
                {
                    ID = entity.ID,
                    DisplayName = entity.DisplayName,
                    Description = entity.Description,
                    NamespaceUri = entity.NamespaceUri,
                    UnitId = entity.UnitId,
                };
            if (verbose)
            {
                //get related data here if needed.
            }
            return result;
        }

        public void MapToEntityPublic(ref EngineeringUnit entity, EngineeringUnitModel model, UserToken userToken)
        {
            MapToEntity(ref entity, model, userToken);
        }
        protected override void MapToEntity(ref EngineeringUnit entity, EngineeringUnitModel model, UserToken userToken)
        {
            entity.DisplayName= model.DisplayName;
            entity.Description = model.Description;
            entity.NamespaceUri = model.NamespaceUri;
            entity.UnitId = model.UnitId;
        }
    }
}