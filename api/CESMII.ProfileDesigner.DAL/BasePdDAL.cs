﻿namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using CESMII.ProfileDesigner.Data.Entities;
    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Data.Repositories;

    public class BasePdDAL<TEntity, TModel> : BaseDAL<TEntity, TModel>, IDal<TEntity, TModel> where TEntity : ProfileDesignerAbstractEntity, new() where TModel : AbstractProfileDesignerModel
    {
        public BasePdDAL(IRepository<TEntity> repo) : base(repo)
        {
        }

        protected override Task<int?> AddAsync(TEntity entity, TModel model, UserToken userToken)
        {
            entity.CreatedById = userToken.UserId;
            entity.UpdatedById = userToken.UserId;
            entity.Created = DateTime.UtcNow;
            entity.Updated = DateTime.UtcNow;
            model.IsActive = true;

            return base.AddAsync(entity, model, userToken);
        }

        //public virtual int AddSync(TModel model, int userId)
        //{
        //    //implement in derived class
        //    throw new NotImplementedException();
        //}

        //protected virtual int Add(TEntity entity, TModel model, int userId)
        //{
        //    entity.ID = 0;
        //    entity.CreatedById = userId;
        //    entity.UpdatedById = userId;
        //    entity.Created = DateTime.UtcNow;
        //    entity.Updated = DateTime.UtcNow;
        //    model.IsActive = true;
        //    this.MapToEntity(ref entity, model);

        //    //this will add and call saveChanges
        //    _repo.Add(entity);

        //    // TODO: Have repo return Id of newly created entity
        //    return entity.ID;
        //}


        public override async Task<int?> Update(TModel model, UserToken userToken)
        {
            TEntity entity = base.GetAllEntities(userToken)
                    .Where(e => e.ID == model.ID).FirstOrDefault();
            entity.UpdatedById = userToken.UserId;
            entity.Updated = DateTime.UtcNow;
            this.MapToEntity(ref entity, model, userToken);

            await _repo.UpdateAsync(entity);
            return entity.ID;
        }

        //public virtual int UpdateSync(TModel model, int userId)
        //{
        //    TEntity entity = _repo.GetAll()
        //            .Where(e => e.ID == model.ID).FirstOrDefault();
        //    entity.UpdatedById = userId;
        //    entity.Updated = DateTime.UtcNow;
        //    this.MapToEntity(ref entity, model);

        //    _repo.Update(entity);
        //    return entity.ID;
        //}

        public virtual async Task<int?> Delete(int id, UserToken userToken)
        {
            var entity = _repo.FindByCondition(e => e.ID == id)
                .FirstOrDefault();
            if (entity == null) return -1;

            //soft delete
            //TBD - add in updated by fields, so we can update those
            entity.Updated = DateTime.UtcNow;
            entity.UpdatedById = userToken.UserId;
            entity.IsActive = false;

            //TBD - do we also de-activate users from this Profile. 
            await _repo.UpdateAsync(entity);
            return await _repo.SaveChanges();
        }

    }
}