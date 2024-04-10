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
    using System.Threading.Tasks;

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
            var returned = InitLocalUser().Result;
            UserModel user = returned.Item1;
            String strError = returned.Item2;

            if (user != null)
            {
                return Ok(new ResultMessageModel() { IsSuccess = true, Message = $"On AAD Login, profile designer user {user.ObjectIdAAD} was initialized." });
            }
            else
            {
                return StatusCode(401,new ResultMessageModel() { IsSuccess = false, Message = strError });
            }
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
        protected async Task<(UserModel,string)> InitLocalUser()
        {
            bool bCheckOrganization = false;
            bool bUpdateUser = false;
            bool bFound = false;
            bool bErrorCondition = false;
            string strError = null;

            UserModel um = null;

            // Search using user's Azure object id (oid).
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
                bFound = true;              // No need for lookup by email address.
            }
            else if (listMatchObjectIdAAD.Count > 1)
            {
                // We should never get here, since it means that the database is corrupted somehow.
                bErrorCondition = true;
                strError = $"InitLocalUser||More than one Profile designer user record found with user name {userAAD.ObjectIdAAD}. {listMatchObjectIdAAD.Count} records found.";
                _logger.LogWarning(strError);
            }

            if (bErrorCondition)
                return (null,strError);


            // We didn't find user by Azure object id (oid), so let's try finding them by email address.
            if (!bFound)
            {
                // Is there a public.user record for the user's email address?
                var listMatchEmailAddress = _dalUser.Where(x => x.EmailAddress.ToLower().Equals(userAAD.Email.ToLower()) && x.ObjectIdAAD == null, null).Data;
                if (listMatchEmailAddress.Count == 0)
                {
                    // Here for (1) manually created users, or (2) users from profile designer or marketplace self-service sign-up.
                    um = new UserModel()
                    {
                        ObjectIdAAD = userAAD.ObjectIdAAD,
                        Email = userAAD.Email,
                        DisplayName = userAAD.DisplayName,
                        LastLogin = DateTime.UtcNow
                    };
                    um.ID = _dalUser.AddAsync(um, null).Result;
                    um = _dalUser.GetById((int)um.ID, null);

                    bUpdateUser = false;        // No need to synch - we just wrote all we know about.
                    bCheckOrganization = true;  // Check the user's organization.
                }
                else if (listMatchEmailAddress.Count == 1)
                {
                    // For self-service sign-up users, this is the first time they are logging into Profile Designer.
                    um = listMatchEmailAddress[0];
                    if (um.ObjectIdAAD == null)
                    {
                        um.ObjectIdAAD = userAAD.ObjectIdAAD;
                        um.Email = userAAD.Email;
                        um.DisplayName = userAAD.DisplayName;
                        um.LastLogin = DateTime.UtcNow;

                        bUpdateUser = true;         // Synch UserModel changes
                        bCheckOrganization = true;  // Check the user's organization.
                    }
                    else
                    {
                        bErrorCondition = true;
                        strError = $"InitLocalUser||Initialized Profile designer user record found with email {userAAD.Email}. {listMatchEmailAddress.Count} records found. Existing object id = {um.ObjectIdAAD}";
                        _logger.LogWarning(strError);
                    }
                }
                else
                {
                    // When more than 1 record, it means they have signed up (and then left) more than 
                    // once. This is okay, but we pick the most recent (newest) one.
                    // listMatchEmailAddress.Sort((em1, em2) => DateTime?.Compare(em1.Created, em2.Created));
                    listMatchEmailAddress.Sort((em1, em2) => 
                        { 
                            DateTime dt1 = new DateTime(em1.Created.Value.Ticks);
                            DateTime dt2 = new DateTime(em2.Created.Value.Ticks);
                            return DateTime.Compare(dt1, dt2);
                        });

                    int iItem = listMatchEmailAddress.Count - 1;   // We must have at least 2 records this index causes an exception
                    if (iItem < 0) iItem = 0;                      // Just to make sure this is within range

                    // We use the most recent records that we have for this user.
                    um = listMatchEmailAddress[iItem];
                    um.ObjectIdAAD = userAAD.ObjectIdAAD;
                    um.Email = userAAD.Email;
                    um.DisplayName = userAAD.DisplayName;
                    um.LastLogin = DateTime.UtcNow;

                    bUpdateUser = true;         // Synch UserModel changes
                    bCheckOrganization = true;  // Check the user's organization.

                    // ***********************************************************************************************
                    // For whatever reason, we are unable to delete the duplicate records here.
                    // Perhaps the user permission does not allow it.
                    // Removing duplicate records.
                    // We log it, just so there is a record of it.

                    //for (int iDeleteMe = 0; iDeleteMe < listMatchEmailAddress.Count - 1; iDeleteMe++)
                    //{
                    //    if (listMatchEmailAddress[iDeleteMe].ID != null)
                    //    {
                    //        string strWarning = $"InitLocalUser|| About to delete record {iDeleteMe} of {listMatchEmailAddress.Count} from public.user. Id: {listMatchEmailAddress[iDeleteMe].ID.Value} Email: {listMatchEmailAddress[iDeleteMe].Email}";
                    //        _logger.LogWarning(strWarning);

                    //        await _dalUser.DeleteAsync(listMatchEmailAddress[iDeleteMe].ID.Value, base.DalUserToken);
                    //    }
                    //}
                    // ***********************************************************************************************
                }
            }

            if (bErrorCondition)
                return (null, strError);

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
                        bErrorCondition = true;
                        strError = $"InitLocalUser||More than one organization record found with Name = {strFindOrgName}. {listMatchOrganizationName.Count} records found.";
                        _logger.LogWarning(strError);
                    }
                }
            }

            if (bErrorCondition)
                return (null, strError);

            if (bUpdateUser)
                _dalUser.UpdateAsync(um, new UserToken() { UserId = um.ID.Value }).Wait();

            return (um,null);

        }

    }
}
