﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;


using CESMII.ProfileDesigner.Common;
using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.Api.Shared.Controllers;
using CESMII.ProfileDesigner.DAL;

namespace CESMII.ProfileDesigner.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class SystemController : BaseController<SystemController>
    {
        public SystemController(UserDAL dalUser, ConfigUtil config, ILogger<SystemController> logger) 
            : base(config, logger, dalUser)
        {
        }

        [AllowAnonymous, HttpPost, Route("log/public")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        [ProducesResponseType(400)]
        public IActionResult LogMessagePublic([FromBody] FrontEndErrorModel model)
        {
            var result = new ResultMessageWithDataModel() { IsSuccess = true, Message = "", Data = null };

            _logger.LogCritical($"REACT|LogMessage|User:Unknown|Error:{model.Message}|Url:{model.Url}");

            return Ok(result);
        }

        [HttpPost, Route("log/private")]
        [ProducesResponseType(200, Type = typeof(ResultMessageWithDataModel))]
        [ProducesResponseType(400)]
        public IActionResult LogMessagePrivate([FromBody] FrontEndErrorModel model)
        {
            var result = new ResultMessageWithDataModel() { IsSuccess = true, Message = "", Data = null };

            _logger.LogCritical($"REACT|LogMessage|User:{LocalUser.ID}|Error:{model.Message}|Url:{model.Url}");

            return Ok(result);
        }

    }
}
