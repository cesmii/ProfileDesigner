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
using System.Linq;
using System.Threading.Tasks;
using CESMII.ProfileDesigner.Data.Entities;

namespace CESMII.ProfileDesigner.Api.Controllers
{

    [Route("api/[controller]")]
    [Authorize]
    public class CloudLibraryController : BaseController<UserController>
    {
        private readonly ICloudLibDal<CloudLibProfileModel> _cloudLibDal;
        private readonly IDal<Profile, ProfileModel> _profileDal;
        private readonly UserDAL _userDal;

        public CloudLibraryController(
            ICloudLibDal<CloudLibProfileModel> cloudLibDal,
            IDal<Profile, ProfileModel> profileDal,
            UserDAL userDal,
            ConfigUtil config, ILogger<UserController> logger)
            : base(config, logger, userDal)
        {
            this._cloudLibDal = cloudLibDal;
            this._profileDal = profileDal;
            _userDal = userDal;
        }


        [HttpPost, Route("pendingapprovals")]
        [Authorize(Roles = "cesmii.profiledesigner.admin")]
        [ProducesResponseType(200, Type = typeof(DALResult<CloudLibProfileModel>))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetPendingApprovalAsync()
        {
            // TODO implement search/filter/pagination
            var pendingNodeSetsResult = await _cloudLibDal.GetNodeSetsPendingApprovalAsync(100, null, false, null);
            if (pendingNodeSetsResult == null)
            {
                return BadRequest($"No records found.");
            }

            DALResult<CloudLibProfileModel> result = new DALResult<CloudLibProfileModel>
            {
                Count = pendingNodeSetsResult.Nodes.Count(),
                Data = pendingNodeSetsResult.Nodes.ToList(),
                EndCursor = pendingNodeSetsResult.PageInfo.EndCursor,
                StartCursor = pendingNodeSetsResult.PageInfo.StartCursor,
                HasNextPage = pendingNodeSetsResult.PageInfo.HasNextPage,
                HasPreviousPage = pendingNodeSetsResult.PageInfo.HasPreviousPage,
            };

            return Ok(result);
        }

        [HttpPost, Route("approve")]
        [Authorize(Roles = "cesmii.profiledesigner.admin")]
        [ProducesResponseType(200, Type = typeof(CloudLibProfileModel))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ApproveProfileAsync([FromBody] ApprovalModel model)
        {
            if (model == null)
            {
                _logger.LogWarning($"ProfileController|ApproveProfile|Invalid model (null)");
                return BadRequest($"Invalid model (null)");
            }

            if (string.IsNullOrEmpty(model.ID))
            {
                _logger.LogWarning($"ProfileController|ApproveProfile|Failed to approve : {model.ID}. Profile has no cloud library id.");
                return Ok(
                    new ResultMessageWithDataModel()
                    {
                        IsSuccess = false,
                        Message = "Profile not in cloud library."
                    }
                );

            }

            var approvedNodeSet = await _cloudLibDal.UpdateApprovalStatusAsync(model.ID, model.ApprovalStatus, model.ApprovalDescription);
            if (approvedNodeSet == null)
            {
                return BadRequest($"Approval update failed.");
            }
            return Ok(approvedNodeSet);
        }


    }
}
