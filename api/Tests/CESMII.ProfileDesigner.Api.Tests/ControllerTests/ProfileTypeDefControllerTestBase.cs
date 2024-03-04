using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using Xunit.Abstractions;
using Newtonsoft.Json;

using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Repositories;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.Data.Contexts;
using CESMII.ProfileDesigner.Api.Shared.Models;

namespace CESMII.ProfileDesigner.Api.Tests.Int.Controllers
{
    public class ProfileTypeDefControllerTestBase : ControllerTestBase
    {
        protected readonly ServiceProvider _serviceProvider;
        //for some tests, tie together a common guid so we can delete the created items at end of test. 
        protected Guid _guidCommon = Guid.NewGuid();

        //get some lookup data that will be needed when we start adding addtribute tests
        protected AppLookupModel _lookupData = null;
        protected ProfileLookupModel _lookupRelated = null;

        protected int _profileRootId;
        protected int _compositionRootId;  //_baseObjectType id 
        protected int _interfaceRootId;  //_baseInterfaceType id 
        protected const string FN_GET_DESCENDANTS = "public.fn_profile_type_definition_get_descendants";

        #region API constants
        protected const string URL_EXTEND = "/api/profiletypedefinition/extend";
        protected const string URL_ADD = "/api/profiletypedefinition/add";
        protected const string URL_GETBYID = "/api/profiletypedefinition/getbyid";

        protected const string URL_LOOKUP_ALL = "/api/lookup/all";
        protected const string URL_LOOKUP_RELATED = "/api/profiletypedefinition/lookup/profilerelated/extend";
        #endregion

        #region data naming constants
        protected const string TITLE_PATTERN = "CESMII.ProfileDesigner.Api.Tests.Integration";
        protected const string PARENT_PROFILE_NAMESPACE = "https://CESMII.Profile.Mock.org/";
        protected const int TYPE_ID_DEFAULT = (int)ProfileItemTypeEnum.Class;  
        #endregion

        public ProfileTypeDefControllerTestBase(
            CustomWebApplicationFactory<Api.Startup> factory, 
            ITestOutputHelper output):
            base(factory, output)
        {
            var services = new ServiceCollection();

            //wire up db context to be used by repos
            base.InitDBContext(services);
            
            // DI - directly inject repo so we can add some test data directly and then have API test against it.
            // when running search tests. 
            services.AddSingleton<IConfiguration>(factory.Config);
            services.AddScoped<IRepository<Profile>, BaseRepo<Profile, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<ProfileTypeDefinition>, BaseRepo<ProfileTypeDefinition, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<ProfileTypeDefinitionAnalytic>, BaseRepo<ProfileTypeDefinitionAnalytic, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<ProfileAttribute>, BaseRepo<ProfileAttribute, ProfileDesignerPgContext>>();
            services.AddScoped<IRepository<LookupDataType>, BaseRepo<LookupDataType, ProfileDesignerPgContext>>();

            services.AddScoped<IRepositoryStoredProcedure<ProfileTypeDefinitionSimple>, BaseRepoStoredProcedure<ProfileTypeDefinitionSimple, ProfileDesignerPgContext>>();

            //need to get user id of test user when we add profile
            services.AddScoped<IRepository<User>, BaseRepo<User, ProfileDesignerPgContext>>();
            
            _serviceProvider = services.BuildServiceProvider();

        }

#pragma warning disable xUnit1026  // Stop warnings related to parameters not used in test cases. 

        #region Insert Mock Data
        protected void PrepareMockData(MyNamespace.Client apiClient)
        {
            //to make this stateless (not dependent on the existence of other nodesets being present),
            //create all of our own dependent data
            InsertDependentMockData().Wait();

            //get lookup data to be used when adding attributes
            _lookupData = GetLookupData(apiClient, _guidCommon).Result;
            //get related lookup data - variable types list, compositions, interfaces. Using mock data to keep this stateless.
            _lookupRelated = GetRelatedData(_profileRootId);
        }

