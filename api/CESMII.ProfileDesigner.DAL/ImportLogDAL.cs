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

    /// <summary>
    /// </summary>
    public class ImportLogDAL : TenantBaseDAL<ImportLog, ImportLogModel>, IDal<ImportLog, ImportLogModel>
    {
        public ImportLogDAL(IRepository<ImportLog> repo) : base(repo)
        {
        }

        public override async Task<int?> Add(ImportLogModel model, UserToken userToken)
        {
            ImportLog entity = new ImportLog
            {
                ID = null
                , Created = DateTime.UtcNow
                , Updated = DateTime.UtcNow
                //, OwnerId = tenantId
                //only update file list on add
                , FileList = model.FileList == null ? null : string.Join(",", model.FileList),
                  IsActive = true
            };
            model.Status = Common.Enums.TaskStatusEnum.InProgress; //set in model because the map to entity will assign val from model.

            //this.MapToEntity(ref entity, model, userToken);

            //this will maptoentity, add and call saveChanges
            await base.AddAsync(entity, model, userToken);
            model.ID = entity.ID;
            // Return id for newly added user
            return entity.ID;
        }

        public override async Task<int?> Update(ImportLogModel model, UserToken userToken)
        {
            ImportLog entity = base.FindByCondition(userToken, x => x.ID == model.ID)
                //.Include(l => l.Messages)
                //.Include(l => l.ProfileWarnings)
                .FirstOrDefault()
                ;
            if (entity == null) return null;
            //model.Updated = DateTime.UtcNow;
            this.MapToEntity(ref entity, model, userToken);
            entity.Updated = DateTime.UtcNow;

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
        public override ImportLogModel GetById(int id, UserToken userToken)
        {
            var entity = base.FindByCondition(userToken, x => x.ID == id)
                //.Include(l => l.Messages)
                .FirstOrDefault();
            return MapToModel(entity, true);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override List<ImportLogModel> GetAll(UserToken userToken, bool verbose = false)
        {
            DALResult<ImportLogModel> result = GetAllPaged(userToken, verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <returns></returns>
        public override DALResult<ImportLogModel> GetAllPaged(UserToken userToken, int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var result = base.Where(x => x.IsActive, userToken, skip, take, returnCount, verbose, q => q
                //.Include(l => l.Messages)
                .OrderByDescending(l => l.Completed)
                .ThenByDescending(l => l.StatusId)
                );
            return result;
            //var count = returnCount ? query.Count() : 0;
            ////query returns IincludableQuery. Jump through the following to find right combo of skip and take
            ////Goal is to have the query execute and not do in memory skip/take
            //IQueryable<ImportLog> data;
            //if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            //else if (skip.HasValue) data = query.Skip(skip.Value);
            //else if (take.HasValue) data = query.Take(take.Value);
            //else data = query;

            //DALResult<ImportLogModel> result = new DALResult<ImportLogModel>();
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
        public override DALResult<ImportLogModel> Where(Expression<Func<ImportLog, bool>> predicate, UserToken user, int? skip, int? take, 
            bool returnCount = true, bool verbose = false)
        {
            return base.Where(predicate, user, skip, take, returnCount, verbose, q => q
            ////put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            //var query = _repo.FindByCondition(predicate)
                .Where(x => x.IsActive)
                //.Include(l => l.Messages)
                .OrderByDescending(l => l.Completed)
                .ThenByDescending(l => l.StatusId)
                );
            //var count = returnCount ? query.Count() : 0;
            ////query returns IincludableQuery. Jump through the following to find right combo of skip and take
            ////Goal is to have the query execute and not do in memory skip/take
            //IQueryable<ImportLog> data;
            //if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            //else if (skip.HasValue) data = query.Skip(skip.Value);
            //else if (take.HasValue) data = query.Take(take.Value);
            //else data = query;

            //DALResult<ImportLogModel> result = new DALResult<ImportLogModel>();
            //result.Count = count;
            //result.Data = MapToModels(data.ToList(), verbose);
            //result.SummaryData = null;
            //return result;
        }

        public async Task<int?> Delete(int id, UserToken userToken)
        {
            ImportLog entity = base.FindByCondition(userToken, x => x.ID == id && x.OwnerId == userToken.UserId).FirstOrDefault();
            entity.IsActive = false;
            _repo.Update(entity);
            //await _repo.Delete(entity);
            await _repo.SaveChanges();
            return entity.ID;
        }


        protected override ImportLogModel MapToModel(ImportLog entity, bool verbose = false)
        {
            if (entity != null)
            {
                var result = new ImportLogModel
                {
                    ID = entity.ID,
                    FileList = string.IsNullOrEmpty(entity.FileList) ? null : entity.FileList.Split(","),
                    Status = (Common.Enums.TaskStatusEnum)entity.StatusId,
                    Completed = entity.Completed,
                    Created = entity.Created,
                    Updated = entity.Updated,
                    OwnerId = entity.OwnerId??0,
                    IsActive = entity.IsActive
                    //Owner = new UserSimpleModel
                    //{
                    //    ID = entity.Owner.ID,
                    //    FirstName = entity.Owner.FirstName,
                    //    LastName = entity.Owner.LastName,
                    //}
                };

                if (verbose)
                {
                    result.Messages = MapToModelMessages(entity);
                    result.ProfileWarnings = MapToModelProfileWarnings(entity);
                }
                return result;
            }
            else
            {
                return null;
            }

        }

        private List<ImportLogMessageModel> MapToModelMessages(ImportLog entity)
        {
            if (entity.Messages == null) return null;
            return entity.Messages.OrderByDescending(x => x.Created)
                .Select(msg => new ImportLogMessageModel
                    {
                        ID = msg.ID,
                        Message = msg.Message,
                        Created = msg.Created
                    }
                )
                .ToList();
        }

        private List<ImportProfileWarningModel> MapToModelProfileWarnings(ImportLog entity)
        {
            if (entity.ProfileWarnings == null) return null;
            return entity.ProfileWarnings.OrderByDescending(x => x.Created)
                .Select(msg => new ImportProfileWarningModel
                {
                    ID = msg.ID,
                    Message = msg.Message,
                    //ProfileId = msg.ProfileId,
                    Created = msg.Created
                }
                )
                .ToList();
        }

        protected override void MapToEntity(ref ImportLog entity, ImportLogModel model, UserToken userToken)
        {
            //only update file list, owner, created on add
            //entity.Name = model.Name;
            entity.StatusId = (int)model.Status;
            if (model.Completed.HasValue)
            {
                entity.Completed = model.Completed;
            }
            MapToEntityMessages(ref entity, model.Messages, userToken);
            MapToEntityProfileWarnings(ref entity, model.ProfileWarnings, userToken);
        }

        protected void MapToEntityMessages(ref ImportLog entity, List<Models.ImportLogMessageModel> messages, UserToken userToken)
        {
            //init visit services for new scenario
            if (entity.Messages == null) entity.Messages = new List<ImportLogMessage>();

            // Remove attribs no longer used
            // Use counter from end of collection so we can remove and not mess up loop iterator 
            if (entity.Messages.Count > 0)
            {
                var length = entity.Messages.Count - 1;
                for (var i = length; i >= 0; i--)
                {
                    var currentId = entity.Messages[i].ID;

                    //remove if no longer present - shouldn't happen with import messages
                    var source = messages?.Find(x => x.ID.Equals(currentId));
                    if (source == null)
                    {
                        entity.Messages.RemoveAt(i);
                    }
                    else
                    {
                        //do nothing
                    }
                }
            }

            // Loop over messages passed in and only add those not already there
            if (messages != null)
            {
                foreach (var msg in messages)
                {
                    if ((msg.ID ?? 0) == 0 || entity.Messages.Find(up => up.ID.Equals(msg.ID)) == null)
                    {
                        entity.Messages.Add(new ImportLogMessage
                        {
                            Message = msg.Message,
                            Created = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        protected void MapToEntityProfileWarnings(ref ImportLog entity, List<Models.ImportProfileWarningModel> warnings, UserToken userToken)
        {
            //init for new scenario
            if (entity.ProfileWarnings == null) entity.ProfileWarnings = new List<ImportProfileWarning>();

            // Remove attribs no longer used
            // Use counter from end of collection so we can remove and not mess up loop iterator 
            if (entity.ProfileWarnings.Count > 0)
            {
                var length = entity.ProfileWarnings.Count - 1;
                for (var i = length; i >= 0; i--)
                {
                    var currentId = entity.ProfileWarnings[i].ID;

                    //remove if no longer present - shouldn't happen with import warnings
                    var source = warnings?.Find(x => x.ID.Equals(currentId));
                    if (source == null)
                    {
                        entity.ProfileWarnings.RemoveAt(i);
                    }
                    else
                    {
                        //do nothing
                    }
                }
            }

            // Loop over warnings passed in and only add those not already there
            if (warnings != null)
            {
                foreach (var msg in warnings)
                {
                    if ((msg.ID ?? 0) == 0 || entity.ProfileWarnings.Find(up => up.ID.Equals(msg.ID)) == null)
                    {
                        entity.ProfileWarnings.Add(new ImportProfileWarning
                        {
                            Message = msg.Message,
                            ProfileId = msg.ProfileId,
                            Created = DateTime.UtcNow
                        });
                    }
                }
            }
        }
    }
}