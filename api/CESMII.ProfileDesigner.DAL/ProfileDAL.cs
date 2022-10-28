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

    public class ProfileDAL : TenantBaseDAL<Profile, ProfileModel>, IDal<Profile, ProfileModel>
    {
        public ProfileDAL(IRepository<Profile> repo, IDal<NodeSetFile, NodeSetFileModel> nodeSetFileDAL, IDal<StandardNodeSet, StandardNodeSetModel> standardNodeSetDAL) : base(repo)
        {
            // TODO clean up the dependencies: expand interface?
            _nodeSetFileDAL = nodeSetFileDAL as NodeSetFileDAL;
            _standardNodeSetDAL = standardNodeSetDAL as StandardNodeSetDAL;
        }
        private readonly NodeSetFileDAL _nodeSetFileDAL;
        private readonly StandardNodeSetDAL _standardNodeSetDAL;

        public override async Task<int?> AddAsync(ProfileModel model, UserToken userToken)
        {
            var entity = new Profile
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

        public override async Task<int?> UpdateAsync(ProfileModel model, UserToken userToken)
        {
            Profile entity = base.FindByCondition(userToken, x => x.ID == model.ID && x.AuthorId == model.AuthorId)
                .FirstOrDefault();
            if (entity == null)
            {
                throw new ArgumentNullException("NodeSet not found during update or access was denied.");
            }
            this.MapToEntity(ref entity, model, userToken);

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();
            return entity.ID;
        }

        public override Profile CheckForExisting(ProfileModel model, UserToken userToken, bool cacheOnly = false)
        {
            var query = base.FindByCondition(userToken, x =>
               (
                 (model.ID != 0 && model.ID != null && x.ID == model.ID)
                 || (x.Namespace == model.Namespace && x.Version == model.Version)
               ) /*&& (x.AuthorId == null || x.AuthorId == tenantId)*/, cacheOnly);
            var profile = query.FirstOrDefault();
            return profile;
        }

        public override DALResult<ProfileModel> GetAllPaged(UserToken userToken, int? skip, int? take, bool returnCount = false, bool verbose = false)
        {
            var result = base.Where(p => true, userToken, skip, take, returnCount, verbose, q => q
                .OrderBy(x => x.Namespace).ThenByDescending(x => x.Version));
            return result;
        }

        public override DALResult<ProfileModel> Where(Expression<Func<Profile, bool>> predicate, UserToken user, int? skip = null, int? take = null, bool returnCount = false, bool verbose = false)
        {
            return base.Where(predicate, user, skip, take, returnCount, verbose, q => q
                .OrderBy(x => x.Namespace).ThenByDescending(x => x.Version)
                );
        }

        /// <summary>
        /// Deletes a record from the Profile Cache
        /// </summary>
        /// <param name="id">Id of the record to be deleted</param>
        /// <param name="userId">owner of the record. If set to -1 AuthorID is ignored (force delete)</param>
        /// <returns></returns>
        public async Task<int?> DeleteAsync(int id, UserToken userToken)
        {
            //TBD - delete needs to add some include statements to pull back related children.
            //do filter on author id so that the user can only delete their stuff
            Profile entity = base.FindByCondition(userToken, x => x.ID == id && (x.AuthorId == userToken.UserId || userToken.UserId == -1)).FirstOrDefault();
            if (entity == null)
                return 0;

            //complex delete with many cascading implications, call stored proc which deletes all dependent objects 
            // in proper order, etc.
            await _repo.ExecStoredProcedureAsync("call public.sp_nodeset_delete({0})", id.ToString());
            await _repo.SaveChangesAsync(); // SP does not get executed until SaveChanges
            return 1;
        }

        public override async Task<int> DeleteManyAsync(List<int> ids, UserToken userToken)
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


        public ProfileModel MapToModelPublic(Profile entity, bool verbose = false)
        {
            return MapToModel(entity, verbose);
        }
        protected override ProfileModel MapToModel(Profile entity, bool verbose = true)
        {
            if (entity != null)
            {
                var result = new ProfileModel()
                {
                    ID = entity.ID,
                    Namespace = entity.Namespace,
                    //TBD - may be able to do away with this field in model side.
                    StandardProfileID = entity.StandardProfileID,
                    StandardProfile = entity.StandardProfile == null ? null :
                        new StandardNodeSetModel()
                        {
                            ID = entity.StandardProfile.ID,
                            Filename = entity.StandardProfile.Filename,
                            Namespace = entity.StandardProfile.Namespace,
                            PublishDate = entity.StandardProfile.PublishDate,
                            Version = entity.StandardProfile.Version,
                            CloudLibraryId = entity.StandardProfile.CloudLibraryId,
                        },
                    AuthorId = entity.AuthorId,
                    Author = MapToModelSimpleUser(entity.Author),
                    NodeSetFiles = verbose ? entity.NodeSetFiles.Select(nsf => _nodeSetFileDAL.MapToModelPublic(nsf ,verbose)).ToList() : null, 
                    Version = entity.Version,
                    PublishDate = entity.PublishDate,
                };

                if (verbose)
                {
                    //pull list of warnings - only used for export scenario (uncommon so 
                    //we may consider not pulling these all the time. 
                    result.ImportWarnings = entity.ImportWarnings?.OrderBy(x => x.Message).Select(i =>
                        new ImportProfileWarningModel
                        {
                            Created = i.Created,
                            Message = i.Message,
                            ProfileId = i.ProfileId
                        }).ToList();
                }
                return result;
            }
            else
            {
                return null;
            }

        }

        public void MapToEntityPublic(ref Profile entity, ProfileModel model, UserToken userToken)
        {
            MapToEntity(ref entity, model, userToken);
        }
        protected override void MapToEntity(ref Profile entity, ProfileModel model, UserToken userToken)
        {
            entity.Namespace = model.Namespace;
            entity.StandardProfileID = model.StandardProfileID;

            if (model.StandardProfile != null)
            {
                var entityStandardProfile = 
                    entity.StandardProfile 
                    ?? _standardNodeSetDAL.CheckForExisting(model.StandardProfile, userToken) 
                    ?? new StandardNodeSet();
                _standardNodeSetDAL.MapToEntityPublic(ref entityStandardProfile, model.StandardProfile, userToken);
                entity.StandardProfile = entityStandardProfile;
            }
            entity.AuthorId = model.AuthorId;
            entity.Version = model.Version;
            entity.PublishDate = model.PublishDate;
            MapToEntityNodeSetFiles(ref entity, model.NodeSetFiles, userToken);
        }

        protected void MapToEntityNodeSetFiles(ref Profile entity, List<NodeSetFileModel> files, UserToken userToken)
        {
            //init interfaces obj for new scenario
            if (entity.NodeSetFiles == null) entity.NodeSetFiles = new List<NodeSetFile>();

            //this shouldn't happen...unless creating from within system. If all items removed, then it should be a collection w/ 0 items.
            if (files == null) return; 

            // Remove items no longer used
            // Use counter from end of collection so we can remove and not mess up loop iterator 
            if (entity.NodeSetFiles.Any())
            {
                var length = entity.NodeSetFiles.Count - 1;
                for (var i = length; i >= 0; i--)
                {
                    var current = entity.NodeSetFiles[i];

                    //remove if no longer present
                    var source = files.Find(v => v.ID.Equals(current.ID) || v.FileName == current.FileName);
                    if (source == null)
                    {
                        entity.NodeSetFiles.RemoveAt(i);
                    }
                    else
                    {
                        _nodeSetFileDAL.MapToEntityPublic(ref current, source, userToken);
                    }
                }
            }

            // Loop over interfaces passed in and only add those not already there
            foreach (var file in files)
            {
                if ((file.ID??0) == 0 || entity.NodeSetFiles.Find(x => x.ID.Equals(file.ID) || x.FileName == file.FileName) == null)
                {
                    var fileEntity = _nodeSetFileDAL.CheckForExisting(file, userToken);
                    if (fileEntity == null)
                    {
                        throw new ArgumentNullException($"NodeSetFile must be added explicitly");
                    }
                    _nodeSetFileDAL.MapToEntityPublic(ref fileEntity, file, userToken);
                    entity.NodeSetFiles.Add(fileEntity);
                }
            }
        }
    }
}