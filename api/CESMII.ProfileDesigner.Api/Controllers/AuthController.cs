using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;


using CESMII.ProfileDesigner.Common;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.Api.Shared.Controllers;
using CESMII.ProfileDesigner.Api.Shared.Extensions;
using CESMII.ProfileDesigner.Common.Enums;

namespace CESMII.ProfileDesigner.Api.Controllers
{
    [Authorize, Route("api/[controller]")]
    public class AuthController : BaseController<AuthController>
    {
        public AuthController(UserDAL dal, ConfigUtil config, ILogger<AuthController> logger)
            : base(config, logger, dal)
        {
        }

        [HttpPost, Route("onAADLogin")]
        public IActionResult OnAADLogin()
        {
            //extract user name from identity passed in via token
            //check if that user record is in DB. If not, add it.
            //InitLocalUser: this property checks for user, adds to db and returns a fully formed user model if one does not exist. 
            var user = InitLocalUser();
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = $"On AAD Login, profile designer user {user.ObjectIdAAD} was initialized." });
        }

        /// <summary>
        /// On successful Azure AD login, front end will call this to initialize the user in our DB (if not already there).
        /// Once this happens, then subsequent calls will expect user record is already and just ask for id. We won't have multiple 
        /// parallel calls trying to create user locally.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected UserModel InitLocalUser()
        {
            UserModel result = null;

            //extract user name from identity passed in via token
            //check if that user record is in DB. If not, add it.
            var userAAD = User.GetUserAAD();
            var matches = _dalUser.Where(x => x.ObjectIdAAD.ToLower().Equals(userAAD.ObjectIdAAD), null).Data;
            switch (matches.Count)
            {
                case 1:
                    result = matches[0];
                    result.LastLogin = DateTime.UtcNow;
                    result.DisplayName = userAAD.DisplayName;
                    _dalUser.UpdateAsync(matches[0], new UserToken() { UserId = result.ID.Value }).Wait();
                    break;
                case 0:
                    result = new UserModel()
                    {
                        ObjectIdAAD = userAAD.ObjectIdAAD,
                        DisplayName = userAAD.DisplayName,
                        LastLogin = DateTime.UtcNow
                    };
                    result.ID = _dalUser.AddAsync(result, null).Result;
                    break;
                default:
                    _logger.LogWarning($"InitLocalUser||More than one Profile designer user record found with user name {userAAD.ObjectIdAAD}.");
                    throw new ArgumentNullException($"InitLocalUser: More than one Profile designer record user found with user name {userAAD.ObjectIdAAD}.");
            }

            return result;

        }

    }
}
