namespace CESMII.ProfileDesigner.Api.Controllers
{
    using CESMII.ProfileDesigner.Api.Shared.Controllers;
    using CESMII.ProfileDesigner.Api.Shared.Extensions;
    using CESMII.ProfileDesigner.Api.Shared.Models;
    using CESMII.ProfileDesigner.Common;
    using CESMII.ProfileDesigner.DAL;
    using CESMII.ProfileDesigner.DAL.Models;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;

    [Authorize, Route("api/[controller]")]
    public class AuthController : BaseController<AuthController>
    {
        protected readonly OrganizationDAL _dalOrganization;

        public AuthController(UserDAL dal, OrganizationDAL orgdal, ConfigUtil config, ILogger<AuthController> logger)
            : base(config, logger, dal)
        {
            _dalOrganization = orgdal;
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
            bool bCheckOrganization = false;
            bool bUpdateUser = false;
            bool bFound = false;

            UserModel um = null;

            // Search using user's Azure id.
            var userAAD = User.GetUserAAD();
            var listMatchObjectIdAAD = _dalUser.Where(x => x.ObjectIdAAD.ToLower().Equals(userAAD.ObjectIdAAD), null).Data;
            if (listMatchObjectIdAAD.Count == 1)
            {
                um = listMatchObjectIdAAD[0];
                um.Email = userAAD.Email;
                um.DisplayName = userAAD.DisplayName;
                um.LastLogin = DateTime.UtcNow;

                bUpdateUser = true;         // Synch UserModel changes
                bCheckOrganization = true;  // Check the user's organization.

                bFound = true;              // No need to keep looking.
            }
            else if (listMatchObjectIdAAD.Count > 1)
            {
                _logger.LogWarning($"InitLocalUser||More than one Profile designer user record found with user name {userAAD.ObjectIdAAD}. {listMatchObjectIdAAD.Count} records found.");
                throw new ArgumentNullException($"InitLocalUser: More than one Profile designer record user found with user name {userAAD.ObjectIdAAD}. {listMatchObjectIdAAD.Count} records found.");
            }


            // If Azure id not found, search by email address.
            if (!bFound)
            {
                var listMatchEmailAddress = _dalUser.Where(x => x.EmailAddress.ToLower().Equals(userAAD.Email.ToLower()), null).Data;
                if (listMatchEmailAddress.Count == 0)
                {
                    // First time we are encountering this user.
                    um = new UserModel()
                    {
                        ObjectIdAAD = userAAD.ObjectIdAAD,
                        Email = userAAD.Email,
                        DisplayName = userAAD.DisplayName,
                        LastLogin = DateTime.UtcNow
                    };
                    um.ID = _dalUser.AddAsync(um, null).Result;
                    um = _dalUser.GetById((int)um.ID, null);

                    bUpdateUser = true;         // Synch UserModel changes
                    bCheckOrganization = true;  // Check the user's organization.
                }
                else if (listMatchEmailAddress.Count == 1)
                {
                    um = listMatchEmailAddress[0];

                    // Update the user's Azure id. If we are here, then Azure id has changed.
                    // This can happen if user
                    // (1) Does self-service sign-up, 
                    // (2) Leaves the organization, then
                    // (3) Signs-up again.
                    um.ObjectIdAAD = userAAD.ObjectIdAAD;
                    um.DisplayName = userAAD.DisplayName;
                    um.LastLogin = DateTime.UtcNow;

                    bUpdateUser = true;         // Synch UserModel changes
                    bCheckOrganization = true;  // Check the user's organization.
                }
                else
                {
                    string strError = $"InitLocalUser||More than one Profile designer user record found with email {userAAD.Email}. {listMatchEmailAddress.Count} records found.";
                    _logger.LogWarning(strError);
                    throw new ArgumentNullException(strError);
                }
            }

            // Check organzation and update it if needed.
            if (bCheckOrganization)
            {
                if (um.Organization == null && um.SelfServiceSignUp_Organization_Name != null)
                {
                    // Name to search for
                    string strFindOrgName = um.SelfServiceSignUp_Organization_Name;

                    // Search for organization
                    var listMatchOrganizationName = _dalOrganization.Where(x => x.Name.ToLower().Equals(strFindOrgName.ToLower()), null).Data;
                    if (listMatchOrganizationName.Count == 0)
                    {
                        // Nothing in public.organization? Create a new record.
                        OrganizationModel om = new OrganizationModel()
                        {
                            Name = strFindOrgName
                        };

                        var idNewOrg = _dalOrganization.AddAsync(om, null).Result;
                        om = _dalOrganization.GetById((int)idNewOrg, null);
                        um.Organization = om;
                        bUpdateUser = true;         // Synch UserModel changes
                    }
                    else if (listMatchOrganizationName.Count == 1)
                    {
                        // Found? Assign it.
                        um.Organization = listMatchOrganizationName[0];
                        bUpdateUser = true;         // Synch UserModel changes
                    }
                    else
                    {
                        // More than one -- oops. A problem.
                        string strError = $"InitLocalUser||More than one organization record found with Name = {strFindOrgName}. {listMatchOrganizationName.Count} records found.";
                        _logger.LogWarning(strError);
                        throw new ArgumentNullException(strError);
                    }
                }
            }

            if (bUpdateUser)
                _dalUser.UpdateAsync(um, new UserToken() { UserId = um.ID.Value }).Wait();

            return um;

        }

    }
}
