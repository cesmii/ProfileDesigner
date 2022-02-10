using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;


using CESMII.ProfileDesigner.Common;
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
    public class AuthController : BaseController<AuthController>
    {
        private readonly UserDAL _dal;
        private readonly MailRelayService _mailRelay;
        private readonly TokenUtils _tokenUtils;

        public AuthController(UserDAL dal, TokenUtils tokenUtils, MailRelayService mailRelay, ConfigUtil config, ILogger<AuthController> logger) 
            : base(config, logger)
        {
            _dal = dal;
            _tokenUtils = tokenUtils;
            _mailRelay = mailRelay;
        }

        [AllowAnonymous, HttpPost, Route("Login")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var result = new ResultMessageWithDataModel() { IsSuccess = false, Message = "", Data = null };

            if (string.IsNullOrEmpty(model.UserName))
            {
                result.Message = "Please supply the required username.";
                return Ok(result);
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                result.Message = "Please supply the required password.";
                return Ok(result);
            }

            var user = await _dal.Validate(model.UserName, model.Password);
            if (user == null)
            {
                result.Message = "Invalid user name or password. Please try again.";
                return Ok(result);
            }

            var tokenModel = _tokenUtils.BuildToken(user);
            result.IsSuccess = true;
            result.Data = new LoginResultModel()
            {
                Token = tokenModel.Token,
                IsImpersonating = tokenModel.IsImpersonating,
                User = user
            };

            return Ok(result);
        }

        [HttpPost, Route("ValidateByToken")]
        [ProducesResponseType(200, Type = typeof(UserModel))]
        public async Task<IActionResult> ValidateByToken([FromBody] ChangePasswordModel model)
        {
            if (string.IsNullOrEmpty(model.NewPassword))
            {
                return BadRequest("Password is required.");
            }
            var userToken = UserExtension.DalUserToken(User);

            var user = _dal.GetById(User.GetUserID(), userToken);
            if (user == null)
            {
                return BadRequest("User was not found. Please contact support.");
            }

            // If we get here, update the user data with new password
            await _dal.ChangePassword(user.ID.Value, model.OldPassword, model.NewPassword);

            // Why do we expect a new token here? The user is already authenticated and just changing their password?
            var tokenModel = _tokenUtils.BuildToken(user);
            return Ok(tokenModel);
        }

        [HttpPost, Route("ExtendToken")]
        [ProducesResponseType(200, Type = typeof(TokenModel))]
        public IActionResult ExtendToken()
        {
            var userToken = UserExtension.DalUserToken(User);
            if (User.IsImpersonating())
            {
                // So little tricky bit here, because we "believe" the UserID to be the org ID we cannot use the base
                // UserID and must acquire this from the token directly; not the helper method.
                var realUser = _dal.GetById(User.GetRealUserID(), userToken);

                // Refresh the token with the target user and org id.
                return Ok(_tokenUtils.BuildImpersonationToken(realUser, User.ImpersonationTargetUserID()));
            }
            else
            {
                var user = _dal.GetById(User.GetUserID(), userToken);
                var newToken = _tokenUtils.BuildToken(user);
                return Ok(newToken);
            }
        }

        [AllowAnonymous]
        [HttpPost, Route("ForgotPassword")]
        public /*async*/ Task<IActionResult> ForgotPassword([FromBody] UserModel model)
        {
            throw new NotImplementedException();
        }

    }
}
