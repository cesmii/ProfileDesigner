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

namespace CESMII.ProfileDesigner.Api.Controllers
{
    [Authorize, Route("api/[controller]")]
    public class ProfileController : BaseController<ProfileController>
    {
        private readonly IDal<Profile, ProfileModel> _dal;
        private readonly Utils.ImportService _svcImport;
        private readonly OpcUaImporter _exporter;

        public ProfileController(IDal<Profile, ProfileModel> dal,
            Utils.ImportService svcImport,
            OpcUaImporter exporter,
            ConfigUtil config, ILogger<ProfileController> logger)
            : base(config, logger)
        {
            _dal = dal;
            _svcImport = svcImport;
            _exporter = exporter;
        }

        [HttpPost, Route("GetByID")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageProfile))]
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
            var userToken = UserExtension.DalUserToken(User);

            var result = _dal.GetById(model.ID, userToken);
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
        [Authorize(Policy = nameof(PermissionEnum.CanViewProfile))]
        [ProducesResponseType(200, Type = typeof(DALResult<ProfileModel>))]
        public IActionResult GetMine([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("ProfileController|GetMine|Invalid model.");
                return BadRequest("Profile|GetMine|Invalid model");
            }

            var userId = new UserToken { UserId = User.GetUserID() };
            if (string.IsNullOrEmpty(model.Query))
            {
                return Ok(_dal.Where(s => !s.StandardProfileID.HasValue /*&& s.AuthorId.HasValue && s.AuthorId.Value.Equals(userId)*/,
                                userId, model.Skip, model.Take, true));
            }

            model.Query = model.Query.ToLower();
            var result = _dal.Where(s =>
                            !s.StandardProfileID.HasValue && /*s.AuthorId.HasValue && s.AuthorId.Value.Equals(userId) &&*/
                            //string query section
                            (s.Namespace.ToLower().Contains(model.Query)
                            //|| (s.Author != null && (s.Author.FirstName.ToLower() + s.Author.LastName.ToLower()).Contains(
                            //    model.Query.Replace(" ", "").Replace("-", ""))) ||  //in case they search for code and name in one string.
                            ),
                            userId, model.Skip, model.Take, true);
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
        [Authorize(Policy = nameof(PermissionEnum.CanViewProfile))]
        [ProducesResponseType(200, Type = typeof(DALResult<ProfileModel>))]
        public IActionResult GetLibrary([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("ProfileController|GetLibrary|Invalid model");
                return BadRequest("Profile|Library|Invalid model");
            }

            //NEW - return all items. The list is small so return both standard, referenced and mine in one list.
            var userId = new UserToken { UserId = User.GetUserID() };
            if (string.IsNullOrEmpty(model.Query))
            {
                return Ok(_dal.GetAllPaged(userId, model.Skip, model.Take, true));
            }

            //search on some pre-determined fields
            model.Query = model.Query.ToLower();
            var result = _dal.Where(s =>
                            //string query section
                            s.Namespace.ToLower().Contains(model.Query),
                            userId, model.Skip, model.Take, true);
            return Ok(result);
        }

        /// <summary>
        /// Get an all profile count and a count of my profiles. 
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("Count")]
        [Authorize(Policy = nameof(PermissionEnum.CanViewProfile))]
        [ProducesResponseType(200, Type = typeof(ProfileCountModel))]
        [ProducesResponseType(400)]
        public IActionResult GetCounts()
        {
            var userToken = UserExtension.DalUserToken(User);
            var all = _dal.Count(s => s.StandardProfileID.HasValue, userToken);
            var mine = _dal.Count(s => !s.StandardProfileID.HasValue && s.AuthorId.HasValue && s.AuthorId.Value.Equals(userToken.UserId), userToken);
            return Ok(new ProfileCountModel() { All = all, Mine = mine });
        }

        /// <summary>
        /// Add a profile from the front end. This is different than importing a nodeset XML file. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Add")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageProfile))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Add([FromBody] ProfileModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileController|Add|Invalid model (null)");
                return BadRequest($"Invalid model (null). Check Publish Date formatting.");
            }
            var userToken = UserExtension.DalUserToken(User);

            //test for unique namespace/owner id/publish date combo
            if (!IsValidModel(model, userToken))
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = "There is already a profile with this namespace and publish date combination. Enter a different namespace or publish date.",
                    Data = null
                });
            }

            //set some values server side 
            model.AuthorId = userToken.UserId;
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

            var id = await _dal.Add(model, userToken);
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
        [Authorize(Policy = nameof(PermissionEnum.CanManageProfile))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public IActionResult ValidateModel([FromBody] ProfileModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileController|ValidateModel|Invalid model (null)");
                return BadRequest($"Invalid model (null). Check Publish Date formatting.");
            }
            var userToken = UserExtension.DalUserToken(User);

            //test for unique namespace/owner id/publish date combo
            if (!IsValidModel(model, userToken))
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
                             x.OwnerId.HasValue && x.OwnerId.Value.Equals(User.GetUserID()) &&
                             (x.PublishDate == model.PublishDate)
                            , userToken) == 0;
        }

        /// <summary>
        /// Update an existing nodeset that is maintained within this system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Update")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageProfile))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Update([FromBody] ProfileModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileController|Add|Invalid model (null)");
                return BadRequest($"Invalid model (null). Check Publish Date formatting.");
            }
            var userToken = UserExtension.DalUserToken(User);

            //test for unique namespace/owner id/publish date combo
            if (_dal.Count(x => !x.ID.Equals(model.ID) && x.Namespace.ToLower().Equals(model.Namespace.ToLower()) &&
                             x.OwnerId.HasValue && x.OwnerId.Value.Equals(User.GetUserID()) 
                             && ((!model.PublishDate.HasValue && !x.PublishDate.HasValue)
                                    || (model.PublishDate.HasValue && x.PublishDate.HasValue && model.PublishDate.Value.Equals(x.PublishDate.Value)))
                            //&& (!x.PublishDate.HasValue ? new DateTime(0) : x.PublishDate.Value.Date).Equals(!model.PublishDate.HasValue ? new DateTime(0) : model.PublishDate.Value.Date)
                            , userToken) > 0)
            {
                return Ok(new ResultMessageWithDataModel()
                {
                    IsSuccess = false,
                    Message = "There is already a profile with this namespace and publish date combination. Enter a different namespace or publish date.",
                    Data = null
                });
            }

            var item = _dal.GetById(model.ID.Value, userToken);
            //can't update an item that is not yours
            if (!item.AuthorId.Equals(User.GetUserID()))
            {
                _logger.LogWarning($"ProfileController|Update|AuthorId {model.AuthorId} of item {model.ID} is different than User Id {User.GetUserID()} making update.");
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

            var result = await _dal.Update(model, userToken);
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
        [Authorize(Policy = nameof(PermissionEnum.CanDeleteProfile))]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Delete([FromBody] IdIntModel model)
        {
            var userToken = UserExtension.DalUserToken(User);

            //This also deletes all associated type defs, attributes, custom data types, compositions, interfaces 
            //associated with this profile
            try
            {
                var result = await _dal.Delete(model.ID, userToken);
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
            return _dal.Delete(model.ID, new UserToken { UserId = -1 }).Result;
        }

        /// <summary>
        /// Delete one or many nodesets. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("DeleteMany")]
        [Authorize(Policy = nameof(PermissionEnum.CanDeleteProfile))]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> DeleteMany([FromBody] List<IdIntModel> model)
        {
            var userToken = UserExtension.DalUserToken(User);

            //check that this user is author of all the items they are trying to delete
            var ids = model.Select(x => x.ID).ToList();

            //This also deletes all associated type defs, attributes, custom data types, compositions, interfaces 
            //associated with this nodeset and its profiles
            try
            {
                var result = await _dal.DeleteMany(model.Select(x => x.ID).ToList(), userToken);
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
        [Authorize(Policy = nameof(PermissionEnum.CanManageProfile))]
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
        [Authorize(Policy = nameof(PermissionEnum.CanManageProfile))]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        public async Task<IActionResult> Import([FromBody] List<ImportOPCModel> model /*, [FromServices] OpcUaImporter importer*/)
        {
            if (!ModelState.IsValid)
            {
                var errors = ExtractModelStateErrors();
                _logger.LogCritical($"ProfileController|Import|User Id:{User.GetUserID()}, Errors: {errors}");
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
                _logger.LogWarning($"ProfileController|Import|No nodeset files to import. User Id:{User.GetUserID()}.");
                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = false,
                        Message = "No nodeset files to import."
                    }
                );
            }

            _logger.LogInformation($"ProfileController|ImportMyOpcUaProfile|Importing {model.Count} nodeset files. User Id:{User.GetUserID()}.");

            //pass in the author id as current user
            var userToken = UserExtension.DalUserToken(User);

            //kick off background process, logid is returned immediately so front end can track progress...
            var logId = await _svcImport.ImportOpcUaNodeSet(model, userToken);

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
        [Authorize(Policy = nameof(PermissionEnum.CanManageProfile))]
        [ProducesResponseType(200, Type = typeof(ResultMessageExportModel))]
        public async Task<IActionResult> Export([FromBody] IdIntModel model)
        {
            var userToken = UserExtension.DalUserToken(User);

            //get profile to export
            var sw = Stopwatch.StartNew();
            _logger.LogTrace("Starting export");
            var item = _dal.GetById(model.ID, userToken);
            if (item == null)
            {
                //return Task.FromResult(new ResultMessageWithDataModel()
                return Ok(
                    new ResultMessageExportModel()
                    {
                        IsSuccess = false,
                        Message = "Profile not found."
                    }
                );
            }

            // Populate the OPC model into a new importer instance
            try
            {
                string result = null;
                using (var xmlNodeSetStream = new MemoryStream())
                {
                    _logger.LogTrace($"Timestamp||Export||Starting: {sw.Elapsed}");
                    if (_exporter.ExportNodeSet(item, xmlNodeSetStream, userToken, null))
                    {
                        _logger.LogTrace($"Timestamp||Export||Nodeset Stream generated: {sw.Elapsed}");
                        result = Encoding.UTF8.GetString(xmlNodeSetStream.ToArray());
                        _logger.LogTrace($"Timestamp||Export||Data Converted to Response: {sw.Elapsed}");

                        //TBD - read and include the required models in a ZIP file, optionally?
                        //TBD - get the warnings that were logged on import and publish them here. 
                    }
                    else
                    {
                        return Ok(new ResultMessageExportModel()
                        {
                            IsSuccess = false,
                            Message = "An error occurred downloading the profile."
                        });
                    }
                }
                return Ok(new ResultMessageExportModel()
                {
                    IsSuccess = true,
                    Message = "",
                    Data = result,
                    Warnings = item.ImportWarnings.Select(x => x.Message).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"ProfileController|Export|Failed:{ex.Message}");
                _logger.LogTrace($"Timestamp||Export||Error: {sw.Elapsed}|| {ex.Message}");
                return Ok(new ResultMessageExportModel()
                {
                    IsSuccess = false,
                    Message = $"Technical details: {ex.Message}"
                });
            }
        }
    }
}
