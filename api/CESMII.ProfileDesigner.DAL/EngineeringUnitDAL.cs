﻿namespace CESMII.ProfileDesigner.DAL
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
        public EngineeringUnitDAL(IRepository<EngineeringUnit> repo, IServiceProvider serviceProvider) : base(repo)
        {
            // TODO Clean this up so we only use the interface
            _serviceProvider = serviceProvider;
        }

        private ProfileTypeDefinitionDAL _profileTypeDefinitionDALPrivate;
        ProfileTypeDefinitionDAL _profileTypeDefinitionDAL
        {
            get
            {
                if (_profileTypeDefinitionDALPrivate == null)
                {
                    _profileTypeDefinitionDALPrivate = _serviceProvider.GetService<IDal<ProfileTypeDefinition, ProfileTypeDefinitionModel>>() as ProfileTypeDefinitionDAL;
                }
                return _profileTypeDefinitionDALPrivate;
            }
        }
        private IServiceProvider _serviceProvider;

        public override async Task<int?> Add(EngineeringUnitModel model, UserToken userToken)
        {
            EngineeringUnit entity = new EngineeringUnit
            {
                ID = null,
                //,Created = DateTime.UtcNow
                //,CreatedBy = userId
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
            //var entity = base.CheckForExisting(model, tenantId);
            //if (entity != null && (entity.OwnerId == null || entity.OwnerId == tenantId))
            //{
            //    return entity;
            //}
            var entity = base.FindByCondition(userToken, dt =>
                (
                  (model.ID != 0 && model.ID != null && dt.ID == model.ID)
                  || (dt.UnitId != null && model.UnitId == dt.UnitId)
                  || (dt.UnitId == null && dt.DisplayName == model.DisplayName && dt.NamespaceUri == model.NamespaceUri)
                )
                /*&& (dt.OwnerId == null || dt.OwnerId == tenantId)*/, cacheOnly).FirstOrDefault();
            return entity;
        }

        public override async Task<int?> Update(EngineeringUnitModel model, UserToken userToken)
        {
            EngineeringUnit entity = _repo.FindByCondition(x => x.ID == model.ID).FirstOrDefault();
            //model.Updated = DateTime.UtcNow;
            this.MapToEntity(ref entity, model, userToken);

            await _repo.UpdateAsync(entity);
            await _repo.SaveChanges();
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
            DALResult<EngineeringUnitModel> result = GetAllPaged(userToken, verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override DALResult<EngineeringUnitModel> GetAllPaged(UserToken userToken, int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var result = base.Where(l => l.IsActive, userToken,skip, take, returnCount, verbose, q => q
                    .OrderBy(l => l.NamespaceUri)
                    .ThenBy(l => l.DisplayName)
                    );
            return result;
            //var count = returnCount ? query.Count() : 0;
            ////query returns IincludableQuery. Jump through the following to find right combo of skip and take
            ////Goal is to have the query execute and not do in memory skip/take
            //IQueryable<LookupDataType> data;
            //if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            //else if (skip.HasValue) data = query.Skip(skip.Value);
            //else if (take.HasValue) data = query.Take(take.Value);
            //else data = query;

            //DALResult<EngineeringUnitModel> result = new DALResult<LookupDataTypeModel>();
            //result.Count = count;
            //result.Data = MapToModels(data.ToList(), verbose);
            //result.SummaryData = null;
            //return result;
        }

        /// <summary>
        /// This should be used when getting all sites and the calling code should pass in the where clause.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<EngineeringUnitModel> Where(Expression<Func<EngineeringUnit, bool>> predicate, UserToken user, int? skip, int? take, 
            bool returnCount = true, bool verbose = false)
        {
            return base.Where(predicate, user, skip, take, returnCount, verbose, q => q
            ////put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            //var query = _repo.FindByCondition(predicate)
                .Where(l => l.IsActive)
                .OrderBy(l => l.NamespaceUri)
                .ThenBy(l => l.DisplayName)
                );
            //var count = returnCount ? query.Count() : 0;
            ////query returns IincludableQuery. Jump through the following to find right combo of skip and take
            ////Goal is to have the query execute and not do in memory skip/take
            //IQueryable<LookupDataType> data;
            //if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            //else if (skip.HasValue) data = query.Skip(skip.Value);
            //else if (take.HasValue) data = query.Take(take.Value);
            //else data = query;

            //DALResult<LookupDataTypeModel> result = new DALResult<LookupDataTypeModel>();
            //result.Count = count;
            //result.Data = MapToModels(data.ToList(), verbose);
            //result.SummaryData = null;
            //return result;
        }

        public async Task<int?> Delete(int id, UserToken userToken)
        {
            EngineeringUnit entity = base.FindByCondition(userToken, x => x.ID == id).FirstOrDefault();
            //entity.Updated = DateTime.UtcNow;
            //entity.UpdatedBy = userId;
            entity.IsActive = false;

            await _repo.UpdateAsync(entity);
            await _repo.SaveChanges();
            return entity.ID;
        }


        protected override EngineeringUnitModel MapToModel(EngineeringUnit entity, bool verbose = true)
        {
            return MapToModelPublic(entity, verbose);
        }
        public EngineeringUnitModel MapToModelPublic(EngineeringUnit entity, bool verbose = true)
        {
            if (entity != null)
            {
                return new EngineeringUnitModel
                {
                    ID = entity.ID,
                    DisplayName = entity.DisplayName,
                    Description = entity.Description,
                    NamespaceUri = entity.NamespaceUri,
                    UnitId = entity.UnitId,
                };
            }
            else
            {
                return null;
            }

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