        protected async Task InsertDependentMockData()
        {
            //root type def and parent profile
            var baseDataType = await InsertMockProfileAndTypeDefinition((int)ProfileItemTypeEnum.CustomDataType, _guidCommon, "_BaseDataType");
            _profileRootId = baseDataType.ProfileId.Value;

            using (var scope = _serviceProvider.CreateScope())
            {
                var repo = scope.ServiceProvider.GetService<IRepository<ProfileTypeDefinition>>();
                var repoDataType = scope.ServiceProvider.GetService<IRepository<LookupDataType>>();
                var repoUser = scope.ServiceProvider.GetService<IRepository<User>>();

                var baseObjectType = await InsertMockTypeDefinition(repo, repoUser, _profileRootId, null, (int)ProfileItemTypeEnum.Class, _guidCommon, "_BaseObjectType");
                _compositionRootId = baseObjectType.ID.Value;
                var baseVariableType = await InsertMockTypeDefinition(repo, repoUser, _profileRootId, null, (int)ProfileItemTypeEnum.VariableType, _guidCommon, "_BaseVariableType");
                var baseInterfaceType = await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseObjectType, (int)ProfileItemTypeEnum.Interface, _guidCommon, "_BaseInterfaceType");
                _interfaceRootId = baseInterfaceType.ID.Value;
                var baseEnumeration = await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseDataType, (int)ProfileItemTypeEnum.Enumeration, _guidCommon, "_Enumeration");

