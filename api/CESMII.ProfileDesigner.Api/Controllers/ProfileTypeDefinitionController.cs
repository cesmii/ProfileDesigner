using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using CESMII.ProfileDesigner.Api.Shared.Extensions;
using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.Api.Shared.Controllers;
using CESMII.ProfileDesigner.Common;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.Data.Extensions;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.DAL.Utils;
using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.OpcUa;

namespace CESMII.ProfileDesigner.Api.Controllers
{

    [Authorize(Policy = nameof(PermissionEnum.UserAzureADMapped)), Route("api/[controller]")]
    public class ProfileTypeDefinitionController : BaseController<ProfileTypeDefinitionController>
    {
        private readonly IDal<ProfileTypeDefinition, ProfileTypeDefinitionModel> _dal;
        private readonly IDal<ProfileTypeDefinitionAnalytic, ProfileTypeDefinitionAnalyticModel> _dalAnalytics;
        private readonly ProfileMapperUtil _profileUtils;
        private readonly IDal<Profile, ProfileModel> _dalProfile;

        public ProfileTypeDefinitionController(IDal<ProfileTypeDefinition, ProfileTypeDefinitionModel> dal,
            IDal<ProfileTypeDefinitionAnalytic, ProfileTypeDefinitionAnalyticModel> dalAnalytics,
            IDal<Profile, ProfileModel> dalProfile,
            UserDAL dalUser, 
            ConfigUtil config, ProfileMapperUtil profileUtils, ILogger<ProfileTypeDefinitionController> logger)
            : base(config, logger, dalUser)
        {
            _dal = dal;
            _dalAnalytics = dalAnalytics;
            _dalProfile = dalProfile;
            _profileUtils = profileUtils;
        }

