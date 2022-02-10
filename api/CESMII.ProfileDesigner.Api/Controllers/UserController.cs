using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using CESMII.ProfileDesigner.Common;
using CESMII.ProfileDesigner.Common.Enums;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.Api.Shared.Utils;
using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.Api.Shared.Controllers;
using CESMII.ProfileDesigner.Api.Shared.Extensions;

namespace CESMII.ProfileDesigner.Api.Controllers
{
    
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : BaseController<UserController>
    {
        private readonly UserDAL _dal;
        private readonly IDal<Permission, PermissionModel> _dalPermission;
        private readonly TokenUtils _tokenUtils;
        //private int _pageSize = 30;

        public UserController(UserDAL dal,
            IDal<Permission, PermissionModel> dalPermission,
            IDal<LookupType, LookupTypeModel> dalLookupType,
            ConfigUtil config, TokenUtils tokenUtils, ILogger<UserController> logger)
            : base(config, logger)
        {
            _dal = dal;
            _dalPermission = dalPermission;
            _tokenUtils = tokenUtils;
            //_pageSize = config.AdminSettings.UserSettings.PageSize;
        }


        [HttpGet, Route("All")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [ProducesResponseType(200, Type = typeof(List<UserModel>))]
        [ProducesResponseType(400)]
        public IActionResult GetAll()
        {
            var userToken = UserExtension.DalUserToken(User);
            var result = _dal.GetAll(userToken);
            if (result == null)
            {
                return BadRequest($"No records found.");
            }
            return Ok(result);
        }

        [HttpPost, Route("GetByID")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [ProducesResponseType(200, Type = typeof(UserModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByID([FromBody] IdIntModel model)
        {
            var userToken = UserExtension.DalUserToken(User);
            var result = _dal.GetById(model.ID, userToken);
            if (result == null)
            {
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            return Ok(result);
        }

        [HttpPost, Route("Search")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [ProducesResponseType(200, Type = typeof(DALResult<UserModel>))]
        public IActionResult Search([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                return BadRequest("User|Search|Invalid model");
            }

            var userToken = UserExtension.DalUserToken(User);

            if (string.IsNullOrEmpty(model.Query))
            {
                return Ok(_dal.GetAllPaged(userToken, model.Skip, model.Take, true));
            }

            model.Query = model.Query.ToLower();
            var result = _dal.Where(s =>
                            //string query section
                            s.IsActive && 
                            (s.UserName.ToLower().Contains(model.Query) ||
                            (s.FirstName.ToLower() + s.LastName.ToLower()).Contains(
                                model.Query.Replace(" ", "").Replace("-", ""))),  //in case they search for code and name in one string.
                            userToken, model.Skip, model.Take, true);
            return Ok(result);
        }

        [HttpPost, Route("Add")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [ProducesResponseType(200, Type = typeof(List<UserModel>))]
        public async Task<IActionResult> Add([FromBody] UserModel model)
        {
            if (!ModelState.IsValid)
            {
                //var errors = ExtractModelStateErrors();
                return BadRequest("The user record is invalid. Please correct the following:...TBD - join errors collection into string list.");
            }
            var userToken = UserExtension.DalUserToken(User);

            var result = await _dal.Add(model, userToken);
            model.ID = result;
            if (result == 0)
            {
                _logger.LogWarning($"Could not add user: {model.FirstName} {model.LastName}.");
                return BadRequest("Could not add user. ");
            }
            _logger.LogInformation($"Added user item. Id:{result}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was added." });
        }


        [HttpPost, Route("Update")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [ProducesResponseType(200, Type = typeof(List<UserModel>))]
        public async Task<IActionResult> Update([FromBody] UserModel model)
        {
            if (!ModelState.IsValid)
            {
                //var errors = ExtractModelStateErrors();
                return BadRequest("The profile record is invalid. Please correct the following:...join errors collection into string list.");
            }
            var userToken = UserExtension.DalUserToken(User);

            var result = await _dal.Update(model, userToken);
            if (result < 0)
            {
                _logger.LogWarning($"Could not update user. Invalid id:{model.ID}.");
                return BadRequest("Could not update user. Invalid id.");
            }
            _logger.LogInformation($"Updated user. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was updated." });
        }

        [HttpPost, Route("Delete")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [ProducesResponseType(200, Type = typeof(List<UserModel>))]
        public async Task<IActionResult> Delete([FromBody] IdIntModel model)
        {
            var userToken = UserExtension.DalUserToken(User);
            var result = await _dal.Delete(model.ID, userToken);
            if (result < 0)
            {
                _logger.LogWarning($"Could not delete user. Invalid id:{model.ID}.");
                return BadRequest("Could not delete user. Invalid id.");
            }
            _logger.LogInformation($"Deleted user. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was deleted." });
        }


        /// <summary>
        /// TBD - Perform server side validation prior to saving
        /// </summary>
        /// <param name="model"></param>
        private void ValidateModel(UserModel model)
        {
            //Check for duplicate service and return model state error
            //if (model.Attributes != null && model.Attributes.GroupBy(v => v.Name).Where(g => g.Count() > 1).Any())
            //{
            //    ModelState.AddModelError("", "Duplicate attribute names found. Remove the duplicates.");
            //}
        }

    }
    
}
