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
    using System.Linq;
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


        [Authorize]
        [HttpPost, Route("QueryCurrentOrganization")]
        public IActionResult QueryCurrentOrganization()
        {
            string strOrgName = (LocalUser.Organization == null) ? "" :
                                (LocalUser.Organization.Name == null) ? "" :
                                LocalUser.Organization.Name;
            return Ok(new ResultMessageModel() { IsSuccess = true, Message = strOrgName });
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
            bool bFound = false;

            UserModel um = null;

            // Search using user's Azure id.
            var userAAD = User.GetUserAAD();
            var listMatchObjectIdAAD = _dalUser.Where(x => x.ObjectIdAAD.ToLower().Equals(userAAD.ObjectIdAAD), null).Data;
            if (listMatchObjectIdAAD.Count > 0)
            {
                var mysort = listMatchObjectIdAAD.OrderBy(item => item.Created);
                um = mysort.First();
                um.Email = userAAD.Email;
                um.DisplayName = userAAD.DisplayName;
                um.LastLogin = DateTime.UtcNow;

                bCheckOrganization = true;  // Check the user's organization.

                bFound = true;              // No need to keep looking.

                if (listMatchObjectIdAAD.Count > 1)
                {
                    // This is likely a database problem, so let's let them in.
                    _logger.LogWarning($"InitLocalUser||More than one Profile designer user record found with user object id {userAAD.ObjectIdAAD}. {listMatchObjectIdAAD.Count} records found.");
                }
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

                    bCheckOrganization = true;  // Check the user's organization.
                }
                else // We have one or more records with the same email address.
                {
                    // If more than 1 item, grab the oldest one.
                    var mysort = listMatchEmailAddress.OrderBy(item => item.Created);
                    um = mysort.First();

                    // Update the user's Azure id. If we are here, then Azure id has changed.
                    // This can happen if user
                    // (1) Does self-service sign-up, 
                    // (2) Leaves the organization, then
                    // (3) Signs-up again.
                    um.ObjectIdAAD = userAAD.ObjectIdAAD;
                    um.DisplayName = userAAD.DisplayName;
                    um.LastLogin = DateTime.UtcNow;

                    bCheckOrganization = true;  // Check the user's organization.

                    // Log an error message.
                    if (listMatchEmailAddress.Count > 1)
                    {
                        // Could be a database problem, or maybe they left the organization and then signed up again.
                        // Let's let them in.
                        string strError = $"InitLocalUser||More than one Profile designer user record found with email {userAAD.Email}. {listMatchEmailAddress.Count} records found.";
                        _logger.LogWarning(strError);
                    }
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
                    }
                    else if (listMatchOrganizationName.Count > 0)
                    {
                        // More than one? Go with first one.
                        var myOrgSort = listMatchOrganizationName.OrderBy(org => org.ID);
                        um.Organization = myOrgSort.First();
                        if (listMatchOrganizationName.Count > 1)
                        {
                            // More than one organization. Oops. 
                            // Not sure why this happened, but we log it and go with the first one.
                            string strError = $"InitLocalUser||More than one organization record found with Name = {strFindOrgName}. {listMatchOrganizationName.Count} records found.";
                            _logger.LogWarning(strError);
                        }
                    }
                }
            }

            // We always update the user record.
            // If nothing else, we need to update the last login date & time.
            _dalUser.UpdateAsync(um, new UserToken() { UserId = um.ID.Value }).Wait();

            return um;

        }
    }
}