        [HttpPost, Route("GetByID")]
        [ProducesResponseType(200, Type = typeof(ProfileTypeDefinitionModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetByID([FromBody] IdIntModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileTypeDefinitionController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            //  FIX: 
            //  Error:A second operation was started on this context instance before a previous operation completed.
            //  This is usually caused by different threads concurrently using the same instance of DbContext. For more information on how to avoid threading issues with DbContext, see https://go.microsoft.com/fwlink/?linkid=2097913."
            //  Approach is to add await to getItem calls to ensure this completes prior to starting analytics calls. 
            var result = await this.GetItem(model.ID);
            if (result == null)
            {
                _logger.LogWarning($"ProfileTypeDefinitionController|GetById|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            try
            {
                //increment page visit count for this item
                var analytic = _dalAnalytics.Where(x => x.ProfileTypeDefinitionId == model.ID, base.DalUserToken, null, null, false).Data.FirstOrDefault();
                if (analytic == null)
                {
                    await _dalAnalytics.AddAsync(new ProfileTypeDefinitionAnalyticModel() { ProfileTypeDefinitionId = model.ID, PageVisitCount = 1 }, base.DalUserToken);
                }
                else
                {
                    analytic.PageVisitCount += 1;
                    await _dalAnalytics.UpdateAsync(analytic, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error saving analytics data on GetByID call");
            }
            return Ok(result);
        }


        /// <summary>
        /// Search profiles library for profiles matching criteria passed in. This is a simple search field and 
        /// this will check against several profile fields and return results. 
        /// </summary>
        /// <remarks>Items in profiles library will not have an author id</remarks>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("library")]
        [ProducesResponseType(200, Type = typeof(DALResult<ProfileTypeDefinitionModel>))]
        public IActionResult GetLibrary([FromBody] ProfileTypeDefFilterModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("ProfileTypeDefinitionController|GetLibrary|Invalid model");
                return BadRequest("ProfileTypeDefinitionController|Library|Invalid model");
            }

            //search on some pre-determined fields
            var orderByExprs = _profileUtils.BuildSearchOrderByExpressions(LocalUser.ID.Value, model.SortByEnum);
            var result = _dal.Where(BuildPredicate(model, base.DalUserToken), base.DalUserToken, model.Skip, model.Take, true, false, orderByExprs.ToArray());

            //TBD - come back to this - 
            //This is used when user clicks on View Type Defs for a single profile. 
            //Add support to return list or profiles if the front end begins to support filtering by multiple profiles
            var profileCategory = model.Filters?.Find(c => c.ID.Value == (int)SearchCriteriaCategoryEnum.Profile);
            var profileFilters = profileCategory?.Items.Where(x => x.Selected).Select(x => x.ID.Value).ToList();

            return Ok(new ProfileTypeDefinitionSearchResult<ProfileTypeDefinitionModel>()
            {
                Count = result.Count,
                Data = result.Data,
                SummaryData = result.SummaryData,
                //return the profiles used in the filter -null, one or many
                Profiles = profileFilters == null ? null : 
                    _dalProfile.Where(x => profileFilters.Contains(x.ID.Value), base.DalUserToken).Data
            });
        }

        private List<Expression<Func<ProfileTypeDefinition, bool>>> BuildPredicate(ProfileTypeDefFilterModel model, UserToken userToken)
        {
            model.Query = string.IsNullOrEmpty(model.Query) ? null : model.Query.ToLower();

            //init 
            var result = new List<Expression<Func<ProfileTypeDefinition, bool>>>
            {
                //Build collection of expressions - various parts depend on existence of values incoming in the model.
                //Dal will loop over predicates and call query = query.where(predicate) which will 
                //create AND between each predicate

                //Part 0 - Always exclude some types that are behind the scenes type
                x => !ProfileMapperUtil.ExcludedProfileTypes.Contains(x.ProfileTypeId)
            };

            //Part 0 - string contains
            if (!string.IsNullOrEmpty(model.Query))
            {
                result.Add(x => x.Name.ToLower().Contains(model.Query) ||
                         x.Description.ToLower().Contains(model.Query) ||
                        (x.Profile != null && x.Profile.Namespace.ToLower().Contains(model.Query)) ||
                        (x.Author != null && x.Author.DisplayName.ToLower().Contains(model.Query)) ||
                        (x.ExternalAuthor != null && x.ExternalAuthor.ToLower().Contains(model.Query)) ||
                         x.MetaTags.ToLower().Contains(model.Query));
            }

            //Part 1 - Mine OR Popular - This will be an OR clause within this portion
            //TBD - weave in popular with author
            var filterAuthors = model.Filters?.Find(c => c.ID.Value == (int)SearchCriteriaCategoryEnum.Author)?
                .Items?.Where(x => x.Selected).ToList();
            if (filterAuthors!= null && filterAuthors.Any())
            {
                Expression<Func<ProfileTypeDefinition, bool>> predAuthor = null;
                foreach (var filterAuthor in filterAuthors)
                {
                    Expression<Func<ProfileTypeDefinition, bool>> fnz = x => string.IsNullOrEmpty(x.Profile.CloudLibraryId) 
                        && x.Profile.AuthorId.Value.Equals(filterAuthor.ID.Value);
                    predAuthor = predAuthor.OrExtension(fnz);
                }
                //append to predicate list
                result.Add(predAuthor);
            }

            //Part 1a - Filter on popular
            var filtersPopular = model.Filters?.Find(c => c.ID.Value == (int)SearchCriteriaCategoryEnum.Popular)?
                .Items?.Where(x => x.Selected).ToList();
            if (filtersPopular != null && filtersPopular.Any())
            {
                //for popular, there is only one item in collection so we don't loop over filtersPopular 
                //get list of type defs we characterize as popular - top 30
                var popularProfiles = _profileUtils.GetPopularItems(userToken);

                Expression<Func<ProfileTypeDefinition, bool>> predPopular = null;
                Expression<Func<ProfileTypeDefinition, bool>> fnz = x => popularProfiles.Any(z => z.Equals(x.ID.Value));
                predPopular = predPopular.OrExtension(fnz);

                //append to predicate list
                result.Add(predPopular);
            }

            //Part 2 - Filter on Profile - Typedefs associated with a specific profile - none, one or many
            var filterProfiles = model.Filters?.Find(c => c.ID.Value == (int)SearchCriteriaCategoryEnum.Profile)?
                .Items?.Where(x => x.Selected).ToList();
            if (filterProfiles != null && filterProfiles.Any())
            {
                Expression<Func<ProfileTypeDefinition, bool>> predProfile = null;
                foreach (var filterProfile in filterProfiles)
                {
                    Expression<Func<ProfileTypeDefinition, bool>> fnz = x => x.ProfileId.Value.Equals(filterProfile.ID.Value);
                    predProfile = predProfile.OrExtension(fnz);
                }
                //append to predicate list
                result.Add(predProfile);
            }

            //Part 3 - Filter on typeDef types (object, variable type, structure, enumeration) associated with a specific profile
            var filterTypes = model.Filters?.Find(c => c.ID.Value == (int)SearchCriteriaCategoryEnum.TypeDefinitionType)?
                .Items?.Where(x => x.Selected).ToList();
            if (filterTypes != null && filterTypes.Any())
            {
                Expression<Func<ProfileTypeDefinition, bool>> predTypeId = null;
                foreach (var filterType in filterTypes)
                {
                    Expression<Func<ProfileTypeDefinition, bool>> fnz = x => x.ProfileTypeId.Value.Equals(filterType.ID.Value);
                    predTypeId = predTypeId.OrExtension(fnz);
                }
                result.Add(predTypeId);

            }
            //append to predicate list

            return result;
        }

        /// <summary>
        /// When modifying profile attrs, the user can point this profile to another profile. 
        /// However, we can't point to another profile if the other profile is a parent, grandparent, etc. 
        /// We can't point to a profile that depends on us. 
        /// An interface profile can only point to other interface profiles.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("lookup/profilerelated")]
        [ProducesResponseType(200, Type = typeof(ProfileLookupModel))]
        [ProducesResponseType(400)]
        public IActionResult LookupProfileRelated([FromBody] IdIntModel model)
        {
            //for a new profile being created from front end, the profile won't have an id yet.
            if (model == null || model.ID <= 0)
            {
                return Ok(new ProfileLookupModel());
            }

            var typeDef = _dal.GetById(model.ID, base.DalUserToken);
            var result = new ProfileLookupModel
            {
                Compositions = _profileUtils.BuildCompositionLookup(base.DalUserToken),
                Interfaces = _profileUtils.BuildInterfaceLookup(typeDef, base.DalUserToken),
                VariableTypes = _profileUtils.BuildVariableTypeLookup(base.DalUserToken),
            };
            return Ok(result);
        }

        /// <summary>
        /// When modifying profile attrs, the user can point this profile to another profile. 
        /// However, we can't point to another profile if the other profile is a parent, grandparent, etc. 
        /// We can't point to a profile that depends on us. 
        /// An interface profile can only point to other interface profiles.
        /// </summary>
        /// <remarks>In Extend scenario, we get the parent id's ancestory info and skip dependencies. The id is the parent's id.</remarks>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("lookup/profilerelated/extend")]
        [ProducesResponseType(200, Type = typeof(ProfileLookupModel))]
        [ProducesResponseType(400)]
        public IActionResult LookupProfileRelatedExtend([FromBody] IdIntModel model)
        {
            var result = new ProfileLookupModel
            {
                Compositions = _profileUtils.BuildCompositionLookup(base.DalUserToken),
                Interfaces = _profileUtils.BuildInterfaceLookup(null, base.DalUserToken),
                VariableTypes = _profileUtils.BuildVariableTypeLookup(base.DalUserToken),
            };
            return Ok(result);
        }

        /// <summary>
        /// Get an all profile type def count and a count of my profile type defs. 
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("Count")]
        [ProducesResponseType(200, Type = typeof(ProfileCountModel))]
        [ProducesResponseType(400)]
        public IActionResult GetCounts()
        {
            var all = _dal.Count(p => p.Author == null && !ProfileMapperUtil.ExcludedProfileTypes.Contains(p.ProfileTypeId), base.DalUserToken); 
            var mine = _dal.Count(p => p.Author != null && p.Author.ID.Equals(DalUserToken.UserId) && !ProfileMapperUtil.ExcludedProfileTypes.Contains(p.ProfileTypeId), base.DalUserToken); 
            return Ok(new ProfileCountModel() { All = all, Mine = mine });
        }

        /// <summary>
        /// Get all related data for this profile to show in an explorer type view. Get dependencies, interfaces, inheritance tree, compositions 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Explorer")]
        [ProducesResponseType(200, Type = typeof(ProfileExplorerModel))]
        [ProducesResponseType(400)]
        public IActionResult GetProfileExplorer([FromBody] IdIntModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileTypeDefinitionController|GetProfileExplorer|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var typeDef = _dal.GetById(model.ID, base.DalUserToken);

            if (typeDef == null)
            {
                _logger.LogWarning($"ProfileTypeDefinitionController|GetProfileExplorer|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            //Build the explorer...
            var dependencies = _profileUtils.GenerateDependencies(typeDef, base.DalUserToken);

            var treeview = _profileUtils.GenerateAncestoryTree(typeDef, base.DalUserToken, true);

            // note interfaces, compositions already accounted for in profile object
            var result = new ProfileExplorerModel()
            {
                TypeDefinition = typeDef,
                Dependencies = dependencies,
                Tree = treeview
            };

            return Ok(result);
        }

        /// <summary>
        /// Load a collection of favorites (for this owner). 
        /// </summary>
        /// <remarks>Items in profiles library will not have an author id</remarks>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet, Route("lookup/favorites")]
        [ProducesResponseType(200, Type = typeof(DALResult<ProfileTypeDefinitionModel>))]
        public IActionResult LookupFavorites()
        {
            //search on some pre-determined fields
            var result = _dal.Where(x => x.Favorite != null && x.Favorite.IsFavorite, base.DalUserToken, null, null, false, false);

            return Ok(result);
        }

        /// <summary>
        /// Get an existing profile as the starting point for extending to a new profile. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Extend")]
        [ProducesResponseType(200, Type = typeof(ProfileTypeDefinitionModel))]
        [ProducesResponseType(400)]
        public IActionResult Extend([FromBody] IdIntModel model)
        {
            var parent = _dal.GetById(model.ID, base.DalUserToken);
            if (parent == null)
            {
                _logger.LogWarning($"ProfileTypeDefinitionController|Extend|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            var result = ExtendTypeDefinitionInternal(parent, null, true, base.DalUserToken);

            return Ok(result);
        }

        /// <summary>
        /// Get an existing profile as the starting point for extending to a new profile. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Wizard/Extend")]
        [ProducesResponseType(200, Type = typeof(ProfileTypeDefinitionModel))]
        [ProducesResponseType(400)]
        public IActionResult ExtendWizard([FromBody] ProfileTypeDefinitionWizardExtendModel model)
        {
            var parent = _dal.GetById(model.ID, base.DalUserToken);
            if (parent == null)
            {
                _logger.LogWarning($"ProfileTypeDefinitionController|ExtendWizard|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }

            var result = ExtendTypeDefinitionInternal(parent, model.ProfileId, true, base.DalUserToken);

            return Ok(result);
        }

        /// <summary>
        /// Init a new profile. The returned model is a properly constructed starting point. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet, Route("Init")]
        [ProducesResponseType(200, Type = typeof(ProfileTypeDefinitionModel))]
        [ProducesResponseType(400)]
        public IActionResult InitProfileTypeDefinition()
        {
            //TBD - move search condition to config file
            //TBD - also include opc node id as qualifier?
            //always extend from baseObjectType. If null, throw exception.
            var matches = _dal.Where(x => x.Name.ToLower().Equals("baseobjecttype"), base.DalUserToken, null, null, false, true).Data;
            if (matches == null || matches.Count == 0)
            {
                _logger.LogWarning($"ProfileTypeDefinitionController|InitProfileTypeDefinition|BaseObjectType not found");
                return BadRequest($"BaseObjectType not found");
            }
            else if (matches.Count > 1)
            {
                _logger.LogWarning($"ProfileTypeDefinitionController|InitProfileTypeDefinition|Multiple items with name BaseObjectType found");
                return BadRequest($"Multiple items with name BaseObjectType found");
            }

            var result = ExtendTypeDefinitionInternal(matches[0], null, false, base.DalUserToken);
            //reset the type id so the user can select type.
            result.Type = null;
            result.TypeId = -1;

            return Ok(result);
        }

        private ProfileTypeDefinitionModel ExtendTypeDefinitionInternal(ProfileTypeDefinitionModel parent, int? profileId, bool isExtend, UserToken userToken)
        {
            //used to populate author info
            var user = _dalUser.GetById(userToken.UserId, base.DalUserToken);

            //update the new profile to clean out and update some key parts of the item
            //make clean copy of parent and then remove some lingering parent data items
            var result = JsonConvert.DeserializeObject<ProfileTypeDefinitionModel>(JsonConvert.SerializeObject(parent));
            result.ID = null;
            result.OpcNodeId = null;
            result.BrowseName = null;
            result.SymbolicName = null;
            result.DocumentUrl = null;
            result.Name = isExtend ? "[Extend]" : "[New]"; //name useful in ancestory tree. After that is built, we clear name.
            result.Parent = ProfileMapperUtil.MapToModelProfileSimple(parent);
            result.AuthorId = LocalUser.ID;
            result.Author = new UserSimpleModel()
            {
                ID = user.ID,
                ObjectIdAAD = LocalUser.ObjectIdAAD,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Organization = user.Organization
            };
            result.CreatedBy = result.Author;
            result.Created = DateTime.UtcNow;
            result.UpdatedBy = result.Author;
            result.Updated = DateTime.UtcNow;
            //return dependencies, ancestory for this profile as part of this response to reduce volume of calls for a profile. 
            result.Dependencies = new List<ProfileTypeDefinitionSimpleModel>();
            result.Ancestory = _profileUtils.GenerateAncestoryLineage(result, base.DalUserToken);
            result.ProfileAttributes = new List<ProfileAttributeModel>();
            result.Interfaces = new List<ProfileTypeDefinitionModel>();
            result.Compositions = new List<ProfileTypeDefinitionRelatedModel>();
            result.ExtendedProfileAttributes = _profileUtils.GetExtendedAttributes(result, base.DalUserToken);

            //clear name, profile fk before returning...
            result.Name = "";
            result.Description = "";
            result.IsFavorite = false;

            //for normal extend, new, profileId is null. For wizard, profile Id will be selected by user in upstream step
            if (profileId.HasValue)
            {
                result.Profile = _dalProfile.GetById(profileId.Value,userToken);
                result.ProfileId = profileId;
            }
            else
            {
                result.Profile = null;
                result.ProfileId = 0;
            }

            return result;
        }

        /// <summary>
        /// Add a profile type def from the front end. This is different than importing a nodeset XML file. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Add")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Add([FromBody] ProfileTypeDefinitionModel model)
        {
            return await UpdateInternal(model, true);
        }


        /// <summary>
        /// Update an existing profile type def that is maintained within this system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Update")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Update([FromBody] ProfileTypeDefinitionModel model)
        {
            return await UpdateInternal(model, false);
        }

        /// <summary>
        /// Toggle favorite status on an item.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("togglefavorite")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> ToggleFavorite([FromBody] IdIntModel model)
        {
            var item = _dal.GetById(model.ID, base.DalUserToken);
            item.IsFavorite = !item.IsFavorite;
            return await UpdateInternal(item, false);
        }

        /// <summary>
        /// Add or update a profile type def from the front end. This is different than importing a nodeset XML file. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<IActionResult> UpdateInternal(ProfileTypeDefinitionModel model, bool isAdd)
        {
            //For front end, set Profile to null to ensure that the DAL does not see this as a changed object that must be updated.
            //Setting ProfileId will suffice.
            if (model.ProfileId.HasValue && model.ProfileId.Value > 0)
            {
                model.Profile = null;
            }
            if (isAdd)
            {
                model.ID = 0; //satisfy required field and set to 0 will indicate new item 
            }
            //clean up stuff - set new ids to 0 rather than -1 for server side handling
            if (model.ProfileAttributes != null)
            {
                foreach (var attrib in model.ProfileAttributes)
                {
                    if (attrib.TypeDefinitionId <= 0) attrib.TypeDefinitionId = model.ID;
                    if (attrib.ID < 0) attrib.ID = null;
                    if (attrib.CompositionId < 0) attrib.CompositionId = null;
                }
                //on front end, all attributes stored in single collection.
                // split out compositions, primitive attributes
                // into individual lists for DAL, DB.
                model.Attributes = model.ProfileAttributes.Where(a => (!a.CompositionId.HasValue || a.CompositionId < 0)).ToList();
                var comps = model.ProfileAttributes.Where(a => a.CompositionId.HasValue && a.CompositionId >= 0).ToList();
                model.Compositions = _profileUtils.MapProfileAttributeToCompositionModels(comps);
            }

            //clear out stuff unrelated to the save of the item. There is lots of related stuff that we use in the display
            //that will trip up validation but is not central to the profile save operation.
            model.Ancestory = null;
            model.TypeId = model.Type.ID;

            //re-validate
            ModelState.Clear();
            TryValidateModel(model);

            //required
            if (!model.ProfileId.HasValue)
            {
                ModelState.AddModelError("Profile", "Profile type definition must be assigned to a profile");
            }

            if (!ModelState.IsValid)
            {
                var errors = ExtractModelStateErrors();
                return BadRequest("The item is invalid. Please correct the following: " + errors.ToString());
            }

            int? id = 0;
            if (isAdd)
            {
                id = await _dal.AddAsync(model, base.DalUserToken);

                //increment extend count for this item's parent
                var analytic = _dalAnalytics.Where(x => x.ProfileTypeDefinitionId == model.Parent.ID, base.DalUserToken, null, null, false).Data.FirstOrDefault();
                if (analytic == null)
                {
                    await _dalAnalytics.AddAsync(new ProfileTypeDefinitionAnalyticModel() { ProfileTypeDefinitionId = model.Parent.ID.Value, ExtendCount = 1 }, base.DalUserToken);
                }
                else
                {
                    analytic.ExtendCount += 1;
                    await _dalAnalytics.UpdateAsync(analytic, null);
                }


            }
            else
            {
                id = await _dal.UpdateAsync(model, base.DalUserToken);
            }

            if (id < 0)
            {
                _logger.LogWarning($"ProfileTypeDefinitionController|UpdateInternal|Could not {(isAdd ? "add" : "update")} profile type definition.");
                return BadRequest($"Could not {(isAdd ? "add" : "update")} profile type definition.");
            }
            _logger.LogInformation($"ProfileTypeDefinitionController|UpdateInternal|{(isAdd ? "Added" : "Updated")} profile type definition. Id:{id}.");

            //return result object plus id.
            return Ok(new ResultMessageWithDataModel()
            {
                IsSuccess = true,
                Message = $"Item was {(isAdd ? "added" : "updated")}.",
                //Data = new JRaw(JsonConvert.SerializeObject(this.GetItem(model.ID))
                //Data = new JRaw(JsonConvert.SerializeObject(new IdIntModel() { ID = id }))
                Data = id
            });
        }

        /// <summary>
        /// Delete an existing profile type def. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Delete")]
        [ProducesResponseType(200, Type = typeof(List<ProfileTypeDefinitionModel>))]
        public async Task<IActionResult> Delete([FromBody] IdIntModel model)
        {
            //check for dependencies - if other profile types depend on this, it cannot be deleted. 
            var item = await GetItem(model.ID);
            if (item.Dependencies != null && item.Dependencies.Count > 0)
            {
                _logger.LogWarning($"ProfileTypeDefinitionController|Delete|Could not delete type definition because other type definitions depend on it. Id:{model.ID}.");
                    return Ok(new ResultMessageModel() { IsSuccess = false, Message = "This item cannot be deleted because other type definitions depend on this item." });
            }


            var result = await _dal.DeleteAsync(model.ID, base.DalUserToken);
            if (result < 0)
            {
                _logger.LogWarning($"ProfileTypeDefinitionController|Delete|Could not delete profile type definition. Invalid id:{model.ID}.");
                return BadRequest("Could not delete item. Invalid id.");
            }
            _logger.LogInformation($"ProfileTypeDefinitionController|Delete|Deleted profile type definition. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was deleted." });
        }

        private async Task<ProfileTypeDefinitionModel>  GetItem(int id)
        {
            var result = await _dal.GetByIdAsync(id, base.DalUserToken);
            if (result == null) return null;
            //return dependencies, ancestory for this profile as part of this response to reduce volume of calls for a profile. 
            result.Dependencies = _profileUtils.GenerateDependencies(result, base.DalUserToken);
            result.Ancestory = _profileUtils.GenerateAncestoryLineage(result, base.DalUserToken);
            //pull extended attributes from ancestory AND interface attributes
            result.ExtendedProfileAttributes = _profileUtils.GetExtendedAttributes(result, base.DalUserToken);
            //merge profile attributes, compositions, variable types
            result.ProfileAttributes = _profileUtils.MergeProfileAttributes(result);
            //reduce size of returned object and clear out individual collections
            result.Attributes = null;
            result.Compositions = null;
            return result;
        }

    }
}


