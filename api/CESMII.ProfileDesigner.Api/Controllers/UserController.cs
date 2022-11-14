using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using CESMII.ProfileDesigner.Common;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.DAL;
using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.Api.Shared.Controllers;

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
        [Authorize(Roles = "cesmii.profiledesigner.admin")]
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
        [Authorize(Roles = "cesmii.profiledesigner.admin")]
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
        [Authorize(Roles = "cesmii.profiledesigner.admin")]
        [ProducesResponseType(200, Type = typeof(DALResult<UserModel>))]
        public IActionResult Search([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                return BadRequest("User|Search|Invalid model");
            }

            DALResult<UserModel> result;
            if (string.IsNullOrEmpty(model.Query))
            {
                result = _dal.GetAllPaged(base.DalUserToken, model.Skip, model.Take, true);

            }
            else
            {
                model.Query = model.Query.ToLower();
                result = _dal.Where(s =>
                                //string query section
                                s.DisplayName.ToLower().Contains(model.Query) &&
                                !string.IsNullOrEmpty(s.ObjectIdAAD),
                                base.DalUserToken, model.Skip, model.Take, true);
            }

            //obscure the object id AAD a bit more
            foreach (var u in result.Data)
            {
                u.ObjectIdAAD = String.IsNullOrEmpty(u.ObjectIdAAD) ? u.ObjectIdAAD : u.ObjectIdAAD.Substring(0, 12) + "...";
            }
            return Ok(result);
        }

    }

}
