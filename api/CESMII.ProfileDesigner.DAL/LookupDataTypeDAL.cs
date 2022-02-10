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

    public class LookupDataTypeDAL : TenantBaseDAL<LookupDataType, LookupDataTypeModel>, IDal<LookupDataType, LookupDataTypeModel>
    {
        public LookupDataTypeDAL(IRepository<LookupDataType> repo, IServiceProvider serviceProvider) : base(repo)
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

        public override async Task<int?> Add(LookupDataTypeModel model, UserToken userToken)
        {
            LookupDataType entity = new LookupDataType
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
        public override LookupDataType CheckForExisting(LookupDataTypeModel model, UserToken userToken, bool cacheOnly = false)
        {
            //var entity = base.CheckForExisting(model, tenantId);
            //if (entity != null && (entity.OwnerId == null || entity.OwnerId == tenantId))
            //{
            //    return entity;
            //}
            var entity = base.FindByCondition(userToken, dt =>
                (
                  (model.ID != 0 && model.ID != null && dt.ID == model.ID)
                  || ( dt.Name == model.Name && dt.Code == model.Code)
                )
                /*&& (dt.OwnerId == null || dt.OwnerId == tenantId)*/, cacheOnly).FirstOrDefault();
            return entity;
        }

        public override async Task<int?> Update(LookupDataTypeModel model, UserToken userToken)
        {
            LookupDataType entity = _repo.FindByCondition(
                dt => 
                  (model.ID != 0 && model.ID != null && dt.ID == model.ID)
                  || (dt.Name == model.Name && dt.Code == model.Code)

                ).FirstOrDefault();
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
        public override LookupDataTypeModel GetById(int id, UserToken userToken)
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
        public override List<LookupDataTypeModel> GetAll(UserToken userToken, bool verbose = false)
        {
            DALResult<LookupDataTypeModel> result = GetAllPaged(userToken, verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override DALResult<LookupDataTypeModel> GetAllPaged(UserToken userToken, int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var result = base.Where(l => l.IsActive, userToken,skip, take, returnCount, verbose, q => q
                    .OrderBy(l => l.DisplayOrder)
                    .ThenBy(l => l.Name)
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

            //DALResult<LookupDataTypeModel> result = new DALResult<LookupDataTypeModel>();
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
        public override DALResult<LookupDataTypeModel> Where(Expression<Func<LookupDataType, bool>> predicate, UserToken user, int? skip, int? take, 
            bool returnCount = true, bool verbose = false)
        {
            return base.Where(predicate, user, skip, take, returnCount, verbose, q => q
            ////put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            //var query = _repo.FindByCondition(predicate)
                .Where(l => l.IsActive)
                .OrderBy(l => l.DisplayOrder)
                .ThenBy(l => l.Name)
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
            LookupDataType entity = base.FindByCondition(userToken, x => x.ID == id).FirstOrDefault();
            //entity.Updated = DateTime.UtcNow;
            //entity.UpdatedBy = userId;
            entity.IsActive = false;

            await _repo.UpdateAsync(entity);
            await _repo.SaveChanges();
            return entity.ID;
        }


        protected override LookupDataTypeModel MapToModel(LookupDataType entity, bool verbose = true)
        {
            if (entity != null)
            {
                return new LookupDataTypeModel
                {
                    ID = entity.ID,
                    Name = entity.Name,
                    Code = entity.Code,
                    IsNumeric = entity.IsNumeric,
                    DisplayOrder = entity.DisplayOrder,
                    UseMinMax = entity.UseMinMax,
                    UseEngUnit = entity.UseEngUnit, 
                    CustomTypeId = entity.CustomTypeId,
                    CustomType = entity.CustomType == null ? null :
                        _profileTypeDefinitionDAL.MapToModelPublic(entity.CustomType),
                    OwnerId = entity.OwnerId
                };
            }
            else
            {
                return null;
            }

        }

        public void MapToEntityPublic(ref LookupDataType entity, LookupDataTypeModel model, UserToken userToken)
        {
            MapToEntity(ref entity, model, userToken);
        }
        protected override void MapToEntity(ref LookupDataType entity, LookupDataTypeModel model, UserToken userToken)
        {
            entity.OwnerId = model.OwnerId;
            entity.Name = model.Name;
            entity.Code = model.Code;
            entity.DisplayOrder = model.DisplayOrder;
            entity.IsNumeric = model.IsNumeric;
            entity.UseMinMax = model.UseMinMax;
            entity.UseEngUnit = model.UseEngUnit;
            if (model.CustomTypeId != 0)
            {
                entity.CustomTypeId = model.CustomTypeId != 0 ? model.CustomTypeId : null;
            }
            if (model.CustomType != null)
            {
                var customTypeEntity = entity.CustomType;
                if (customTypeEntity == null)
                {
                    customTypeEntity = _profileTypeDefinitionDAL.CheckForExisting(model.CustomType, userToken);
                    if (customTypeEntity == null)
                    {
                        throw new NotImplementedException("Must add new profile type definitions for custom types explicitly");
                        //customTypeEntity = new ProfileTypeDefinition
                        //{
                        //    // TODO set createdby etc.
                        //};
                        //_profileTypeDefinitionDAL.MapToEntityPublic(ref customTypeEntity, model.CustomType, userToken);
                    }
                    entity.CustomType = customTypeEntity;
                }
            }

        }
    }
}