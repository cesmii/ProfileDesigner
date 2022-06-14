using System;
using System.Collections.Generic;
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
using CESMII.ProfileDesigner.Api.Shared.Controllers;
using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.Api.Shared.Extensions;
using CESMII.ProfileDesigner.Api.Shared.Utils;
using CESMII.ProfileDesigner.OpcUa;
using Microsoft.Extensions.DependencyInjection;

namespace CESMII.ProfileDesigner.Api.Controllers
{
    [Authorize, Route("api/[controller]")]
    public class ImportLogController : BaseController<ImportLogController>
    {
        private readonly IDal<ImportLog, ImportLogModel> _dal;

        public ImportLogController(IDal<ImportLog, ImportLogModel> dal,
            ConfigUtil config, ILogger<ImportLogController> logger) 
            : base(config, logger)
        {
            _dal = dal;
        }

        [HttpPost, Route("GetByID")]
        [Authorize(Policy = nameof(PermissionEnum.CanManageProfile))]
        //[ProducesResponseType(200, Type = typeof(NodeSetModel))]
        [ProducesResponseType(200, Type = typeof(ImportLogModel))]
        [ProducesResponseType(400)]
        public IActionResult GetByID([FromBody] IdIntModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ImportLogController|GetByID|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }
            var userToken = UserExtension.DalUserToken(User);

            var result = _dal.GetById(model.ID, userToken);
            if (result == null)
            {
                _logger.LogWarning($"ImportLogController|GetById|No records found matching this ID: {model.ID}");
                return BadRequest($"No records found matching this ID: {model.ID}");
            }
            return Ok(result);
        }


        /// <summary>
        /// Get my import logs
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Mine")]
        [Authorize(Policy = nameof(PermissionEnum.CanViewProfile))]
        [ProducesResponseType(200, Type = typeof(DALResult<ProfileModel>))]
        public IActionResult GetMine([FromBody] PagerFilterSimpleModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("ImportLogController|GetMine|Invalid model.");
                return BadRequest("Profile|GetMine|Invalid model");
            }

            var userToken= new UserToken { UserId = User.GetUserID() };
            if (string.IsNullOrEmpty(model.Query))
            {
                return Ok(_dal.GetAllPaged(userToken, model.Skip, model.Take, false, true));
            }

            model.Query = model.Query.ToLower();
            var result = _dal.Where(s =>
                            //string query section
                            string.Join(",", s.FileList).ToLower().Contains(model.Query)
                            ,userToken, null, null, false, true);
            return Ok(result);
        }

        /// <summary>
        /// Delete an existing import log. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Route("Delete")]
        [Authorize(Policy = nameof(PermissionEnum.CanDeleteProfile))]
        [ProducesResponseType(200, Type = typeof(ResultMessageModel))]
        public async Task<IActionResult> Delete([FromBody] IdIntModel model)
        {
            var userToken = UserExtension.DalUserToken(User);

            //This is a soft delete
            var result = await _dal.Delete(model.ID, userToken);
            if (result < 0)
            {
                _logger.LogWarning($"ImportLogController|Delete|Could not delete item. Invalid id:{model.ID}.");
                return BadRequest("Could not delete item. Invalid id.");
            }
            _logger.LogInformation($"ImportLogController|Delete|Deleted item. Id:{model.ID}.");

            //return success message object
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = "Item was deleted." });
        }

    }

}
