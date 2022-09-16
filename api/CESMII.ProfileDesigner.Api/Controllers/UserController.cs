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

        public UserController(UserDAL dal,
            ConfigUtil config, ILogger<UserController> logger)
            : base(config, logger, dal)
        {
            _dal = dal;
        }


        [HttpGet, Route("All")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [Authorize(Roles = "cesmii.marketplace.useradmin")]
        [ProducesResponseType(200, Type = typeof(List<UserModel>))]
        [ProducesResponseType(400)]
        public IActionResult GetAll()
        {
            var result = _dal.GetAll(base.DalUserToken);
            if (result == null)
            {
                return BadRequest($"No records found.");
            }
            return Ok(result);
        }

        [HttpPost, Route("GetByID")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [Authorize(Roles = "cesmii.marketplace.useradmin")]
        [ProducesResponseType(200, Type = typeof(UserModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByID([FromBody] IdIntModel model)
        {
            var result = _dal.GetById(model.ID, base.DalUserToken);
            if (result == null)
            {
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            return Ok(result);
        }

        [HttpPost, Route("Search")]
        //[Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [Authorize(Roles = "cesmii.marketplace.useradmin")]
        [ProducesResponseType(200, Type = typeof(DALResult<UserModel>))]
        public IActionResult Search([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                return BadRequest("User|Search|Invalid model");
            }

            if (string.IsNullOrEmpty(model.Query))
            {
                return Ok(_dal.GetAllPaged(base.DalUserToken, model.Skip, model.Take, true));
            }

            model.Query = model.Query.ToLower();
            var result = _dal.Where(s =>
                            //string query section
                            //s.IsActive && 
                            //(s.UserName.ToLower().Contains(model.Query) ||
                            //(s.FirstName.ToLower() + s.LastName.ToLower()).Contains(
                            //    model.Query.Replace(" ", "").Replace("-", ""))),  //in case they search for code and name in one string.
                            s.DisplayName.ToLower().Contains(model.Query),
                            base.DalUserToken, model.Skip, model.Take, true);
            return Ok(result);
        }

        /*
        /// <summary>
        /// Create a blank user model for an add scenario
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("init")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [ProducesResponseType(200, Type = typeof(UserModel))]
        public IActionResult InitUser()
        {
            //get existing user, wipe out certain pieces of info, add new user
            //update new user's password, 
            var result = new UserModel();

            //return object
            return Ok(result);
        }

        /// <summary>
        /// Add user. This flow is intended (in future) to have server side generate the password and email user a registration link.
        /// The registration link would then allow user to create new password and complete registration process. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Add")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Add([FromBody] UserModel model)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new ResultMessageModel()
                {
                    IsSuccess = false,
                    Message = ExtractModelStateErrors().ToString()
                });
            }
            var userToken = UserExtension.DalUserToken(User, LocalUser.ID);

            var result = await _dal.AddAsync(model, base.DalUserToken);
            model.ID = result;
            if (result == 0)
            {
                _logger.LogWarning($"Could not add user: {model.FirstName} {model.LastName}.");
                return Ok(new ResultMessageModel()
                {
                    IsSuccess = false,
                    Message = "Could not add user. "
                });
            }
            _logger.LogInformation($"Added user item. Id:{result}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was added." });
        }

        /// <summary>
        /// Add a user and allow caller (an admin) to include a password with the add user model. 
        /// </summary>
        /// <remarks> Main difference from normal Add is this one does an extra step of taking in a new password immediately instead of going 
        /// through a registration flow with email, etc. 
        /// Also note that this endpoint is only permitted for users with user mgmt permissions. 
        /// </remarks>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("add/onestep")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> AddOneStep([FromBody] UserAddModel model)
        {
            var userToken = UserExtension.DalUserToken(User, LocalUser.ID);
            ValidateCopyModel(model, base.DalUserToken);

            if (!ModelState.IsValid)
            {
                return Ok(new ResultMessageModel()
                {
                    IsSuccess = false,
                    Message = ExtractModelStateErrors().ToString()
                });
            }


            var result = await _dal.AddOneStep(model, base.DalUserToken, model.Password);
            model.ID = result;
            if (result == 0)
            {
                _logger.LogWarning($"Could not add user: {model.FirstName} {model.LastName}.");
                return Ok(new ResultMessageModel()
                {
                    IsSuccess = false,
                    Message = "Could not add user. "
                });
            }
            _logger.LogInformation($"Added user item. Id:{result}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was copied." });
        }
        
        /// <summary>
        /// Copy an existing user, clear out certain characteristics (user name, first name, last name, etc.) and then 
        /// return a model to allow caller to set those unique user characteristics.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("copy")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [ProducesResponseType(200, Type = typeof(UserModel))]
        public IActionResult CopyUser([FromBody] IdIntModel model)
        {
            var userToken = UserExtension.DalUserToken(User, LocalUser.ID);

            //get existing user, wipe out certain pieces of info, add new user
            //update new user's password, 
            var result = _dal.GetById(model.ID, base.DalUserToken);
            if (result == null)
            {
                return BadRequest("Original user not found.");
            }

            //keep permissions and organization same
            result.ID = null;
            result.Created = DateTime.UtcNow;
            result.Email = null;
            result.FirstName = null;
            result.LastName = null;
            result.UserName = null;
            result.LastLogin = null;
            result.RegistrationComplete = null;

            return Ok(result);

        }

        [HttpPost, Route("Update")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageUsers))]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Update([FromBody] UserModel model)
        {
            var userToken = UserExtension.DalUserToken(User, LocalUser.ID);
            ValidateModel(model, base.DalUserToken);

            if (!ModelState.IsValid)
            {
                return Ok(new ResultMessageModel()
                {
                    IsSuccess = false,
                    Message = ExtractModelStateErrors().ToString()
                });
            }

            var result = await _dal.UpdateAsync(model, base.DalUserToken);
            if (result < 0)
            {
                _logger.LogWarning($"Could not update user. Invalid id:{model.ID}.");
                return Ok(new ResultMessageModel()
                {
                    IsSuccess = false,
                    Message = "Could not update user. Invalid id."
                });
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
            var userToken = UserExtension.DalUserToken(User, LocalUser.ID);
            var result = await _dal.DeleteAsync(model.ID, base.DalUserToken);
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
        /// Perform server side validation prior to saving
        /// </summary>
        /// <param name="model"></param>
        private void ValidateModel(UserModel model, base.DalUserToken token)
        {
            //check for dup user name
            if (_dal.Count(x => x.UserName.ToLower().Equals(model.UserName.ToLower()) 
                    && (!model.ID.HasValue || !model.ID.Value.Equals(x.ID.Value)), token) > 0)
            {
                ModelState.AddModelError("User Name", "Duplicate user name. Please enter a different user name.");
            }
        }

        /// <summary>
        /// Perform server side validation prior to saving
        /// </summary>
        /// <param name="model"></param>
        private void ValidateCopyModel(UserAddModel model, UserToken token)
        {
            //check for dup user name
            if (_dal.Count(x => x.UserName.ToLower().Equals(model.UserName.ToLower()), token) > 0)
            {
                ModelState.AddModelError("User Name", "Duplicate user name. Please enter a different user name.");
            }
        }
        */
    }

}
