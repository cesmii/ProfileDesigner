using CESMII.ProfileDesigner.Api.Shared.Controllers;
using CESMII.ProfileDesigner.Api.Shared.Extensions;
using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.Common;
using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.OpcUa;
using CESMII.OpcUa.NodeSetImporter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CESMII.ProfileDesigner.OpcUa.AASX;
using System.Text.RegularExpressions;
using Opc.Ua.Cloud.Library.Client;
using System.Globalization;
using System.Linq.Expressions;

namespace CESMII.ProfileDesigner.Api.Controllers
{
    [Authorize, Route("api/[controller]")]
    public class ProfileController : BaseController<ProfileController>
    {
        private readonly IDal<Profile, ProfileModel> _dal;
        private readonly ICloudLibDal<CloudLibProfileModel> _cloudLibDal;

        private readonly Utils.ImportService _svcImport;
        private readonly OpcUaImporter _exporter;

        public ProfileController(IDal<Profile, ProfileModel> dal,
            ICloudLibDal<CloudLibProfileModel> cloudLibDal,
            UserDAL dalUser,
            Utils.ImportService svcImport,
            OpcUaImporter exporter,
            ConfigUtil config, ILogger<ProfileController> logger)
            : base(config, logger, dalUser)
        {
            _dal = dal;
            _cloudLibDal = cloudLibDal;
            _svcImport = svcImport;
            _exporter = exporter;
        }