                var baseDataVariableType = await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseVariableType, (int)ProfileItemTypeEnum.VariableType, _guidCommon, "_BaseDataVariableType");
                //data types
                var dt0 = await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseVariableType, (int)ProfileItemTypeEnum.VariableType, _guidCommon, "_Int");
                await InsertMockDataType(repoDataType, dt0, _guidCommon);
                var dt1 = await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseVariableType, (int)ProfileItemTypeEnum.VariableType, _guidCommon, "_DateTime");
                await InsertMockDataType(repoDataType, dt1, _guidCommon);
                var dt2 = await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseVariableType, (int)ProfileItemTypeEnum.VariableType, _guidCommon, "_Boolean");
                await InsertMockDataType(repoDataType, dt2, _guidCommon);
                var dt3 = await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseVariableType, (int)ProfileItemTypeEnum.VariableType, _guidCommon, "_Number");
                await InsertMockDataType(repoDataType, dt3, _guidCommon);
                var dt4 = await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseVariableType, (int)ProfileItemTypeEnum.VariableType, _guidCommon, "_String");
                await InsertMockDataType(repoDataType, dt4, _guidCommon);
                var dt5 = await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseVariableType, (int)ProfileItemTypeEnum.VariableType, _guidCommon, "_Double");
                await InsertMockDataType(repoDataType, dt5, _guidCommon);
                var dt6 = await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseVariableType, (int)ProfileItemTypeEnum.VariableType, _guidCommon, "_Float");
                await InsertMockDataType(repoDataType, dt6, _guidCommon);
                
                //variable types
                await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseDataVariableType, (int)ProfileItemTypeEnum.VariableType, _guidCommon, "_FrameType");
                await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseDataVariableType, (int)ProfileItemTypeEnum.VariableType, _guidCommon, "_ProgramDiagnosticType");
                await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseDataVariableType, (int)ProfileItemTypeEnum.VariableType, _guidCommon, "_ReferenceDescriptionVariableType");
                await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseDataVariableType, (int)ProfileItemTypeEnum.VariableType, _guidCommon, "_PubSubDiagnosticsCounterType");

                //compositions - make them descendants of each other for more layers
                var compCurrent = baseObjectType;
                for (int i = 1; i <= 5; i++)
                {
                    compCurrent = await InsertMockTypeDefinition(repo, repoUser, _profileRootId, compCurrent, (int)ProfileItemTypeEnum.Class, _guidCommon, $"_BaseComposition_{i}");
                }

                //interfaces
                await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseInterfaceType, (int)ProfileItemTypeEnum.Interface, _guidCommon, "_IVlanIdType");
                await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseInterfaceType, (int)ProfileItemTypeEnum.Interface, _guidCommon, "_IOrderedObjectType");
                //enumerations
                await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseEnumeration, (int)ProfileItemTypeEnum.Enumeration, _guidCommon, "_ServerState");
                await InsertMockTypeDefinition(repo, repoUser, _profileRootId, baseEnumeration, (int)ProfileItemTypeEnum.Enumeration, _guidCommon, "_ApplicationType");
            }
        }

        /// <summary>
        /// Create a parent profile and an entity to extend from. 
        /// </summary>
        /// <param name="guidCommon"></param>
        /// <returns></returns>
        protected async Task<ProfileTypeDefinition> InsertMockProfileAndTypeDefinition(int typeId, Guid guidCommon, string name = null)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repoProfile = scope.ServiceProvider.GetService<IRepository<Profile>>();
                var repo = scope.ServiceProvider.GetService<IRepository<ProfileTypeDefinition>>();
                var repoUser = scope.ServiceProvider.GetService<IRepository<User>>();
                var user = GetTestUser(repoUser);

                //create a parent profile
                var profile = CreateProfileEntity(guidCommon, user);
                await repoProfile.AddAsync(profile);

                //create a parent type definition
                var result = CreateEntity(0, profile.ID, null, typeId, guidCommon, Guid.NewGuid(), user);
                if (!string.IsNullOrEmpty(name)) result.Name = name;
                await repo.AddAsync(result);

                //assign profile to type def in case caller needs it.
                result.Profile = profile;

                return result;
            }
        }

        protected async Task<ProfileTypeDefinition> InsertMockTypeDefinition(int profileId,
            ProfileTypeDefinition parent, int typeId, Guid guidCommon, string name = null)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repo = scope.ServiceProvider.GetService<IRepository<ProfileTypeDefinition>>();
                var repoUser = scope.ServiceProvider.GetService<IRepository<User>>();
                return await InsertMockTypeDefinition(repo, repoUser, profileId, parent, typeId, guidCommon, name);
            }
        }

        protected async Task<ProfileTypeDefinition> InsertMockTypeDefinition(
            IRepository<ProfileTypeDefinition> repo,
            IRepository<User> repoUser,
            int profileId, ProfileTypeDefinition parent, int typeId, Guid guidCommon, string name = null)
        {
            var user = GetTestUser(repoUser);
            //create a type definition
            var result = CreateEntity(0, profileId, parent, typeId, guidCommon, Guid.NewGuid(), user);
            if (!string.IsNullOrEmpty(name)) result.Name = name;
            await repo.AddAsync(result);
            return result;
        }

        protected async Task<LookupDataType> InsertMockDataType(
            IRepository<LookupDataType> repo,
            ProfileTypeDefinition typeDef, Guid guidCommon)
        {
            //create a data type, pointing to a type def
            //sybmolic name includes guidCommon
            var result = new LookupDataType() { Code = $"http://{guidCommon}/{typeDef.Name}", Name = typeDef.Name, CustomTypeId = typeDef.ID };
            await repo.AddAsync(result);
            return result;
        }

        /// <summary>
        /// This is used to create a row directly into DB. Bypasses everything except baseRepo
        /// </summary>
        /// <param name="i"></param>
        /// <param name="uuid"></param>
        /// <param name="creator"></param>
        /// <param name="cloudLibraryId"></param>
        /// <returns></returns>
        protected static ProfileTypeDefinition CreateEntity(int i, int? profileId, ProfileTypeDefinition parent, int typeId, Guid guidCommon, Guid uuid, User user)
        {
            var parentName = parent == null ? "TypeDef" : $"{parent.Name}::Extend";
            var dt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            var tags = new List<MetaTag>() {
                new MetaTag() { Name = (i % 4 == 0 ? "abcd" : (i % 3 == 0) ? "efgh" : (i % 2 == 0) ? "ijkl" : "mnop") }
            };
            return new ProfileTypeDefinition()
            {
                OpcNodeId = uuid.ToString(),
                Name = $"{parentName}-{i}",
                ProfileId = profileId,
                ParentId = parent?.ID,  //for some tests, we start with null parent and assign during test
                BrowseName = $"browse-{i}-{guidCommon}-{uuid}",
                SymbolicName = guidCommon.ToString(),
                Description = (i % 3 == 0 ? "Unique description for 3" : (i % 2 == 0) ? "Unique description for 2" : "Common description"),
                ProfileTypeId = parent == null ? typeId : parent?.ProfileTypeId,
                IsAbstract = i % 9 == 0,
                Created = dt,
                Updated = dt,
                AuthorId = user?.ID,
                OwnerId = user?.ID,
                CreatedById = user == null ? 0 : user.ID.Value,
                UpdatedById = user == null ? 0 : user.ID.Value,
                MetaTags = JsonConvert.SerializeObject(tags),
                IsActive = true,
            };
        }

        /// <summary>
        /// This is used to create a row directly into DB. Bypasses everything except baseRepo
        /// </summary>
        protected static Profile CreateProfileEntity(Guid guidCommon, User user)
        {
            var dt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            return new Profile()
            {
                Namespace = $"{PARENT_PROFILE_NAMESPACE}/{guidCommon}",
                Title = TITLE_PATTERN,
                Version = "1.0.0.0",
                CategoryName = "TEST",
                PublishDate = dt,
                AuthorId = user?.ID,
                OwnerId = user != null ? user.ID : null,
                Keywords = new string[] { guidCommon.ToString() },
                CreatedById = user == null ? 0 : user.ID.Value,
                UpdatedById = user == null ? 0 : user.ID.Value,
                Created = dt,
                Updated = dt,
            };
        }

        /// <summary>
        /// Create a composition attribute
        /// </summary>
        protected static ProfileAttributeModel CreateAttributeComposition(
            string name, string compositionName, Guid guidCommon,
            ProfileLookupModel lookupRelated, AppLookupModel lookupData)
        {
            var dt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            var comp = lookupRelated.Compositions.Find(x => x.Name.ToLower().Equals(compositionName.ToLower()));
            var attrType = lookupData.AttributeTypes.Find(x => x.Name.ToLower().Equals("composition"));
            //var dataType = _lookupData.DataTypes.Find(x => x.Name.ToLower().Equals("composition"));

            return new ProfileAttributeModel()
            {
                CompositionId = comp.ID,
                Composition = new ProfileTypeDefinitionRelatedModel()
                {
                    ID = comp.ID,
                    SymbolicName = guidCommon.ToString(),
                    RelatedProfileTypeDefinitionId = comp.ID,
                    Name = comp.Name
                },
                AttributeType = attrType,
                //matching happens on browse name - add unique portion to browse name
                BrowseName = $"{Guid.NewGuid()}:::{guidCommon}",
                SymbolicName = guidCommon.ToString(),
                //DataType = dataType,
                //DataTypeId = dataType.ID,
                Name = name
            };
        }

        protected static ProfileTypeDefinitionModel CreateItemModel(int i, int? profileId, ProfileTypeDefinition parent, Guid guidCommon, Guid uuid, string cloudLibraryId = null)
        {
            var entity = CreateEntity(i, profileId, parent, parent == null ? TYPE_ID_DEFAULT : parent.ProfileTypeId.Value, guidCommon, uuid, null);
            return MapToModel(entity);
        }

        #endregion

        #region Helper Methods
        protected ProfileLookupModel GetRelatedData(int profileId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repo = scope.ServiceProvider.GetService<IRepository<ProfileTypeDefinition>>();
                var repoDataType = scope.ServiceProvider.GetService<IRepository<LookupDataTypeRanked>>();
                var repoUser = scope.ServiceProvider.GetService<IRepository<User>>();
                var repoRelated = scope.ServiceProvider
                    .GetService<IRepositoryStoredProcedure<ProfileTypeDefinitionSimple>>();
                var user = GetTestUser(repoUser);

                var items = repo.FindByCondition(x => x.ProfileId.HasValue && x.ProfileId.Value.Equals(profileId));

                var orderBys = new List<OrderBySimple>() { new OrderBySimple() { FieldName = "name" } };
                var itemsComposition = repoRelated.ExecStoredFunction(FN_GET_DESCENDANTS, null, null, null, orderBys, 
                    _compositionRootId, user.ID, false, false)
                    .ToList();
                //(fnName, null, skip, take, orderBys, parameters)
                var itemsInterface = repoRelated.ExecStoredFunction(FN_GET_DESCENDANTS, null, null, null, orderBys, 
                    _interfaceRootId, user.ID, false, false)
                    .ToList();

                return new ProfileLookupModel()
                {
                    VariableTypes = items.Where(x => x.ProfileTypeId.Equals((int)ProfileItemTypeEnum.VariableType))
                                        .Select(x => new ProfileTypeDefinitionSimpleModel()
                                        {
                                            Name = x.Name,
                                            //Type = (ProfileItemTypeEnum)x.ProfileTypeId.Value,
                                            ProfileId = x.ProfileId.Value,
                                            VariableDataTypeId = x.VariableDataTypeId.Value,
                                            SymbolicName = x.SymbolicName,
                                            ID = x.ID,
                                            BrowseName = x.BrowseName,
                                        }).ToList(),
                    Compositions = itemsComposition
                                        .Select(x => new ProfileTypeDefinitionSimpleModel()
                                        {
                                            Name = x.Name,
                                            ProfileId = x.ProfileId,
                                            VariableDataTypeId = x.VariableDataTypeId.HasValue ? x.VariableDataTypeId.Value : null,
                                            ID = x.ID,
                                            BrowseName = x.BrowseName,
                                        }).ToList(),
                    Interfaces = itemsInterface
                                        .Select(x => new ProfileTypeDefinitionModel()
                                        {
                                            Name = x.Name,
                                            ProfileId = x.ProfileId,
                                            ID = x.ID,
                                            BrowseName = x.BrowseName,
                                        }).ToList()
                };
            }
        }

        protected async Task<AppLookupModel> GetLookupData(MyNamespace.Client apiClient, Guid guidCommon)
        {
            //get some static lookup data from db by calling api. get mock inserted data in a more direct manner.
            var result = await apiClient.ApiGetItemGenericAsync<AppLookupModel>(URL_LOOKUP_ALL, method: "GET");

            using (var scope = _serviceProvider.CreateScope())
            {
                //var repo = scope.ServiceProvider.GetService<IRepository<ProfileTypeDefinition>>();
                var repoDataType = scope.ServiceProvider.GetService<IRepository<LookupDataType>>();

                var itemDatatypes = repoDataType.FindByCondition(x => x.Code.Contains(guidCommon.ToString()));
                result.DataTypes = itemDatatypes.Select(x => new LookupDataTypeRankedModel()
                {
                    Code = x.Code,
                    BaseDataTypeId = x.CustomTypeId,
                    CustomTypeId = x.CustomTypeId,
                    Name = x.Name
                }).ToList();
            }
            return result;
        }
        
        /// <summary>
        /// Create a mock parent item. Then set up a newly extended item with that parent item.
        /// Finally, apply the model values to the newly extended item and return.
        /// </summary>
        /// <returns></returns>
        protected async Task<ProfileTypeDefinitionModel> MapModelToExtendedItem(MyNamespace.Client apiClient, Guid guidCommon,
            ProfileTypeDefinition itemExtend, ProfileTypeDefinitionModel model)
        {
            //extend item
            var result = await apiClient.ApiGetItemAsync<ProfileTypeDefinitionModel>(URL_EXTEND,
                new IdIntModel() { ID = itemExtend.ID.Value });
            //map data to newly created extend
            result.OpcNodeId = model.OpcNodeId;
            result.Name = model.Name;
            result.BrowseName = model.BrowseName;
            result.SymbolicName = guidCommon.ToString();  //so we can delete this item once done
            result.Description = model.Description;
            result.Created = model.Created;
            result.MetaTags = model.MetaTags;
            result.Attributes = model.Attributes;
            result.ProfileId = itemExtend.ProfileId;
            result.Profile = new ProfileModel() { ID = itemExtend.ProfileId };
            return result;
        }


        protected static ProfileTypeDefinitionModel MapToModel(ProfileTypeDefinition entity)
        {
            var tags = string.IsNullOrEmpty(entity.MetaTags) ? new List<string>() :
                    Newtonsoft.Json.JsonConvert.DeserializeObject<List<MetaTag>>(entity.MetaTags).Select(s => s.Name.Trim()).ToList();

            return new ProfileTypeDefinitionModel()
            {
                ID = entity.ID,
                OpcNodeId = entity.OpcNodeId,
                Name = entity.Name,
                ProfileId = entity.ProfileId != 0 ? entity.ProfileId : null,
                //Profile = MapToModelProfile(entity.Profile),
                BrowseName = entity.BrowseName,
                SymbolicName = entity.SymbolicName,
                Description = entity.Description,
                TypeId = entity.ProfileTypeId,
                Type = entity.ProfileType != null ?
                        new LookupItemModel { ID = entity.ProfileType.ID, Name = entity.ProfileType.Name, TypeId = entity.ProfileType.ID }
                        : null,
                AuthorId = entity.AuthorId ?? null,
                //Author = MapToModelSimpleUser(entity.Author),
                ExternalAuthor = entity.ExternalAuthor,
                DocumentUrl = entity.DocumentUrl,
                IsAbstract = entity.IsAbstract,
                Created = entity.Created,
                Updated = entity.Updated,
                MetaTags = tags,
                MetaTagsConcatenated = string.IsNullOrEmpty(entity.MetaTags) ? "" :
                    string.Join(", ", tags),
                IsActive = entity.IsActive,
                IsFavorite = entity.Favorite != null,
                //calculated value which gives more emphasis on extending an item
                //PopularityIndex = MapToModelPopularityIndex(entity)
            };
        }

        protected static ProfileTypeDefinitionSimpleModel MapToModelSimple(ProfileTypeDefinition entity)
        {
            return new ProfileTypeDefinitionSimpleModel()
            {
                ID = entity.ID,
                OpcNodeId = entity.OpcNodeId,
                Name = entity.Name,
                ProfileId = entity.ProfileId != 0 ? entity.ProfileId : null,
                //Profile = MapToModelProfile(entity.Profile),
                BrowseName = entity.BrowseName,
                SymbolicName = entity.SymbolicName,
                Description = entity.Description,
                Type = entity.ProfileType != null ?
                        new LookupItemModel { ID = entity.ProfileType.ID, Name = entity.ProfileType.Name, TypeId = entity.ProfileType.ID }
                        : null,
                Author = new UserSimpleModel() {ID = entity.AuthorId } ,
                IsAbstract = entity.IsAbstract,
                MetaTags = string.IsNullOrEmpty(entity.MetaTags) ? new List<string>() :
                    JsonConvert.DeserializeObject<List<MetaTag>>(entity.MetaTags).Select(s => s.Name.Trim()).ToList(),
            };
        }


        /// <summary>
        /// Delete profiles created during each test
        /// User <_guidCommon> as way to find items to delete 
        /// </summary>
        /// <returns></returns>
        protected virtual async Task CleanupEntities()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                //type defs
                var repo = scope.ServiceProvider.GetService<IRepository<ProfileTypeDefinition>>();
                var repoProfile = scope.ServiceProvider.GetService<IRepository<Profile>>();
                var repoAttribute = scope.ServiceProvider.GetService<IRepository<ProfileAttribute>>();
                var repoDataType = scope.ServiceProvider.GetService<IRepository<LookupDataType>>();

                //order by to account for some fk delete issues, include child tables - analytics, attributes, compositions, interfaces
                var items = repo.FindByCondition(x =>
                    x.SymbolicName != null && x.SymbolicName.ToLower().Contains(_guidCommon.ToString()))
                    //.OrderBy(x => x.ParentId.HasValue)
                    .OrderByDescending(x => !x.ParentId.HasValue ? 0 : x.ParentId.Value)
                    .Include(x => x.Interfaces)
                    .Include(x => x.Compositions)
                    .Include(x => x.Analytics)
                    .Include(x => x.Attributes)
                    .ToList();

                //parent profiles
                var itemsProfile = repoProfile.FindByCondition(x =>
                    items.Select(y => y.ProfileId.Value).Contains(x.ID.Value))
                    .ToList();

                //child attrs
                var itemsAttribute = repoAttribute.FindByCondition(x =>
                    items.Select(y => y.ID.Value).Contains(x.ProfileTypeDefinitionId.Value))
                    .ToList();

                //get intermediate items created server side that are related to items test created - intermediate objs
                //assuming the item would be a child of the parent profile we will delete below. 
                //prevent dups - only the items not collected above so that we don't try to delete something not there. 
                var itemsIntermediate = repo.FindByCondition(x =>
                    itemsProfile.Select(y => y.ID).Contains(x.ProfileId) &&
                    !items.Select(y => y.ID.Value).Contains(x.ID.Value))
                    .Include(x => x.Interfaces)
                    .Include(x => x.Compositions)
                    .Include(x => x.Analytics)
                    .Include(x => x.Attributes)
                    .ToList();

                var itemsDataType = repoDataType.FindByCondition(x => x.Code.Contains(_guidCommon.ToString())).ToList();

                //order of operation matters - several dependencies and FK constraints
                //attributes associated w/ type defs - do these first so we can remove FK in order to then delete data types
                foreach (var item in itemsAttribute)
                {
                    await repoAttribute.DeleteAsync(item);
                }
                await repoAttribute.SaveChangesAsync();

                //data types
                foreach (var item in itemsDataType)
                {
                    await repoDataType.DeleteAsync(item);
                }
                await repoDataType.SaveChangesAsync();

                //type defs
                foreach (var item in items)
                {
                    await repo.DeleteAsync(item);
                }
                //intermediate items - do this AFTER delete type defs
                foreach (var item in itemsIntermediate)
                {
                    await repo.DeleteAsync(item);
                }
                //commit changes
                await repo.SaveChangesAsync();

                //parent profiles
                foreach (var item in itemsProfile)
                { 
                    await repoProfile.DeleteAsync(item);
                }
                await repoProfile.SaveChangesAsync();
            }
        }
        #endregion

        /// <summary>
        /// do any post test cleanup here.
        /// </summary>
        /// <remarks>this will run after each test. So, if AddItem has 10 iterations of data, this will run once for each iteration.</remarks>
        public override void Dispose()
        {
            CleanupEntities().Wait();
        }

    }
}
