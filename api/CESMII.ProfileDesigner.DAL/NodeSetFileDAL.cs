using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.Data.Repositories;

namespace CESMII.ProfileDesigner.DAL
{
    public class NodeSetFileDAL : TenantBaseDAL<NodeSetFile, NodeSetFileModel>, IDal<NodeSetFile, NodeSetFileModel>
    {
        public NodeSetFileDAL(IRepository<NodeSetFile> repo) : base(repo)
        {
        }

        public override async Task<int?> Add(NodeSetFileModel model, UserToken userToken)
        {
            NodeSetFile entity = new NodeSetFile
            {
                ID = null
            };

            this.MapToEntity(ref entity, model, userToken);

            //this will add and call saveChanges
            await base.AddAsync(entity, model, userToken);

            model.ID = entity.ID;
            // Return id for newly added user
            return entity.ID;
        }

        public override async Task<int?> Update(NodeSetFileModel model, UserToken userToken)
        {
            NodeSetFile entity = base.FindByCondition(userToken, x => x.ID == model.ID && x.AuthorId == model.AuthorId).FirstOrDefault();
            if (entity == null)
            {
                throw new Exception("NodeSet not found during update or access was denied.");
            }
            this.MapToEntity(ref entity, model, userToken);

            await _repo.UpdateAsync(entity);
            await _repo.SaveChanges();
            return entity.ID;
        }

        public override NodeSetFile CheckForExisting(NodeSetFileModel model, UserToken userToken, bool cacheOnly = false)
        {
            //var entity = base.CheckForExisting(model, tenantId);
            //if (entity != null && ((model.AuthorId==null && entity.AuthorId==null) || entity.AuthorId == model.AuthorId))
            //{
            //    return entity;
            //}
            return base.FindByCondition(userToken, x => 
                ( 
                  (model.ID != 0 && model.ID != null && x.ID == model.ID)
                  || x.FileName == model.FileName
                ) 
                /*&& (model.AuthorId == null || x.AuthorId == model.AuthorId)*/, cacheOnly).FirstOrDefault();
        }

        public override DALResult<NodeSetFileModel> GetAllPaged(UserToken userToken, int? skip, int? take, bool returnCount = false, bool verbose = false)
        {
            var result = base.Where(f => true, userToken, skip, take, returnCount, verbose, q => q
                .OrderBy(x => x.FileName).ThenByDescending(x => x.PublicationDate));
            return result;
            //var count = returnCount ? query.Count() : 0;
            ////query returns IincludableQuery. Jump through the following to find right combo of skip and take
            ////Goal is to have the query execute and not do in memory skip/take
            //IQueryable<NodeSetFile> data;
            //if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            //else if (skip.HasValue) data = query.Skip(skip.Value);
            //else if (take.HasValue) data = query.Take(take.Value);
            //else data = query;

            //DALResult<NodeSetFileModel> result = new DALResult<NodeSetFileModel>();
            //result.Count = count;
            //result.Data = MapToModels(data.ToList(), verbose);
            //result.SummaryData = null;
            //return result;
        }

        public override DALResult<NodeSetFileModel> Where(Expression<Func<NodeSetFile, bool>> predicate, UserToken user, int? skip, int? take, bool returnCount = false, bool verbose = false)
        {
            return base.Where(predicate, user, skip, take, returnCount, verbose, q => q
            //var query = _repo.FindByCondition(predicate)
                .OrderBy(x => x.FileName).ThenByDescending(x => x.PublicationDate)
                );
            //var count = returnCount ? query.Count() : 0;
            ////query returns IincludableQuery. Jump through the following to find right combo of skip and take
            ////Goal is to have the query execute and not do in memory skip/take
            //IQueryable<NodeSetFile> data;
            //if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            //else if (skip.HasValue) data = query.Skip(skip.Value);
            //else if (take.HasValue) data = query.Take(take.Value);
            //else data = query;

            //DALResult<NodeSetFileModel> result = new DALResult<NodeSetFileModel>();
            //result.Count = count;
            //result.Data = MapToModels(data.ToList(), verbose);
            //result.SummaryData = null;
            //return result;
        }

        //public override NodeSetFileModel GetByFunc(Expression<Func<NodeSetFile, bool>> predicate, bool verbose)
        //{
        //    var tRes = _repo.FindByCondition(predicate).OrderByDescending(s => s.PublicationDate).FirstOrDefault();
        //    return MapToModel(tRes, verbose);
        //}

        /// <summary>
        /// Deletes a record from the NodeSetFile Cache
        /// </summary>
        /// <param name="id">Id of the record to be deleted</param>
        /// <param name="userId">owner of the record. If set to -1 AuthorID is ignored (force delete)</param>
        /// <returns></returns>
        public Task<int?> Delete(int id, UserToken userToken)
        {
            //TBD - delete needs to add some include statements to pull back related children.
            //do filter on author id so that the user can only delete their stuff
            NodeSetFile entity = base.FindByCondition(userToken, x => x.ID == id && (x.AuthorId == userToken.UserId || userToken.UserId == -1)).FirstOrDefault();
            if (entity == null)
                return Task.FromResult<int?>(0);

            //complex delete with many cascading implications, call stored proc which deletes all dependent objects 
            // in proper order, etc.
            //TODO: @Sean: If a nodeset is deleted from the cache table, some tables with references to the cache table might be broken.
            //await _repo.ExecStoredProcedureAsync("call public.sp_nodeset_delete({0})", id.ToString());
            return Task.FromResult<int?>(1);
        }

        public override async Task<int> DeleteMany(List<int> ids, UserToken userToken)
        {
            //find matches in the db regardless of author. Note there could be a scenario where they pass in an id that
            //isn't there anymore which is why we check this way.
            var matchesCount = base.FindByCondition(userToken, x => ids.Contains(x.ID??0)).Count();
            var matchesWAuthor = base.FindByCondition(userToken, x => ids.Contains(x.ID??0) && x.AuthorId == userToken.UserId).ToList();

            //then filter on author. If the number of matches > number of matches with author filter, then we 
            //return 0 because they are not permitted to delete a nodeset they don't own.
            if (matchesCount > matchesWAuthor.Count)
                throw new InvalidOperationException("User is not permitted to delete one or many of these nodesets");

            //only delete items where this user is the author - regardless of their original list
            var idsString = string.Join(",", matchesWAuthor.Select(x => x.ID).ToList());
            await _repo.ExecStoredProcedureAsync("call public.sp_nodeset_delete({0})", idsString);
            return 1;
        }

        public NodeSetFileModel MapToModelPublic(NodeSetFile entity, bool verbose = false)
        {
            return MapToModel(entity, verbose);
        }
        protected override NodeSetFileModel MapToModel(NodeSetFile entity, bool verbose = false)
        {
            if (entity != null)
            {
                return new NodeSetFileModel
                {
                    ID = entity.ID,
                    FileName = entity.FileName,
                    AuthorId = entity.AuthorId,
                    FileCache = verbose ? entity.FileCache : null,
                    Version = entity.Version,
                    PublicationDate = entity.PublicationDate,
                };
            }
            else
            {
                return null;
            }

        }

        public void MapToEntityPublic(ref NodeSetFile entity, NodeSetFileModel model, UserToken userToken)
        {
            MapToEntity(ref entity, model, userToken);
        }
        protected override void MapToEntity(ref NodeSetFile entity, NodeSetFileModel model, UserToken userToken)
        {
            entity.FileName = model.FileName;
            entity.FileCache = model.FileCache;
            entity.AuthorId = model.AuthorId;
            entity.Version = model.Version;
            entity.PublicationDate = model.PublicationDate;
        }

    }
}