        [HttpPost, Route("GetByID")]
        [Authorize(Roles = "cesmii.profiledesigner.user")]
        //[ProducesResponseType(200, Type = typeof(NodeSetModel))]
        [ProducesResponseType(200, Type = typeof(ProfileTypeDefinitionModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByID([FromBody] IdIntModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            var result = _dal.GetById(model.ID, base.DalUserToken);
            if (result == null)
            {
                _logger.LogWarning($"ProfileController|GetById|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            return Ok(result);
        }


        /// <summary>
        /// Search my profiles library for profiles matching criteria passed in. This is a simple search field and 
        /// this will check against several profile fields and return results. 
        /// </summary>
        /// <remarks>Items in my profiles will have an author id</remarks>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Mine")]
        [Authorize(Roles = "cesmii.profiledesigner.user")]
        [ProducesResponseType(200, Type = typeof(DALResult<ProfileModel>))]
        public IActionResult GetMine([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("ProfileController|GetMine|Invalid model.");
                return BadRequest("Profile|GetMine|Invalid model");
            }

            if (string.IsNullOrEmpty(model.Query))
            {
                return Ok(_dal.Where(s => !s.StandardProfileID.HasValue /*&& s.AuthorId.HasValue && s.AuthorId.Value.Equals(userId)*/,
                                base.DalUserToken, model.Skip, model.Take, true));
            }

            model.Query = model.Query.ToLower();
            var result = _dal.Where(s =>
                            !s.StandardProfileID.HasValue && /*s.AuthorId.HasValue && s.AuthorId.Value.Equals(userId) &&*/
                            //string query section
                            (s.Namespace.ToLower().Contains(model.Query)
                            //|| (s.Author != null && (s.Author.FirstName.ToLower() + s.Author.LastName.ToLower()).Contains(
                            //    model.Query.Replace(" ", "").Replace("-", ""))) ||  //in case they search for code and name in one string.
                            ),
                            base.DalUserToken, model.Skip, model.Take, true);
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
        [Authorize(Roles = "cesmii.profiledesigner.user")]
        [ProducesResponseType(200, Type = typeof(DALResult<ProfileModel>))]
        public IActionResult GetLibrary([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("ProfileController|GetLibrary|Invalid model");
                return BadRequest("Profile|Library|Invalid model");
            }

            //NEW - return all items. The list is small so return both standard, referenced and mine in one list.
            if (string.IsNullOrEmpty(model.Query))
            {
                return Ok(_dal.GetAllPaged(base.DalUserToken, model.Skip, model.Take, true));
            }

            //search on some pre-determined fields
            model.Query = model.Query.ToLower();
            var result = _dal.Where(s =>
                            //string query section
                            s.Namespace.ToLower().Contains(model.Query),
                            base.DalUserToken, model.Skip, model.Take, true);
            return Ok(result);
        }

        /// <summary>
        /// Search profiles library for profiles matching criteria passed in. This is a simple search field and 
        /// this will check against several profile fields and return results. 
        /// </summary>
        /// <remarks>Items in profiles library will not have an author id</remarks>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("cloudlibrary")]
        [Authorize(Roles = "cesmii.profiledesigner.user")]
        [ProducesResponseType(200, Type = typeof(DALResult<CloudLibProfileModel>))]
        public async Task<IActionResult> GetCloudLibrary([FromBody] CloudLibFilterModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("ProfileController|GetLibrary|Invalid model");
                return BadRequest("Profile|Library|Invalid model");
            }
            if (model.Keywords != null && model.Keywords.Any(k => k == null))
            {
                _logger.LogWarning("ProfileController|GetLibrary|Invalid model: null keyword");
                return BadRequest("Profile|Library|Invalid model null keyword");
            }

            List<CloudLibProfileModel> result = new();

            // Get both local and cloudlib cursors
            var cursors = model.Cursor?.Split(",");
            string cloudLibCursor = cursors?[0] ?? null;
            if (cloudLibCursor == string.Empty)
            {
                cloudLibCursor = null;
            }
            string localCursor = cursors?[1] ?? "0";

            bool bFullResultCloud;
            bool bFullResultLocal = false;
            List<ProfileModel> allLocalProfiles = null;
            List<GraphQlNodeAndCursor<CloudLibProfileModel>> pendingCloudLibProfiles = new();
            List<ProfileModel> pendingLocalProfiles = null;
            int totalCount = 0;
            do
            {
                // Get first batch of profiles from the cloudlib
                var cloudResultTask = _cloudLibDal.Where(model.Take, cloudLibCursor, model.Keywords);

                // Get local profiles in parallel
                allLocalProfiles = allLocalProfiles ?? _dal.GetAll(base.DalUserToken);

                // Wait for the cloudlib query to finish
                var cloudResultPage = await cloudResultTask;
                if (cloudResultPage.Edges.Count > model.Take)
                {
                    _logger.LogWarning($"ProfileController|GetCloudLibrary|Received more profiles than requested: {result.Count}, expected {model.Take}.");
                }
                bFullResultCloud = !cloudResultPage.PageInfo.HasNextPage;
                totalCount = cloudResultPage.TotalCount;
                pendingCloudLibProfiles.AddRange(cloudResultPage.Edges);

                if (model.ExcludeLocalLibrary)
                {
                    // remove all profiles from the cloudlib result that are available locally: mark them as null for correct cursor handling of skipped profiles
                    foreach (var localProfile in allLocalProfiles)
                    {
                        var removedItemIndex = pendingCloudLibProfiles.FindLastIndex(p => p?.Node != null && p.Node.Namespace == localProfile.Namespace);
                        if (removedItemIndex >= 0)
                        {
                            pendingCloudLibProfiles[removedItemIndex].Node = null;
                            totalCount--;
                        }
                    }
                }

                if (model.AddLocalLibrary)
                {
                    if (pendingLocalProfiles == null)
                    {
                        pendingLocalProfiles = new();
                    }

                    // Query local profiles
                    if (model.Keywords != null)
                    {
                        if (!bFullResultLocal)
                        {
                            var orderBy = new List<OrderByExpression<Profile>> {
                                    new OrderByExpression<Profile>
                                    {
                                        Expression = p => p.Namespace,
                                        IsDescending = false,
                                    },
                                    new OrderByExpression<Profile>
                                    {
                                        Expression = p => p.PublishDate,
                                        IsDescending = false,
                                    },
                            };
                            if (model.ExcludeLocalLibrary)
                            {
                                // owned profiles first when excluding and adding
                                orderBy.Insert(0, new OrderByExpression<Profile>
                                {
                                    Expression = p => !p.AuthorId.HasValue || p.StandardProfileID.HasValue,
                                    IsDescending = false,
                                });
                            }

                            string keywordRegex = $".*({string.Join('|', model.Keywords)}).*";


                            var newLocalProfilesResult = _dal.Where(
                                new List<Expression<Func<Profile, bool>>>
                                {
                                    p => Regex.IsMatch(p.Namespace, keywordRegex, RegexOptions.IgnoreCase)
                                    // TODO better keyword search on ProfileTypeDefinitions etc. to match cloudlib's query
                                },
                                base.DalUserToken, int.Parse(localCursor), null, true,
                                orderByExpressions: orderBy.ToArray());
                            var newLocalProfiles = newLocalProfilesResult.Data;
                            bFullResultLocal = newLocalProfiles.Count == model.Take || newLocalProfiles.Count == 0;
                            pendingLocalProfiles.AddRange(newLocalProfiles);
                            totalCount += newLocalProfilesResult.Count; // TODO this is not correct in all scenarios (local profiles that are also in the cloudlib)
                        }
                    }
                    else
                    {
                        bFullResultLocal = true;
                        pendingLocalProfiles.AddRange(
                            (model.ExcludeLocalLibrary ?
                                allLocalProfiles.OrderBy(pm => pm.IsReadOnly).ThenBy(pm => pm.Namespace).ThenBy(pm => pm.PublishDate)// owned profiles first when excluding and adding
                                : allLocalProfiles.OrderBy(pm => pm.Namespace).ThenBy(pm => pm.PublishDate)
                            )
                            .Skip(int.Parse(localCursor)));
                        totalCount += allLocalProfiles.Count(p => string.IsNullOrEmpty(p.StandardProfile?.CloudLibraryId));
                    }
                }
                else
                {
                    bFullResultLocal = true;
                }
                // Process from start of local or cloud lists until we have enough results
                while (result.Count < model.Take
                    && (pendingLocalProfiles?.Any() == true || (pendingCloudLibProfiles.Any()))
                    && ((pendingLocalProfiles?.Any() == true || bFullResultLocal) || (pendingCloudLibProfiles.Any() || bFullResultCloud)))
                {
                    var firstPendingCloudLibProfile = pendingCloudLibProfiles.FirstOrDefault();
                    if (firstPendingCloudLibProfile != null && firstPendingCloudLibProfile.Node == null)
                    {
                        // Cloud profile was excluded earlier: skip it but remember it's cursor for the next query
                        pendingCloudLibProfiles.RemoveAt(0);
                        cloudLibCursor = firstPendingCloudLibProfile.Cursor;
                        continue;
                    }

                    var firstPendingLocalprofile = pendingLocalProfiles?.FirstOrDefault();
                    if (firstPendingLocalprofile != null &&
                        (model.ExcludeLocalLibrary || // Put local library items first if both add and exclude is specified
                          (firstPendingCloudLibProfile?.Node == null || String.Compare(firstPendingLocalprofile.Namespace, firstPendingCloudLibProfile.Node.Namespace, false, CultureInfo.InvariantCulture) < 0)))
                    {
                        result.Add(CloudLibProfileModel.MapFromProfile(firstPendingLocalprofile));
                        pendingLocalProfiles.RemoveAt(0);
                        localCursor = (int.Parse(localCursor) + 1).ToString();
                    }
                    else
                    {
                        if (firstPendingCloudLibProfile != null)
                        {
                            result.Add(firstPendingCloudLibProfile.Node);
                            cloudLibCursor = firstPendingCloudLibProfile.Cursor;
                            pendingCloudLibProfiles.RemoveAt(0);

                            if (firstPendingLocalprofile != null &&
                                firstPendingLocalprofile.Namespace == firstPendingCloudLibProfile.Node.Namespace
                                && firstPendingLocalprofile.PublishDate == firstPendingCloudLibProfile.Node.PublishDate)
                            {
                                // Skip matching local profile to avoid duplicates
                                pendingLocalProfiles.RemoveAt(0);
                                localCursor = (int.Parse(localCursor) + 1).ToString();
                            }
                        }
                    }
                }
            } while ((!bFullResultCloud || !bFullResultLocal) && result.Count < model.Take);

            // Fill in any local profile information for cloud library-only results
            foreach (var localProfile in allLocalProfiles ?? new List<ProfileModel>())
            {
                var cloudLibProfile = result.FirstOrDefault(p => p.Namespace == localProfile.Namespace);
                if (cloudLibProfile != null && cloudLibProfile.ID == null)
                {
                    // Add in local profile information
                    // TODO handle different publication dates between cloudlib and local library
                    cloudLibProfile.ID = localProfile.ID;
                    if (cloudLibProfile.StandardProfileID != localProfile.StandardProfileID)
                    {
                        // TODO: inconsistency in DB?
                    }
                    cloudLibProfile.StandardProfile = localProfile.StandardProfile;
                    cloudLibProfile.StandardProfileID = localProfile.StandardProfileID;
                    cloudLibProfile.AuthorId = localProfile.AuthorId;
                    cloudLibProfile.ImportWarnings = localProfile.ImportWarnings;
                    cloudLibProfile.NodeSetFiles = localProfile.NodeSetFiles;
                    cloudLibProfile.HasLocalProfile = true;
                }
            }
            foreach (var cloudProfile in result.Where(p => p.ID == null || p.ID == 0))
            {
                // Need better solution to avoid conflict with local IDs
                cloudProfile.ID = (int)long.Parse(cloudProfile.CloudLibraryId);
            }

            var lastCursor = $"{cloudLibCursor},{localCursor}";
            var dalResult = new DALResult<CloudLibProfileModel>
            {
                Count = totalCount,
                Data = result,
                Cursor = lastCursor,
            };
            return Ok(dalResult);
        }

        /// <summary>
        /// Search profiles library for profiles matching criteria passed in. This is a simple search field and 
        /// this will check against several profile fields and return results. 
        /// </summary>
        /// <remarks>Items in profiles library will not have an author id</remarks>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("cloudlibrary/import")]
        [Authorize(Roles = "cesmii.profiledesigner.user")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> ImportFromCloudLibrary([FromBody] IdStringModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("ProfileController|ImportFromCloudLibrary|Invalid model");
                return BadRequest("Profile|CloudLibrary||Import|Invalid model");
            }

            CloudLibProfileModel nodeSetToImport;
            try
            {
                nodeSetToImport = await _cloudLibDal.DownloadAsync(model.ID);
                if (nodeSetToImport == null)
                {
                    _logger.LogWarning($"ProfileController|ImportFromCloudLibrary|Did not find nodeset in Cloud Library: {model.ID}.");
                    return Ok(
                        new ResultMessageWithDataModel()
                        {
                            IsSuccess = false,
                            Message = "NodeSet not found in Cloud Library."
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ProfileController|ImportFromCloudLibrary|Failed to download from Cloud Library: {model.ID} {ex.Message}.");
                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = false,
                        Message = "Error downloading NodeSet from Cloud Library."
                    }
                );
            }

            var importModel = new ImportOPCModel
            {
                Data = nodeSetToImport.NodesetXml,
                FileName = nodeSetToImport.Namespace,
            };
            return await Import(new List<ImportOPCModel> { importModel });
        }



        /// <summary>
        /// Get an all profile count and a count of my profiles. 
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("Count")]
        [Authorize(Roles = "cesmii.profiledesigner.user")]
        [ProducesResponseType(200, Type = typeof(ProfileCountModel))]
        [ProducesResponseType(400)]
        public IActionResult GetCounts()
        {
            var all = _dal.Count(s => s.StandardProfileID.HasValue, base.DalUserToken);
            var mine = _dal.Count(s => !s.StandardProfileID.HasValue && s.AuthorId.HasValue && s.AuthorId.Value.Equals(DalUserToken.UserId), base.DalUserToken);
            return Ok(new ProfileCountModel() { All = all, Mine = mine });
        }

        /// <summary>
        /// Add a profile from the front end. This is different than importing a nodeset XML file. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Add")]
        [Authorize(Roles = "cesmii.profiledesigner.user")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Add([FromBody] ProfileModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileController|Add|Invalid model (null)");
                return BadRequest($"Invalid model (null). Check Publish Date formatting.");
            }
            //test for unique namespace/owner id/publish date combo
            if (!IsValidModel(model, base.DalUserToken))
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = "There is already a profile with this namespace and publish date combination. Enter a different namespace or publish date.",
                    Data = null
                });
            }

            //set some values server side 
            model.AuthorId = base.DalUserToken.UserId;
            model.NodeSetFiles = null;
            model.StandardProfileID = null;

            //re-validate
            ModelState.Clear();
            TryValidateModel(model);

            if (!ModelState.IsValid)
            {
                var errors = ExtractModelStateErrors();
                return BadRequest("The profile record is invalid. Please correct the following: " + errors.ToString());
            }

            var id = await _dal.AddAsync(model, base.DalUserToken);
            if (id < 0)
            {
                _logger.LogWarning($"ProfileController|Add|Could not add profile item.");
                return BadRequest("Could not add profile item.");
            }
            _logger.LogInformation($"ProfileController|Add|Added profile item. Id:{id}.");

            return Ok(new ResultMessageWithDataModel()
            {
                IsSuccess = true,
                Message = "Item was added.",
                Data = id
            });
        }

        /// <summary>
        /// Validate the profile model has unique name and publish date combination
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("validate")]
        [Authorize(Roles = "cesmii.profiledesigner.user")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public IActionResult ValidateModel([FromBody] ProfileModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileController|ValidateModel|Invalid model (null)");
                return BadRequest($"Invalid model (null). Check Publish Date formatting.");
            }

            //test for unique namespace/owner id/publish date combo
            if (!IsValidModel(model, base.DalUserToken))
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = "There is already a profile with this namespace and publish date combination. Enter a different namespace or publish date.",
                    Data = null
                });
            }

