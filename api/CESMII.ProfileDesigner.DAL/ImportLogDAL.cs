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

        public override async Task<int?> AddAsync(ImportLogModel model, UserToken userToken)
        {
            var entity = new ImportLog
            {
                ID = null
                , Created = DateTime.UtcNow
                , Updated = DateTime.UtcNow
                //, OwnerId = tenantId
                //only update file list on add
                , IsActive = true
            };
            model.Status = Common.Enums.TaskStatusEnum.InProgress; //set in model because the map to entity will assign val from model.

            //this will maptoentity, add and call saveChanges
            await base.AddAsync(entity, model, userToken);
            model.ID = entity.ID;
            // Return id for newly added user
            return entity.ID;
        }

        public override async Task<int?> UpdateAsync(ImportLogModel model, UserToken userToken)
        {
            ImportLog entity = base.FindByCondition(userToken, x => x.ID == model.ID)
                .FirstOrDefault()
                ;
            if (entity == null) return null;
            this.MapToEntity(ref entity, model, userToken);
            entity.Updated = DateTime.UtcNow;

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
            DALResult<ImportLogModel> result = GetAllPaged(userToken,null, null, verbose: verbose);
            return result.Data;
        }

        /// <summary>
        /// Get all lookup items (with paging)
        /// </summary>
        /// <returns></returns>
        public override DALResult<ImportLogModel> GetAllPaged(UserToken userToken, int? skip, int? take, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var result = base.Where(x => x.IsActive, userToken, skip, take, returnCount, verbose, q => q
                .OrderByDescending(l => l.Completed)
                .ThenByDescending(l => l.StatusId)
                );
            return result;
        }

        /// <summary>
        /// This should be used when getting all sites and the calling code should pass in the where clause.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<ImportLogModel> Where(Expression<Func<ImportLog, bool>> predicate, UserToken user, int? skip = null, int? take = null, 
            bool returnCount = false, bool verbose = false)
        {
            return base.Where(predicate, user, skip, take, returnCount, verbose, q => q
                //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
                .Where(x => x.IsActive)
                .OrderByDescending(l => l.Completed)
                .ThenByDescending(l => l.StatusId)
                );
        }

        public async Task<int?> DeleteAsync(int id, UserToken userToken)
        {
            ImportLog entity = base.FindByCondition(userToken, x => x.ID == id && x.OwnerId == userToken.UserId).FirstOrDefault();
            entity.IsActive = false;
            //clean out file chunks when we delete this item. This has a lot of data and it is no longer needed once the import concludes.
            foreach (var f in entity.Files)
            {
                f.Chunks.Clear();
            }
            _repo.Update(entity);
            await _repo.SaveChangesAsync();
            return entity.ID;
        }

        /// <summary>
        /// Delete many import log items. Intended to clean up import log of old messages. Note this is a soft delete. 
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="userToken"></param>
        /// <returns></returns>
        public override async Task<int> DeleteManyAsync(List<int> ids, UserToken userToken)
        {
            var items = base.FindByCondition(userToken, x => ids.Contains(x.ID ?? 0));
            if (!items.Any()) return 0;

            try
            {
                _repo.StartTransaction();
                foreach (var entity in items)
                {
                    entity.IsActive = false;
                    await _repo.UpdateAsync(entity);
                }
                await _repo.CommitTransactionAsync();
                return items.Count();
            }
            catch (Exception)
            {
                _repo.RollbackTransaction();
                throw;
            }
        }

        protected override ImportLogModel MapToModel(ImportLog entity, bool verbose = true)
        {
            if (entity != null)
            {
                var result = new ImportLogModel
                {
                    ID = entity.ID,
                    Status = (Common.Enums.TaskStatusEnum)entity.StatusId,
                    Completed = entity.Completed,
                    Created = entity.Created,
                    Updated = entity.Updated,
                    OwnerId = entity.OwnerId??0,
                    IsActive = entity.IsActive
                };

                if (verbose)
                {
                    result.Messages = MapToModelMessages(entity);
                    result.ProfileWarnings = MapToModelProfileWarnings(entity);
                    result.Files = MapToModelFiles(entity, verbose);
                }
                return result;
            }
            else
            {
                return null;
            }

        }

        private static List<ImportLogMessageModel> MapToModelMessages(ImportLog entity)
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

        private static List<ImportProfileWarningModel> MapToModelProfileWarnings(ImportLog entity)
        {
            if (entity.ProfileWarnings == null) return null;
            return entity.ProfileWarnings.OrderByDescending(x => x.Created)
                .Select(msg => new ImportProfileWarningModel
                {
                    ID = msg.ID,
                    Message = msg.Message,
                    Created = msg.Created
                }
                )
                .ToList();
        }

        private static List<ImportFileModel> MapToModelFiles(ImportLog entity, bool verbose)
        {
            if (entity.Files == null) return new List<ImportFileModel>();
            return entity.Files.OrderBy(x => x.FileName)
                .Select(file => new ImportFileModel
                {
                    ID = file.ID,
                    ImportActionId = file.ImportActionId,
                    FileName = file.FileName,
                    TotalBytes = file.TotalBytes,
                    TotalChunks = file.TotalChunks,
                    Chunks = verbose ? MapToModelFileChunks(file) : null
                }
                )
                .ToList();
        }

        private static List<ImportFileChunkModel> MapToModelFileChunks(ImportFile file)
        {
            if (file.Chunks == null) return new List<ImportFileChunkModel>();
            return file.Chunks.OrderBy(x => x.ChunkOrder)
                .Select(chunk => new ImportFileChunkModel
                {
                    ID = chunk.ID,
                    ImportFileId = chunk.ImportFileId,
                    ChunkOrder = chunk.ChunkOrder,
                    Contents = chunk.Contents
                }
                )
                .ToList();
        }

        protected override void MapToEntity(ref ImportLog entity, ImportLogModel model, UserToken userToken)
        {
            //only update file list, owner, created on add
            entity.StatusId = (int)model.Status;
            if (model.Completed.HasValue)
            {
                entity.Completed = model.Completed;
            }
            MapToEntityMessages(ref entity, model.Messages);
            MapToEntityProfileWarnings(ref entity, model.ProfileWarnings);
            MapToEntityFiles(ref entity, model.Files);
        }

        protected static void MapToEntityMessages(ref ImportLog entity, List<Models.ImportLogMessageModel> messages)
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

        protected static void MapToEntityProfileWarnings(ref ImportLog entity, List<Models.ImportProfileWarningModel> warnings)
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

        protected static void MapToEntityFiles(ref ImportLog entity, List<Models.ImportFileModel> files)
        {
            //init for new scenario
            if (entity.Files == null) entity.Files = new List<ImportFile>();

            // Remove attribs no longer used
            // Use counter from end of collection so we can remove and not mess up loop iterator 
            if (entity.Files.Count > 0)
            {
                var length = entity.Files.Count - 1;
                for (var i = length; i >= 0; i--)
                {
                    var currentId = entity.Files[i].ID;

                    //remove if no longer present - shouldn't happen with import messages
                    var source = files?.Find(x => x.ID.Equals(currentId));
                    if (source == null)
                    {
                        entity.Files.RemoveAt(i);
                    }
                    else
                    {
                        entity.Files[i].FileName = source.FileName;
                        entity.Files[i].TotalBytes = source.TotalBytes;
                        entity.Files[i].TotalChunks = source.TotalChunks;
                        var itemExisting = entity.Files[i];
                        MapToEntityFileChunks(ref itemExisting, source.Chunks);
                    }
                }
            }

            // Loop over items passed in and only add those not already there
            if (files != null)
            {
                foreach (var file in files)
                {
                    if ((file.ID ?? 0) == 0 || entity.Files.Find(up => up.ID.Equals(file.ID)) == null)
                    {
                        var fileNew = new ImportFile
                        {
                            ImportActionId = entity.ID.HasValue ? entity.ID.Value : 0,
                            FileName = file.FileName,
                            TotalBytes = file.TotalBytes,
                            TotalChunks = file.TotalChunks
                        };
                        MapToEntityFileChunks(ref fileNew, file.Chunks); 
                        entity.Files.Add(fileNew);
                    }
                }
            }
        }

        protected static void MapToEntityFileChunks(ref ImportFile entity, List<Models.ImportFileChunkModel> chunks)
        {
            //init for new scenario
            if (entity.Chunks == null) entity.Chunks = new List<ImportFileChunk>();

            // Remove attribs no longer used
            // Use counter from end of collection so we can remove and not mess up loop iterator 
            if (entity.Chunks.Count > 0)
            {
                var length = entity.Chunks.Count - 1;
                for (var i = length; i >= 0; i--)
                {
                    var currentId = entity.Chunks[i].ID;

                    //remove if no longer present - shouldn't happen with import messages
                    var source = chunks?.Find(x => x.ID.Equals(currentId));
                    if (source == null)
                    {
                        entity.Chunks.RemoveAt(i);
                    }
                    else
                    {
                        entity.Chunks[i].ChunkOrder = source.ChunkOrder;
                        entity.Chunks[i].Contents = source.Contents;
                    }
                }
            }

            // Loop over items passed in and only add those not already there
            if (chunks != null)
            {
                foreach (var file in chunks)
                {
                    if ((file.ID ?? 0) == 0 || entity.Chunks.Find(up => up.ID.Equals(file.ID)) == null)
                    {
                        entity.Chunks.Add(new ImportFileChunk
                        {
                            ImportFileId = entity.ID.HasValue ? entity.ID.Value : 0,
                            ChunkOrder = file.ChunkOrder,
                            Contents = file.Contents
                        });
                    }
                }
            }
        }
    }
}