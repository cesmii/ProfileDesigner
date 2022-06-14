namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Data.Entities;
    using CESMII.ProfileDesigner.Data.Repositories;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using CESMII.ProfileDesigner.Common.Enums;

    public class ProfileTypeDefinitionDAL : TenantBasePdDAL<ProfileTypeDefinition, ProfileTypeDefinitionModel>
    {
        public ProfileTypeDefinitionDAL(IRepository<ProfileTypeDefinition> repo, IDal<Profile, ProfileModel> profileDAL, IDal<LookupDataType, LookupDataTypeModel> dataTypeDAL, IDal<EngineeringUnit, EngineeringUnitModel> euDAL, /*IDal<LookupItem, LookupItemModel> lookupDAL,*/ ILogger<ProfileTypeDefinitionDAL> diLogger) : base(repo)
        {
            _profileDAL = profileDAL as ProfileDAL;
            _dataTypeDAL = dataTypeDAL as LookupDataTypeDAL;
            _euDAL = euDAL as EngineeringUnitDAL;
            this._diLogger = diLogger;
        }
        private readonly ProfileDAL _profileDAL;
        private readonly LookupDataTypeDAL _dataTypeDAL;
        private readonly EngineeringUnitDAL _euDAL;
        private readonly ILogger<ProfileTypeDefinitionDAL> _diLogger;


        //add this layer so we can instantiate the new entity here.
        public override async Task<int?> Add(ProfileTypeDefinitionModel model, UserToken userToken)
        {
            var entity = new ProfileTypeDefinition();
            model.ID = await base.AddAsync(entity, model, userToken);
            return model.ID;
        }

        /// <summary>
        /// Get one
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override ProfileTypeDefinitionModel GetById(int id, UserToken userToken)
        {
            var entity = base.FindByCondition(userToken, x => x.ID == id)
                .Include(p => p.ProfileType)
                .FirstOrDefault();

            return MapToModel(entity, true);
        }

        /// <summary>
        /// Get all 
        /// </summary>
        /// <returns></returns>
        public override DALResult<ProfileTypeDefinitionModel> GetAllPaged(UserToken userToken, int? skip, int? take, bool returnCount = false, bool verbose = false)
        {
            //put the order by and where clause before skip.take so we skip/take on filtered/ordered query 
            var query = base.GetAllEntities(userToken)
                .OrderBy(p => p.Name);
            var count = returnCount ? query.Count() : 0;
            //query returns IincludableQuery. Jump through the following to find right combo of skip and take
            //Goal is to have the query execute and not do in memory skip/take
            IQueryable<ProfileTypeDefinition> data;
            if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            else if (skip.HasValue) data = query.Skip(skip.Value);
            else if (take.HasValue) data = query.Take(take.Value);
            else data = query;

            var result = new DALResult<ProfileTypeDefinitionModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;
        }

        /// <summary>
        /// Get all with a flexible where clause.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<ProfileTypeDefinitionModel> Where(Expression<Func<ProfileTypeDefinition, bool>> predicate, UserToken user, int? skip, int? take,
            bool returnCount = false, bool verbose = false)
        {
            return base.Where(predicate, user, skip, take, returnCount, verbose, q => q
                .OrderBy(p => p.Name)
                .Include(p => p.ProfileType)
            );
        }

        /// <summary>
        /// Get all with a list of where clauses.
        /// Calling code can build up any sequence of complex where clauses that will be put together here. 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<ProfileTypeDefinitionModel> Where(List<Expression<Func<ProfileTypeDefinition, bool>>> predicates,
            UserToken user, int? skip = null, int? take = null, bool returnCount = false, bool verbose = false,
            params OrderByExpression<ProfileTypeDefinition>[] orderByExpressions)
        {
            if (predicates == null) predicates = new List<Expression<Func<ProfileTypeDefinition, bool>>>();

            //build up a query and append n predicates
            var query = _repo.GetAll().AsQueryable<ProfileTypeDefinition>();
            foreach (var p in predicates)
            {
                query = query.Where(p).AsQueryable<ProfileTypeDefinition>();
            }
            //FIX - add filter out by user
            query = query.Where(x => x.OwnerId == null || x.OwnerId == user.UserId).AsQueryable<ProfileTypeDefinition>();

            //append order bys
            if (orderByExpressions == null)
            {
                query = query.OrderBy(p => p.Name);
            }
            else
            {
                //append order by
                ApplyOrderByExpressions(ref query, orderByExpressions);
            }

            //add include
            query = query
                .Include(p => p.ProfileType);

            var count = returnCount ? query.Count() : 0;

            //query returns IincludableQuery. Jump through the following to find right combo of skip and take
            //Goal is to have the query execute and not do in memory skip/take
            IQueryable<ProfileTypeDefinition> data;
            if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            else if (skip.HasValue) data = query.Skip(skip.Value);
            else if (take.HasValue) data = query.Take(take.Value);
            else data = query;

            //put together the result
            var result = new DALResult<ProfileTypeDefinitionModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;
        }

        /// <summary>
        /// Update profile and associated data
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public override async Task<int?> Update(ProfileTypeDefinitionModel model, UserToken userToken)
        {
            Expression<Func<ProfileTypeDefinition, bool>> filterExpression = GetIdentityExpression(model);

            var entity = base.FindByCondition(userToken, filterExpression)
                .Include(p => p.Interfaces)
                .Include(p => p.Compositions)
                .Include(p => p.Attributes).ThenInclude(a => a.DataType)
                .FirstOrDefault();
            entity.UpdatedById = userToken.UserId;
            entity.Updated = DateTime.UtcNow;

            this.MapToEntity(ref entity, model, userToken);

            await _repo.UpdateAsync(entity);

            return entity.ID;
        }

        private static Expression<Func<ProfileTypeDefinition, bool>> GetIdentityExpression(ProfileTypeDefinitionModel model)
        {
            Expression<Func<ProfileTypeDefinition, bool>> filterExpression;
            if ((model.ID ?? 0) != 0)
            {
                filterExpression = pi => pi.ID == model.ID;
            }
            else
            {
                filterExpression = pi => pi.OpcNodeId == model.OpcNodeId && pi.Profile.Namespace == model.Profile.Namespace;
            }

            return filterExpression;
        }
        private static bool MatchIdentity(int? entityId, string entityOpcNodeId, string entityNamespace, int? modelId, string modelOpcNodeId, string modelNamespace)
        {
            if ((modelId ?? 0) != 0 && (entityId ?? 0) != 0)
            {
                return modelId == entityId;
            }
            else
            {
                return modelOpcNodeId == entityOpcNodeId && modelNamespace == entityNamespace;
            }
        }
        private static bool MatchIdentity(ProfileTypeDefinition entity, ProfileTypeDefinitionModel model)
        {
            return MatchIdentity(entity.ID, entity.OpcNodeId, entity.Profile.Namespace, model.ID, model.OpcNodeId, model.Profile.Namespace);
        }
        private static bool MatchIdentity(ProfileComposition entity, ProfileTypeDefinitionRelatedModel model)
        {
            // Compositions don't have a nodeid, matching happens on BrowseName
            return MatchIdentity(entity.ID, entity.BrowseName, null, model.ID, model.BrowseName, null);
        }
        private static bool MatchIdentity(ProfileAttribute entity, ProfileAttributeModel model)
        {
            return MatchIdentity(entity.ID, entity.OpcNodeId, entity.Namespace, model.ID, model.OpcNodeId, model.Namespace)
                && ((model.ID ?? 0) != 0 || (entity.Name == model.Name && entity.BrowseName == model.BrowseName));
        }

        public override ProfileTypeDefinition CheckForExisting(ProfileTypeDefinitionModel model, UserToken userToken, bool cacheOnly = false)
        {
            var existingProfile = base.FindByCondition(userToken,
                GetIdentityExpression(model),
                cacheOnly).FirstOrDefault();
            return existingProfile;
        }

        public override async Task<int?> Delete(int id, UserToken userToken)
        {
            //can only delete your own stuff
            var entity = base.FindByCondition(userToken, x => x.ID == id && x.OwnerId.Equals(userToken.UserId))
                .Include(p => p.Interfaces)
                .Include(p => p.Compositions)
                .Include(p => p.Attributes)
                .FirstOrDefault();
            if (entity == null) return -1;

            //hard delete
            //Note this deletes the dependent related data also
            return await _repo.Delete(entity);
        }


        #region Map Entity to MODEL
        public ProfileTypeDefinitionModel MapToModelPublic(ProfileTypeDefinition entity, bool verbose = false)
        {
            return MapToModel(entity, verbose);
        }
        protected override ProfileTypeDefinitionModel MapToModel(ProfileTypeDefinition entity, bool verbose = true)
        {
            if (entity != null)
            {
                var result = new ProfileTypeDefinitionModel
                {
                    ID = entity.ID,
                    OpcNodeId = entity.OpcNodeId,
                    Name = entity.Name,
                    ProfileId = entity.ProfileId != 0 ? entity.ProfileId : null,
                    Profile = MapToModelProfile(entity.Profile),
                    BrowseName = entity.BrowseName,
                    SymbolicName = entity.SymbolicName,
                    Description = entity.Description,
                    TypeId = entity.ProfileTypeId,
                    Type = entity.ProfileType != null ?
                        new LookupItemModel { ID = entity.ProfileType.ID, Name = entity.ProfileType.Name, TypeId = entity.ProfileType.ID }
                        : null,
                    AuthorId = entity.AuthorId ?? null,
                    Author = MapToModelAuthor(entity),
                    ExternalAuthor = entity.ExternalAuthor,
                    DocumentUrl = entity.DocumentUrl,
                    IsAbstract = entity.IsAbstract,
                    Created = entity.Created,
                    Updated = entity.Updated,
                    MetaTags = string.IsNullOrEmpty(entity.MetaTags) ? new List<string>() : JsonConvert.DeserializeObject<List<MetaTag>>(entity.MetaTags).Select(s => s.Name.Trim()).ToList(),
                    MetaTagsConcatenated = string.IsNullOrEmpty(entity.MetaTags) ? "" : string.Join(", ", Enumerable.ToArray(JsonConvert.DeserializeObject<List<MetaTag>>(entity.MetaTags).Select(s => s.Name.Trim()))),
                    IsActive = entity.IsActive,
                    IsFavorite = entity.Favorite != null,
                    //calculated value which gives more emphasis on extending an item
                    PopularityIndex = MapToModelPopularityIndex(entity)

                };

                //when getting a list of profiles, we need not get all of this stuff
                //only get this when getting individual profiles
                if (verbose)
                {
                    result.Parent = MapToModelProfileTypDefSimple(entity.Parent);
                    result.InstanceParent = MapToModel(entity.InstanceParent, false);
                    result.Attributes = MapToModelAttributes(entity);
                    result.Interfaces = MapToModelInterfaces(entity.Interfaces);
                    result.Compositions = MapToModelCompositions(entity.Compositions, result);
                    result.UpdatedBy = entity.UpdatedBy == null ? null : new UserSimpleModel { ID = entity.UpdatedBy.ID, FirstName = entity.UpdatedBy.FirstName, LastName = entity.UpdatedBy.LastName };
                    result.CreatedBy = entity.CreatedBy == null ? null : new UserSimpleModel { ID = entity.CreatedBy.ID, FirstName = entity.CreatedBy.FirstName, LastName = entity.CreatedBy.LastName };
                }
                return result;
            }
            else
            {
                return null;
            }

        }

        protected static int MapToModelPopularityIndex(ProfileTypeDefinition entity)
        {
            if (entity != null)
            {
                //10 pts for favorite - specific to each user so count does not cross boundaries
                //3 pts for each extend usage - system wide
                //1 pt for each page visit count
                var result = entity.Favorite != null && entity.Favorite.IsFavorite ? 10 : 0;
                result += entity.Analytics == null ? 0 :
                        3 * entity.Analytics.ExtendCount + entity.Analytics.PageVisitCount
                        + entity.Analytics.ManualRank;
                return result;
            }
            else
            {
                return 0;
            }

        }

        protected static ProfileModel MapToModelProfile(Profile entity)
        {
            if (entity != null)
            {
                var result = new ProfileModel
                {
                    ID = entity.ID,
                    Namespace = entity.Namespace,
                    StandardProfileID = entity.StandardProfileID,
                    Version = entity.Version,
                    PublishDate = entity.PublishDate,
                    AuthorId = entity.AuthorId
                    //for space saving, performance, don't populate these values in this scenario
                    //FileCache = entity.NodeSetId,  
                };
                return result;
            }
            else
            {
                return null;
            }

        }

        protected static ProfileTypeDefinitionSimpleModel MapToModelProfileTypDefSimple(ProfileTypeDefinition entity)
        {
            if (entity != null)
            {
                return new ProfileTypeDefinitionSimpleModel
                {
                    ID = entity.ID,
                    OpcNodeId = entity.OpcNodeId,
                    Name = entity.Name,
                    BrowseName = entity.BrowseName,
                    SymbolicName = entity.SymbolicName,
                    ProfileId = entity.ProfileId,
                    Profile = MapToModelProfile(entity.Profile),
                    IsAbstract = entity.IsAbstract,
                    Description = entity.Description,
                    Type = entity.ProfileType != null ?
                        new LookupItemModel { ID = entity.ProfileType.ID, Name = entity.ProfileType.Name, TypeId = (int)LookupTypeEnum.ProfileType }
                        : new LookupItemModel { ID = entity.ProfileTypeId }, // CODE REVIEW: ist the Name required anywhere? Should we do a lookup here?
                    Author = MapToModelAuthor(entity)
                };
            }
            else
            {
                return null;
            }

        }

        private static UserSimpleModel MapToModelAuthor(ProfileTypeDefinition entity)
        {
            if (entity.Author == null) return null;
            return new UserSimpleModel
            {
                ID = entity.Author.ID,
                FirstName = entity.Author.FirstName,
                LastName = entity.Author.LastName,
                Organization = entity.Author.Organization == null ? null :
                    new OrganizationModel() { ID = entity.Author.Organization.ID, Name = entity.Author.Organization.Name },
            };
        }

        private List<ProfileAttributeModel> MapToModelAttributes(ProfileTypeDefinition entity)
        {
            //sort by enum value if present, then by name
            if (entity.Attributes == null) return null;
            return entity.Attributes
                .OrderByDescending(a => a.EnumValue.HasValue)
                .ThenBy(a => a.EnumValue)
                .ThenBy(a => a.Name)
                .Select(a => MapToModelAttribute(entity.ID, a))
                .ToList();
        }


        private ProfileAttributeModel MapToModelAttribute(int? typeDefinitionId, ProfileAttribute item)
        {
            return new ProfileAttributeModel()
            {
                ID = item.ID,
                OpcNodeId = item.OpcNodeId,
                BrowseName = item.BrowseName,
                SymbolicName = item.SymbolicName,
                Namespace = item.Namespace,
                Name = item.Name,
                Description = item.Description,
                DisplayName = item.DisplayName,
                Created = item.Created,
                CreatedBy = item.CreatedBy == null ? null : new UserSimpleModel { ID = item.CreatedBy.ID, FirstName = item.CreatedBy.FirstName, LastName = item.CreatedBy.LastName },
                Updated = item.Updated,
                UpdatedBy = item.UpdatedBy == null ? null : new UserSimpleModel { ID = item.UpdatedBy.ID, FirstName = item.UpdatedBy.FirstName, LastName = item.UpdatedBy.LastName },
                VariableTypeDefinitionId = item.VariableTypeDefinitionId,
                VariableTypeDefinition = MapToModel(item.VariableTypeDefinition, false),
                TypeDefinitionId = typeDefinitionId,
                DataTypeId = item.DataType?.ID,
                DataVariableNodeIds = item.DataVariableNodeIds,
                AttributeType = item.AttributeType != null ? new LookupItemModel() { ID = item.AttributeType.ID, Name = item.AttributeType.Name, Code = item.AttributeType.Code } : null,
                //TBD - come back to this...mapping
                DataType = MapToModelDataType(item),
                EngUnit = !item.EngUnitId.HasValue ? null : _euDAL.MapToModelPublic(item.EngUnit, true),
                EngUnitOpcNodeId = item.EngUnitOpcNodeId,
                MinValue = item.MinValue,
                MaxValue = item.MaxValue,
                InstrumentMinValue = item.InstrumentMinValue,
                InstrumentMaxValue = item.InstrumentMaxValue,
                EnumValue = item.EnumValue,
                IsRequired = item.IsRequired,
                ModelingRule = item.ModelingRule,
                IsArray = item.IsArray,
                ValueRank = item.ValueRank,
                ArrayDimensions = item.ArrayDimensions,
                //// Stored as JSON so return as JRaw, however, check to confirm there is a value first before trying to pass null.
                //TODO: SC - come back to this. Web API not liking JRAW type as part of model.
                //AdditionalData = !string.IsNullOrEmpty(item.AdditionalData) ? new JRaw(item.AdditionalData) : null,

                AccessLevel = item.AccessLevel,
                UserAccessLevel = item.UserAccessLevel,
                AccessRestrictions = item.AccessRestrictions,
                WriteMask = item.WriteMask,
                UserWriteMask = item.WriteMask,

                AdditionalData = item.AdditionalData,
                IsActive = item.IsActive
            };

        }

        /// <summary>
        /// Re-factor this into its own method to address code smell
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private LookupDataTypeModel MapToModelDataType(ProfileAttribute item)
        {
            if (item.DataType == null) return null;

            return new LookupDataTypeModel
            {
                ID = item.DataType.ID,
                Name = item.DataType.Name,
                Code = item.DataType.Code,
                CustomTypeId = item.DataType.CustomTypeId,
                CustomType = item.DataType.CustomType != null ?
                             MapToModel(item.DataType.CustomType, false) : null,
            };
        }

        private List<ProfileTypeDefinitionModel> MapToModelInterfaces(List<ProfileInterface> interfaces)
        {
            if (interfaces == null) return null;
            var result = interfaces.OrderBy(i => i.Interface.Name)
                .Select((i, index) =>
                {
                    var typeModel = new ProfileTypeDefinitionModel
                    {
                        ID = i.InterfaceId,
                        OpcNodeId = i.Interface.OpcNodeId,
                        ProfileId = i.Interface.ProfileId != 0 ? i.Interface.ProfileId : null,
                        Profile = _profileDAL.MapToModelPublic(i.Interface.Profile, true),
                        Name = i.Interface.Name,
                        BrowseName = i.Interface.BrowseName,
                        SymbolicName = i.Interface.SymbolicName,
                        AuthorId = 99,  //just populate with some value for now to prevent model state errors on save. 
                        Description = i.Interface.Description,
                        DocumentUrl = i.Interface.DocumentUrl,
                        TypeId = i.Interface.ProfileTypeId,
                        Type = new LookupItemModel { ID = i.Interface.ProfileType?.ID, Name = i.Interface.ProfileType?.Name },
                        //just get the id for navigating up the ancestory tree
                        Parent = !i.Interface.ParentId.HasValue ? null : new ProfileTypeDefinitionSimpleModel() { ID = i.Interface.ParentId.Value },
                        Attributes = MapToModelInterfaceAttributes(i.Interface, index),
                        //Compositions - added below
                        IsAbstract = i.Interface.IsAbstract,
                    };
                    typeModel.Compositions = MapToModelCompositions(i.Interface.Compositions, typeModel);
                    return typeModel;
                }

                ).ToList();
            return result;
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// TBD - Come back to this and improve encapsulation of this and MapToModelAttributes.
        /// </remarks>
        /// <param name="parentId"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        private List<ProfileAttributeModel> MapToModelInterfaceAttributes(ProfileTypeDefinition entity, int groupId)
        {
            if (entity.Attributes == null) return null;
            var result = entity.Attributes.OrderBy(a => a.Name)
                .Select(a => MapToModelAttribute(entity.ID, a))
                .ToList();

            //add some interface specific stuff for front end
            foreach (var a in result)
            {
                a.Interface = new ProfileTypeDefinitionRelatedModel() { ID = entity.ID, Name = entity.Name, BrowseName = entity.BrowseName, SymbolicName = entity.SymbolicName, OpcNodeId = entity.OpcNodeId };
                a.InterfaceGroupId = groupId;
            }
            return result;

        }

        private List<ProfileTypeDefinitionRelatedModel> MapToModelCompositions(List<ProfileComposition> compositions, ProfileTypeDefinitionModel composingModel)
        {
            if (compositions == null) return null;
            var result = compositions.OrderBy(i => i.Composition.Name)
                .Select(i =>
                new ProfileTypeDefinitionRelatedModel
                {
                    ID = i.ID, //composingModel.ID,
                    ProfileTypeDefinition = composingModel,
                    Name = i.Name,
                    BrowseName = i.BrowseName,
                    Profile = composingModel.Profile,

                    RelatedIsRequired = i.IsRequired,
                    RelatedModelingRule = i.ModelingRule,
                    RelatedIsEvent = i.IsEvent,
                    Description = i.Description,
                    RelatedProfileTypeDefinitionId = i.Composition.ID,
                    RelatedProfileTypeDefinition = this.MapToModel(i.Composition, false),
                    RelatedName = i.Composition.Name,
                    RelatedDescription = i.Composition.Description,
                    RelatedReferenceId = i.ReferenceId,
                    Type = i.Composition.ProfileType != null ? new LookupItemModel { ID = i.Composition.ProfileType.ID, Name = i.Composition.ProfileType.Name } : new LookupItemModel { ID = i.Composition.ProfileTypeId },
                }).ToList();
            return result;
        }

        #endregion

        #region Map MODEL to Entity
        public void MapToEntityPublic(ref ProfileTypeDefinition entity, ProfileTypeDefinitionModel model, UserToken userToken)
        {
            MapToEntity(ref entity, model, userToken);
        }
        protected override void MapToEntity(ref ProfileTypeDefinition entity, ProfileTypeDefinitionModel model, UserToken userToken)
        {
            //updated by, created by - set in base class.
            entity.Name = model.Name;
            if (string.IsNullOrEmpty(model.OpcNodeId))// && !string.IsNullOrEmpty(model.Profile?.Namespace))
            {
                model.OpcNodeId = $"g={Guid.NewGuid()}";
                // TODO find the highest used node id: not just in profile type definitions but also attributes, data types, engineering units etc., so this is insufficient
                //var highestNodeId = this.Where(pd => pd.Profile.Namespace == model.Profile.Namespace, userToken).Data.OrderByDescending(pd => pd.OpcNodeId).FirstOrDefault();
                //if (highestNodeId?.OpcNodeId != null && highestNodeId.OpcNodeId.StartsWith("i=") && int.TryParse(highestNodeId.OpcNodeId.Substring("i=".Length), out var nodeIdNumber))
                //{
                //    highestNodeId.OpcNodeId = $"i={nodeIdNumber+1}";
                //}
                //else
                //{
                //    model.OpcNodeId = "i=5000";
                //}
            }
            entity.OpcNodeId = model.OpcNodeId;
            entity.ProfileId = model.ProfileId != 0 ? model.ProfileId : null;
            if (model.Profile != null)
            {
                var profileEntity = entity.Profile;
                if (profileEntity == null)
                {
                    profileEntity = _profileDAL.CheckForExisting(model.Profile, userToken);
                    if (profileEntity == null)
                    {
                        throw new NotImplementedException($"Profiles must be added explicitly");
                    }
                }
                entity.Profile = profileEntity;
            }
            entity.BrowseName = model.BrowseName;
            entity.SymbolicName = model.SymbolicName;
            entity.ProfileTypeId = model.TypeId;
            entity.Description = model.Description;
            entity.DocumentUrl = model.DocumentUrl;
            entity.IsAbstract = model.IsAbstract;
            entity.Updated = DateTime.UtcNow;

            // TODO any other columns that need to be mapped?

            entity.AuthorId = model.AuthorId;
            entity.ExternalAuthor = model.ExternalAuthor;
            if (CheckForExisting(model, userToken, true) == null)
            {
                _repo.Attach(entity);  // Attach to context so CheckForExisting can find it if there are recursive references in subsequent mapping operations
            }
            entity.ParentId = model.Parent?.ID != 0 ? model.Parent?.ID : null;
            if (model.Parent?.ProfileTypeDefinition != null)
            {
                var parentProfileEntity = entity.Parent;
                if (parentProfileEntity == null)
                {
                    parentProfileEntity = CheckForExisting(model.Parent.ProfileTypeDefinition, userToken);
                    if (parentProfileEntity == null)
                    {
                        this._diLogger.LogWarning($"Creating parent profile type {model.Parent.ProfileTypeDefinition} as side effect of creating {model}");
                        this.Add(model.Parent.ProfileTypeDefinition, userToken).Wait();
                        parentProfileEntity = CheckForExisting(model.Parent.ProfileTypeDefinition, userToken);
                    }
                    entity.Parent = parentProfileEntity;
                }
            }

            entity.InstanceParentId = model.InstanceParent?.ID != 0 ? model.InstanceParent?.ID : null;
            if (model.InstanceParent != null)
            {
                var instanceParentEntity = entity.InstanceParent;
                if (instanceParentEntity == null)
                {
                    instanceParentEntity = CheckForExisting(model.InstanceParent, userToken);
                    if (instanceParentEntity == null)
                    {
                        this.Add(model.InstanceParent, userToken).Wait();
                        instanceParentEntity = CheckForExisting(model.InstanceParent, userToken);
                    }
                    entity.InstanceParent = instanceParentEntity;
                }
                if (instanceParentEntity != null)
                {
                    _diLogger.LogInformation($"Instance parent {model.InstanceParent} in {model} has an entity and was updated.");
                }
            }

            //favorite
            MapToEntityFavorite(ref entity, model, userToken);

            MapToEntityProfileAttribute(ref entity, model.Attributes, userToken);
            MapToEntityInterfaces(ref entity, model.Interfaces, userToken);
            MapToEntityCompositions(ref entity, model.Compositions, userToken);
            MapToEntityMetaTags(ref entity, model.MetaTags);
        }

        protected static void MapToEntityFavorite(ref ProfileTypeDefinition entity, ProfileTypeDefinitionModel model, UserToken userToken)
        {
            if (model.IsFavorite.HasValue && model.IsFavorite.Value)
            {
                entity.Favorite ??= new ProfileTypeDefinitionFavorite() { OwnerId = userToken.UserId, ProfileTypeDefinitionId = model.ID.Value };
                entity.Favorite.IsFavorite = model.IsFavorite.Value;
            }
            else
            {
                entity.Favorite = null;
            }
        }

        protected void MapToEntityProfileAttribute(ref ProfileTypeDefinition entity, List<Models.ProfileAttributeModel> attributes, UserToken userToken)
        {
            //init visit services for new scenario
            if (entity.Attributes == null) entity.Attributes = new List<ProfileAttribute>();

            // Remove attribs no longer used
            // Use counter from end of collection so we can remove and not mess up loop iterator 
            if (entity.Attributes.Count > 0)
            {
                var length = entity.Attributes.Count - 1;
                for (var i = length; i >= 0; i--)
                {
                    var current = entity.Attributes[i];

                    //remove if no longer present
                    var source = attributes?.Find(x =>
                        MatchIdentity(current, x)
                        );
                    if (source == null)
                    {
                        entity.Attributes.RemoveAt(i);
                    }
                    else
                    {
                        //update if present
                        current.Name = source.Name;
                        current.BrowseName = source.BrowseName;
                        current.SymbolicName = source.SymbolicName;
                        if (string.IsNullOrEmpty(source.OpcNodeId)/* && !string.IsNullOrEmpty(source...Profile?.Namespace)*/)
                        {
                            source.OpcNodeId = $"g={Guid.NewGuid()}";
                        }
                        current.OpcNodeId = source.OpcNodeId;
                        current.Namespace = source.Namespace;
                        current.Description = source.Description;
                        current.DisplayName = source.DisplayName;
                        current.Updated = DateTime.UtcNow;
                        current.UpdatedById = userToken.UserId;
                        current.EngUnitId = source.EngUnit?.ID != 0 ? source.EngUnit?.ID : null;
                        current.EngUnitOpcNodeId = source.EngUnitOpcNodeId;
                        current.MinValue = source.MinValue;
                        current.MaxValue = source.MaxValue;
                        current.DataTypeId = source.DataType?.ID != 0 ? source.DataType.ID : null;
                        current.DataVariableNodeIds = source.DataVariableNodeIds;
                        var dataType = current.DataType;
                        if (dataType == null || dataType.ID != source.DataType.ID)
                        {
                            dataType = _dataTypeDAL.CheckForExisting(source.DataType, userToken);
                            if (dataType == null)
                            {
                                throw new Exception($"Unable to resolve data type {source.DataType} in {source} ");
                            }
                            current.DataType = dataType;
                        }

                        current.VariableTypeDefinitionId = source.VariableTypeDefinition?.ID != 0 ? source.VariableTypeDefinitionId : null;
                        var variableType = current.VariableTypeDefinition;
                        if (source.VariableTypeDefinition != null)
                        {
                            if (variableType == null || variableType.ID != source.VariableTypeDefinition.ID)
                            {
                                variableType = CheckForExisting(source.VariableTypeDefinition, userToken);
                                if (variableType == null)
                                {
                                    throw new Exception($"Unable to resolve variable type {source.VariableTypeDefinition} in {source} ");
                                }
                            }
                        }
                        else
                        {
                            variableType = null;
                        }
                        current.VariableTypeDefinition = variableType;
                        if (source.AttributeType?.ID != null)
                        {
                            current.AttributeTypeId = source.AttributeType.ID;
                        }
                        current.EnumValue = source.AttributeType?.ID == (int)AttributeTypeIdEnum.EnumField ? source.EnumValue : null;
                        current.IsRequired = source.IsRequired;
                        current.ModelingRule = source.ModelingRule;
                        current.IsArray = source.IsArray;
                        current.ValueRank = source.ValueRank;
                        current.ArrayDimensions = source.ArrayDimensions;

                        current.AccessLevel = source.AccessLevel;
                        current.UserAccessLevel = source.UserAccessLevel;
                        current.AccessRestrictions = source.AccessRestrictions;
                        current.WriteMask = source.WriteMask;
                        current.UserWriteMask = source.UserWriteMask;

                        current.AdditionalData = source.AdditionalData;

                    }
                }
            }

            // Loop over attribs passed in and only add those not already there
            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    if (
                        entity.Attributes.Find(up => MatchIdentity(up, attr)) == null
                        )
                    {
                        var dataType = _dataTypeDAL.CheckForExisting(attr.DataType, userToken);
                        if (dataType == null)
                        {
                            attr.DataType.ID = _dataTypeDAL.Add(attr.DataType, userToken).Result;
                            dataType = _dataTypeDAL.CheckForExisting(attr.DataType, userToken);
                        }
                        ProfileTypeDefinition variableType = null;
                        if (attr.VariableTypeDefinition != null)
                        {
                            variableType = CheckForExisting(attr.VariableTypeDefinition, userToken);
                            if (variableType == null)
                            {
                                this._diLogger.LogWarning($"Creating variable type {attr.VariableTypeDefinition} as side effect of creating {attr}");
                                this.Add(attr.VariableTypeDefinition, userToken).Wait();
                                variableType = CheckForExisting(attr.VariableTypeDefinition, userToken);
                            }
                        }

                        EngineeringUnit engUnit = null;
                        if (attr.EngUnit != null)
                        {
                            engUnit = _euDAL.CheckForExisting(attr.EngUnit, userToken);
                            if (engUnit == null)
                            {
                                throw new Exception($"Engineering unit must be explicitly created: {attr.EngUnit} for {entity}");
                            }
                        }

                        entity.Attributes.Add(new ProfileAttribute
                        {
                            Name = attr.Name,
                            BrowseName = attr.BrowseName,
                            SymbolicName = attr.SymbolicName,
                            OpcNodeId = attr.OpcNodeId,
                            Namespace = attr.Namespace,
                            ProfileTypeDefinitionId = entity.ID,
                            Description = attr.Description,
                            DisplayName = attr.DisplayName,
                            Created = DateTime.UtcNow,
                            CreatedById = userToken.UserId,
                            Updated = DateTime.UtcNow,
                            UpdatedById = userToken.UserId,
                            MinValue = attr.MinValue,
                            MaxValue = attr.MaxValue,
                            DataTypeId = attr.DataType.ID,
                            DataType = dataType,
                            DataVariableNodeIds = attr.DataVariableNodeIds,
                            VariableTypeDefinitionId = attr.VariableTypeDefinitionId,
                            VariableTypeDefinition = variableType,
                            AttributeTypeId = attr.AttributeType?.ID ?? 0,
                            EnumValue = attr.AttributeType?.ID == (int)AttributeTypeIdEnum.EnumField ? attr.EnumValue : null,
                            IsRequired = attr.IsRequired,
                            ModelingRule = attr.ModelingRule,
                            IsArray = attr.IsArray,
                            ValueRank = attr.ValueRank,
                            ArrayDimensions = attr.ArrayDimensions,
                            EngUnitId = attr.EngUnit != null && attr.EngUnit.ID != 0 ? attr.EngUnit.ID : null,
                            EngUnit = engUnit,
                            EngUnitOpcNodeId = attr.EngUnitOpcNodeId,

                            AccessLevel = attr.AccessLevel,
                            UserAccessLevel = attr.UserAccessLevel,
                            AccessRestrictions = attr.AccessRestrictions,
                            WriteMask = attr.WriteMask,
                            UserWriteMask = attr.UserWriteMask,

                            AdditionalData = attr.AdditionalData,
                            IsActive = true
                        });
                    }
                }
            }
        }

        protected void MapToEntityInterfaces(ref ProfileTypeDefinition entity, List<ProfileTypeDefinitionModel> interfaces, UserToken userToken)
        {
            //init interfaces obj for new scenario
            if (entity.Interfaces == null) entity.Interfaces = new List<ProfileInterface>();

            if (interfaces == null) return; //this shouldn't happen. If all items removed, then it should be a collection w/ 0 items.

            // Remove items no longer used
            // Use counter from end of collection so we can remove and not mess up loop iterator 
            if (entity.Interfaces.Count > 0)
            {
                var length = entity.Interfaces.Count - 1;
                for (var i = length; i >= 0; i--)
                {
                    var current = entity.Interfaces[i];
                    //remove if no longer present
                    var source = interfaces.Find(v => MatchIdentity(current.Interface, v));
                    if (source == null)
                    {
                        entity.Interfaces.RemoveAt(i);
                    }
                    else
                    {
                        //if present, do nothing
                    }
                }
            }

            // Loop over interfaces passed in and only add those not already there
            foreach (var y in interfaces)
            {
                if (
                    entity.Interfaces.Find(x => MatchIdentity(x.Interface, y)) == null
                    )
                {
                    var interfaceEntity = CheckForExisting(y, userToken);
                    if (interfaceEntity == null)
                    {
                        throw new NotImplementedException("Interface must be added explicitly");
                    }
                    entity.Interfaces.Add(new ProfileInterface
                    {
                        ProfileTypeDefinitionId = entity.ID,
                        ProfileTypeDefinition = entity,
                        InterfaceId = interfaceEntity.ID,
                        Interface = interfaceEntity,
                    });
                }
            }
        }

        protected void MapToEntityCompositions(ref ProfileTypeDefinition entity, List<ProfileTypeDefinitionRelatedModel> compositions, UserToken userToken)
        {
            //init compositions obj for new scenario
            if (entity.Compositions == null) entity.Compositions = new List<Data.Entities.ProfileComposition>();

            if (compositions == null) return; //this shouldn't happen. If all items removed, then it should be a collection w/ 0 items.

            // Remove compositions no longer used
            // Use counter from end of collection so we can remove and not mess up loop iterator 
            if (entity.Compositions.Count > 0)
            {
                var length = entity.Compositions.Count - 1;
                for (var i = length; i >= 0; i--)
                {
                    var currentEntity = entity.Compositions[i];

                    //remove if no longer present
                    var source = compositions.Find(v => MatchIdentity(currentEntity, v)); 
                    if (source == null)
                    {
                        entity.Compositions.RemoveAt(i);
                    }
                    else
                    {
                        //if present, update name, description
                        var composition = currentEntity;
                        MapToEntityCompositionInternal(ref composition, source, entity, userToken);
                        entity.Compositions[i] = composition;
                    }
                }
            }

            // Loop over compositions passed in and only add those not already there
            foreach (var y in compositions)
            {
                if (entity.Compositions.Find(x => MatchIdentity(x, y)) == null)//(y.ID ?? 0) != 0 ? x.ID.Equals(y.ID) : x.OpcNodeId == y.OpcNodeId && x.ProfileTypeDefinition.Profile.Namespace == y.Profile.Namespace) == null)
                {
                    // -> front end: composition id will be set
                    // -> importer: related id also 0 -> look up based on name/namespace etc.
                    // Add nodeId to profiles etc.
                    var composition = new ProfileComposition();
                    MapToEntityCompositionInternal(ref composition, y, entity, userToken);
                    entity.Compositions.Add(composition);
                }
            }
        }


        private void MapToEntityCompositionInternal(ref ProfileComposition composition, ProfileTypeDefinitionRelatedModel source, ProfileTypeDefinition parentEntity, /*List<(ProfileTypeDefinitionModel Model, ProfileTypeDefinition Entity)> modelsProcessed, */UserToken userToken)
        {
            composition.CompositionId = source.RelatedProfileTypeDefinitionId; //should be same
            if (composition.Composition == null && source.RelatedProfileTypeDefinition != null)
            {
                var profileTypeDef = CheckForExisting(source.RelatedProfileTypeDefinition, userToken);
                if (profileTypeDef == null)
                {
                    this.Add(source.RelatedProfileTypeDefinition, userToken).Wait();
                    profileTypeDef = CheckForExisting(source.RelatedProfileTypeDefinition, userToken);
                }
                composition.Composition = profileTypeDef;
                composition.CompositionId = profileTypeDef.ID;
            }
            if (composition.ProfileTypeDefinition != null && composition.ProfileTypeDefinitionId != parentEntity.ID)
            {
                throw new Exception($"Internal error: {composition.ProfileTypeDefinition} does not match {parentEntity}");
            }
            composition.ProfileTypeDefinitionId = parentEntity.ID;            //should be same
            if (composition.ProfileTypeDefinition == null && source.ProfileTypeDefinition != null)
            {
                ProfileTypeDefinition profileTypeDef;
                if (MatchIdentity(parentEntity, source.ProfileTypeDefinition))//source.ProfileTypeDefinition.OpcNodeId == parentEntity.OpcNodeId && source.ProfileTypeDefinition.Profile.Namespace == parentEntity.Profile.Namespace)
                {
                    profileTypeDef = parentEntity;
                }
                else
                {
                    profileTypeDef = CheckForExisting(source.ProfileTypeDefinition, userToken);
                    if (profileTypeDef == null)
                    {
                        throw new NotImplementedException("Profile must be added explicitly");
                    }
                }
                composition.ProfileTypeDefinition = profileTypeDef;
                composition.ProfileTypeDefinitionId = profileTypeDef.ID;
            }
            composition.Name = source.Name;
            composition.BrowseName = source.BrowseName;
            composition.IsRequired = source.RelatedIsRequired;
            composition.ModelingRule = source.RelatedModelingRule;
            composition.IsEvent = source.RelatedIsEvent;
            composition.ReferenceId = source.RelatedReferenceId;
            composition.Description = source.Description;
        }

        protected static void MapToEntityMetaTags(ref ProfileTypeDefinition entity, List<string> metaTags)
        {
            if (metaTags == null || !metaTags.Any())
            {
                entity.MetaTags = null;
            }
            else
            {
                //always swap out the entire list of metatags. trim out whitespaces and any empty tags.
                //these are stored as a simple JSON string of strings
                var result = metaTags.Where(s => !string.IsNullOrEmpty(s))
                    .OrderBy(s => s)
                    .Select(s => new MetaTag
                    { Name = s.Trim() }).ToList();

                //convert to json string
                entity.MetaTags = JsonConvert.SerializeObject(result);
            }
        }

        #endregion

    }
}