            //if we get here, passed
            return Ok(new ResultMessageWithDataModel()
            {
                IsSuccess = true,
                Message = "Valid",
                Data = null
            });
        }

        private bool IsValidModel(ProfileModel model, UserToken userToken)
        {
            //test for unique namespace/owner id/publish date combo, 0 count means all good
            return _dal.Count(x => x.Namespace.ToLower().Equals(model.Namespace.ToLower()) &&
                             x.OwnerId.HasValue && x.OwnerId.Value.Equals(LocalUser.ID) &&
                             (x.PublishDate == model.PublishDate)
                            , base.DalUserToken) == 0;
        }

        /// <summary>
        /// Update an existing nodeset that is maintained within this system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Update")]
        [Authorize(Roles = "cesmii.profiledesigner.user")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Update([FromBody] ProfileModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileController|Add|Invalid model (null)");
                return BadRequest($"Invalid model (null). Check Publish Date formatting.");
            }

            //test for unique namespace/owner id/publish date combo
            if (_dal.Count(x => !x.ID.Equals(model.ID) && x.Namespace.ToLower().Equals(model.Namespace.ToLower()) &&
                             x.OwnerId.HasValue && x.OwnerId.Value.Equals(LocalUser.ID)
                             && ((!model.PublishDate.HasValue && !x.PublishDate.HasValue)
                                    || (model.PublishDate.HasValue && x.PublishDate.HasValue && model.PublishDate.Value.Equals(x.PublishDate.Value)))
                            //&& (!x.PublishDate.HasValue ? new DateTime(0) : x.PublishDate.Value.Date).Equals(!model.PublishDate.HasValue ? new DateTime(0) : model.PublishDate.Value.Date)
                            , base.DalUserToken) > 0)
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = "There is already a profile with this namespace and publish date combination. Enter a different namespace or publish date.",
                    Data = null
                });
            }

            var item = _dal.GetById(model.ID.Value, base.DalUserToken);
            //can't update an item that is not yours
            if (!item.AuthorId.Equals(LocalUser.ID))
            {
                _logger.LogWarning($"ProfileController|Update|AuthorId {model.AuthorId} of item {model.ID} is different than User Id {LocalUser.ID} making update.");
                return BadRequest("Invalid operation. You cannot update a profile that you did not author");
            }

            //re-validate
            ModelState.Clear();
            TryValidateModel(model);

            if (!ModelState.IsValid)
            {
                var errors = ExtractModelStateErrors();
                return BadRequest("The profile record is invalid. Please correct the following: " + errors.ToString());
            }

            var result = await _dal.UpdateAsync(model, base.DalUserToken);
            if (result < 0)
            {
                _logger.LogWarning($"ProfileController|Update|Could not update profile item. Invalid id:{model.ID}.");
                return BadRequest("Could not update item. Invalid id.");
            }
            _logger.LogInformation($"ProfileController|Update|Updated item. Id:{model.ID}.");

            //TBD - come back to this. Race condition. timing error - issue with update not completing and then calling get
            //      issue is child item's virtual property is null unless we give it enough time to complete update process. 
            //return result object plus item.
            return Ok(new ResultMessageWithDataModel()
            {
                IsSuccess = true,
                Message = "Item was updated.",
                //Data = new JRaw(JsonConvert.SerializeObject(this.GetItem(model.ID))
                //Data = new JRaw(JsonConvert.SerializeObject(new IdIntModel() { ID = model.ID }))  //TBD - returning empty array - why?
                Data = model.ID
            });
        }

        /// <summary>
        /// Delete an existing nodeset. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Delete")]
        [Authorize(Roles = "cesmii.profiledesigner.user")]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Delete([FromBody] IdIntModel model)
        {
            //This also deletes all associated type defs, attributes, custom data types, compositions, interfaces 
            //associated with this profile
            try
            {
                var result = await _dal.DeleteAsync(model.ID, base.DalUserToken);
                if (result <= 0)
                {
                    _logger.LogWarning($"ProfileController|Delete|Could not delete item. Invalid id:{model.ID}.");
                    return BadRequest("Could not delete item. Invalid id.");
                }
                _logger.LogInformation($"ProfileController|Delete|Deleted item. Id:{model.ID}.");
            }
            catch (Npgsql.PostgresException eDB)
            {
                //trap foreign key error and let user know there is a dependency issue
                _logger.LogCritical($"ProfileController|Delete|Id:{model.ID}.", eDB);
                if (eDB.Message.ToLower().Contains("violates foreign key constraint"))
                {
                    return Ok(new ResultMessageModel()
                    {
                        IsSuccess = false,
                        Message = "This profile cannot be deleted because something else depends on it. Check that no other profiles nor type definitions depend on this profile or this profile's type definitions. "
                    });
                }
                //some other db issue
                return Ok(new ResultMessageModel()
                {
                    IsSuccess = false,
                    Message = "Please contact your system administrator."
                });
            }

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was deleted." });
        }

        internal int? DeleteInternalTestHook(IdIntModel model)
        {
            return _dal.DeleteAsync(model.ID, new UserToken { UserId = -1 }).Result;
        }

        /// <summary>
        /// Delete one or many nodesets. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("DeleteMany")]
        [Authorize(Roles = "cesmii.profiledesigner.user")]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> DeleteMany([FromBody] List<IdIntModel> model)
        {
            //check that this user is author of all the items they are trying to delete
            var ids = model.Select(x => x.ID).ToList();

            //This also deletes all associated type defs, attributes, custom data types, compositions, interfaces 
            //associated with this nodeset and its profiles
            try
            {
                var result = await _dal.DeleteManyAsync(model.Select(x => x.ID).ToList(), base.DalUserToken);
                if (result < 0)
                {
                    _logger.LogWarning($"ProfileController|DeleteMany|Could not delete item(s). Invalid model:{string.Join(", ", ids)}.");
                    return BadRequest("Could not delete item(s). Invalid id(s).");
                }
                else
                {
                    _logger.LogInformation($"ProfileController|Delete|Deleted nodeset item(s). Id:{string.Join(", ", model)}.");
                    //return success message object
                    return Ok(new ResultMessageModel() { IsSuccess = true, Message = model.Count == 1 ? "Item was deleted." : $"{model.Count} Items were deleted." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, ex.Message);
                return BadRequest("Could not delete item(s). Invalid id(s).");
            }
        }

        /// <summary>
        /// Flush the UA Cache 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Obsolete("Is this needed anymore?")]
        [HttpPost, Route("UAFlushCache")]
        [Authorize(Roles = "cesmii.profiledesigner.user")]
        [ProducesResponseType(200, Type = typeof(List<ProfileTypeDefinitionModel>))]
        public Task<IActionResult> UAFlushCache()
        {
            _logger.LogInformation($"ProfileController|Flush NodeSets Importer Cache. .");
            var myNodeSetCache = new UANodeSetFileCache();
            myNodeSetCache.FlushCache();
            //return success message object
            return Task.FromResult<IActionResult>(Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was deleted." }));
        }

        /// <summary>
        /// Import OPC UA nodeset uploaded by front end. There may be multiple files being uploaded. 
        /// </summary>
        /// <remarks>Non-standard nodesets are associated with the user doing the uploading. 
        /// Standard OPC UA nodesets will go into the library of nodesets visible to all.
        /// This method formerly named ImportMyOpcUaNodeSet.
        /// </remarks>
        /// <param name="nodeSetXmlList"></param>
        /// <returns>Return result model with an isSuccess indicator.</returns>
        [HttpPost, Route("Import")]
        [Authorize(Roles = "cesmii.profiledesigner.user")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Import([FromBody] List<ImportOPCModel> model /*, [FromServices] OpcUaImporter importer*/)
        {
            if (!ModelState.IsValid)
            {
                var errors = ExtractModelStateErrors();
                _logger.LogCritical($"ProfileController|Import|User Id:{LocalUser.ID}, Errors: {errors}");
                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = false,
                        Message = "The nodeset data is invalid."
                    }
                );
            }

            if (model == null || model.Count == 0)
            {
                _logger.LogWarning($"ProfileController|Import|No nodeset files to import. User Id:{LocalUser.ID}.");
                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = false,
                        Message = "No nodesets to import."
                    }
                );
            }

            _logger.LogInformation($"ProfileController|ImportMyOpcUaProfile|Importing {model.Count} nodeset files. User Id:{LocalUser.ID}.");

            //pass in the author id as current user
            //kick off background process, logid is returned immediately so front end can track progress...
            var logId = await _svcImport.ImportOpcUaNodeSet(model, base.DalUserToken);

            return Ok(
                new ResultMessageWithDataModel()
                {
                    IsSuccess = true,
                    Message = "Import is processing...",
                    Data = logId
                }
            );
        }


        /// <summary>
        /// Exports all type definitions in a profile 
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Returns the OPC UA models in XML format</returns>
        [HttpPost, Route("Export")]
        [Authorize(Roles = "cesmii.profiledesigner.user")]
        [ProducesResponseType(200, Type = typeof(ResultMessageExportModel))]
        public Task<IActionResult> Export([FromBody] ExportRequestModel model)
        {
            var userToken = DalUserToken;

            //get profile to export
            var sw = Stopwatch.StartNew();
            _logger.LogTrace("Starting export");
            var item = _dal.GetById(model.ID, base.DalUserToken);
            if (item == null)
            {
                //return Task.FromResult(new ResultMessageWithDataModel()
                return Task.FromResult<IActionResult>(Ok(new ResultMessageExportModel()
                {
                    IsSuccess = false,
                    Message = "Profile not found."
                }
                ));
            }

            // Populate the OPC model into a new importer instance
            try
            {
                string result = null;

                _logger.LogTrace($"Timestamp||Export||Starting: {sw.Elapsed}");
                bool bIncludeRequiredModels = model.Format?.ToUpper() == "AASX";
                var exportedNodeSets = _exporter.ExportNodeSet(item, base.DalUserToken, null, bIncludeRequiredModels, model.ForceReexport);
                if (exportedNodeSets != null)
                {
                    if (model.Format?.ToUpper() == "AASX")
                    {
                        var aasxPackage = AASXGenerator.GenerateAAS(exportedNodeSets);
                        if (aasxPackage != null)
                        {
                            return Task.FromResult<IActionResult>(Ok(new ResultMessageExportModel()
                            {
                                IsSuccess = true,
                                Message = "",
                                Data = aasxPackage,
                                Warnings = item.ImportWarnings.Select(x => x.Message).ToList()
                            }));
                        }
                    }
                    else
                    {
                        _logger.LogTrace($"Timestamp||Export||Nodeset Stream generated: {sw.Elapsed}");
                        result = (string)exportedNodeSets.FirstOrDefault().xml;
                        _logger.LogTrace($"Timestamp||Export||Data Converted to Response: {sw.Elapsed}");
                        //TBD - read and include the required models in a ZIP file, optionally?
                        //TBD - get the warnings that were logged on import and publish them here. 
                    }
                }
                else
                {
                    return Task.FromResult<IActionResult>(Ok(new ResultMessageExportModel()
                    {
                        IsSuccess = false,
                        Message = "An error occurred downloading the profile."
                    }));
                }

                return Task.FromResult<IActionResult>(Ok(new ResultMessageExportModel()
                {
                    IsSuccess = true,
                    Message = "",
                    Data = result,
                    Warnings = item.ImportWarnings.Select(x => x.Message).ToList()
                }));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"ProfileController|Export|Failed:{ex.Message}");
                _logger.LogTrace($"Timestamp||Export||Error: {sw.Elapsed}|| {ex.Message}");
                return Task.FromResult<IActionResult>(Ok(new ResultMessageExportModel()
                {
                    IsSuccess = false,
                    Message = $"Technical details: {ex.Message}"
                }));
            }
        }
    }
}
