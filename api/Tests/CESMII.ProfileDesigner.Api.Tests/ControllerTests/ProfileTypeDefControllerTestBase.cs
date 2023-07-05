using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Repositories;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.Data.Contexts;
using CESMII.ProfileDesigner.Api.Shared.Models;

namespace CESMII.ProfileDesigner.Api.Tests.Int
{
    public class ProfileTypeDefControllerTestBase : ControllerTestBase
    {
        protected readonly ServiceProvider _serviceProvider;
        //for some tests, tie together a common guid so we can delete the created items at end of test. 
        protected Guid _guidCommon = Guid.NewGuid();

        #region API constants
        //protected const string URL_INIT = "/api/profiletypedefinition/init";
        protected const string URL_EXTEND = "/api/profiletypedefinition/extend";
        protected const string URL_ADD = "/api/profiletypedefinition/add";
        //protected const string URL_LIBRARY = "/api/profiletypedefinition/library";
        protected const string URL_GETBYID = "/api/profiletypedefinition/getbyid";
        //protected const string URL_DELETE = "/api/profiletypedefinition/delete";
        //protected const string URL_DELETE_MANY = "/api/profiletypedefinition/deletemany";
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
            //need to get user id of test user when we add profile
            services.AddScoped<IRepository<User>, BaseRepo<User, ProfileDesignerPgContext>>();
            
            _serviceProvider = services.BuildServiceProvider();
        }

#pragma warning disable xUnit1026  // Stop warnings related to parameters not used in test cases. 

        #region Helper Methods
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

        /// <summary>
        /// Create a parent profile and an entity to extend from. 
        /// </summary>
        /// <param name="guidCommon"></param>
        /// <returns></returns>
        protected async Task<ProfileTypeDefinition> InsertMockProfileAndExtendEntity(Guid guidCommon)
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
                var result = CreateEntity(0, profile.ID, null, guidCommon, Guid.NewGuid(), user);
                await repo.AddAsync(result);

                //assign profile to type def in case caller needs it.
                result.Profile = profile;
                
                return result;
            }
        }

        protected static ProfileTypeDefinitionModel CreateItemModel(int i, int? profileId, ProfileTypeDefinition parent, Guid guidCommon, Guid uuid, string cloudLibraryId = null)
        {
            var entity = CreateEntity(i, profileId, parent, guidCommon, uuid, null);
            return MapToModel(entity);
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
                    Newtonsoft.Json.JsonConvert.DeserializeObject<List<MetaTag>>(entity.MetaTags).Select(s => s.Name.Trim()).ToList(),
            };
        }

        /// <summary>
        /// This is used to create a row directly into DB. Bypasses everything except baseRepo
        /// </summary>
        /// <param name="i"></param>
        /// <param name="uuid"></param>
        /// <param name="creator"></param>
        /// <param name="cloudLibraryId"></param>
        /// <returns></returns>
        protected static ProfileTypeDefinition CreateEntity(int i, int? profileId, ProfileTypeDefinition parent, Guid guidCommon, Guid uuid, User user)
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
                ProfileTypeId = parent == null ? TYPE_ID_DEFAULT : parent?.ProfileTypeId,
                IsAbstract = i % 9 == 0,
                Created = dt,
                Updated = dt,
                AuthorId = user?.ID,
                OwnerId = user?.ID,
                CreatedById = user == null ? 0 : user.ID.Value,
                UpdatedById = user == null ? 0 : user.ID.Value,
                MetaTags = Newtonsoft.Json.JsonConvert.SerializeObject(tags),
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
                Keywords = new string[] { guidCommon.ToString() }
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

                //var repoUser = scope.ServiceProvider.GetService<IRepository<User>>();
                //var user = GetTestUser(repoUser);
                //var itemsAll = repo.FindByCondition(x => x.OwnerId.Equals(user.ID)).ToList();

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
                var repoProfile = scope.ServiceProvider.GetService<IRepository<Profile>>();
                var itemsProfile = repoProfile.FindByCondition(x =>
                    items.Select(y => y.ProfileId.Value).Contains(x.ID.Value))
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

                //works
                //var itemsIntermediate = repo.FindByCondition(x =>
                //    string.IsNullOrEmpty(x.SymbolicName) && ((ProfileItemTypeEnum)x.ProfileTypeId).Equals(ProfileItemTypeEnum.Object) 
                //    && (x.InstanceParentId.HasValue && items.Select(y => y.ID.Value).Contains(x.InstanceParentId.Value))
                //    )
                //    .ToList();

                //var itemsIntermediate = itemsIntermediate0.Where(x =>
                //    items.Select(y => y.ID.Value).Contains(x.InstanceParentId.Value)
                //    )
                //    .ToList();
                //var itemsIntermediate1 = itemsIntermediate0.Where(x =>
                //    (x.ParentId.HasValue && items.Select(y => y.ID.Value).Contains(x.ParentId.Value)) ||
                //    (x.InstanceParentId.HasValue && items.Select(y => y.ID.Value).Contains(x.InstanceParentId.Value))
                //    )
                //    .ToList();

                /*
                var itemsIntermediate = repo.FindByCondition(x =>
                    string.IsNullOrEmpty(x.SymbolicName) && ((ProfileItemTypeEnum)x.ProfileTypeId).Equals(ProfileItemTypeEnum.Object) &&
                    ((x.ParentId.HasValue && items.Select(y => y.ID.Value).Equals(x.ParentId.Value)) ||
                    (x.InstanceParentId.HasValue && items.Select(y => y.ID.Value).Equals(x.InstanceParentId.Value))) 
                    )
                    .ToList();
                */
                //type def analytics
                //var repoAnalytic = scope.ServiceProvider.GetService<IRepository<ProfileTypeDefinitionAnalytic>>();
                //order to account for some fk delete issues
                //var itemsAnalytic = repoAnalytic.FindByCondition(x =>
                //    items.Select(y => y.ID.Value).Contains(x.ProfileTypeDefinitionId))
                //    .ToList();
                //foreach (var a in itemsAnalytic)
                //{
                    //await repoAnalytic.DeleteAsync(a);
                //}
                //await repoAnalytic.SaveChangesAsync();

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
