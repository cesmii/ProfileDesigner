namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;

    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Data.Entities;
    using CESMII.ProfileDesigner.Data.Repositories;
    using CESMII.ProfileDesigner.Common;

    /// <summary>
    /// Interact with User Data. 
    /// This is a enhanced version of the typical DAL class in that it has 
    /// additional handling for login validation, password handling, 
    /// handling updates for multiple repos at one time, etc. 
    /// </summary>
    public class UserDAL : BaseDAL<User, UserModel>, IDal<User, UserModel>
    {
        protected readonly ConfigUtil _configUtil;

        public UserDAL(IRepository<User> repo, ConfigUtil configUtil) : base(repo)
        {
            _configUtil = configUtil;
        }

        /// <summary>
        /// The user Add flow works differently than other adds. After adding the record here, the calling code will also 
        /// generate a token to send to user so they can complete registration. 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        //add this layer so we can instantiate the new entity here.
        public override async Task<int?> Add(UserModel model, UserToken userToken)
        {
            //generate random password and then encrypt in here. 
            var password = PasswordUtils.GenerateRandomPassword(_configUtil.PasswordConfigSettings.RandomPasswordLength);

            User entity = new User
            {
                ID = null
                ,Created = DateTime.UtcNow
                //not actually used during registration but keep it here because db expects non null.
                //The user sets their own pw on register complete
                ,Password = PasswordUtils.EncryptNewPassword(_configUtil.PasswordConfigSettings.EncryptionSettings, password)
            };

            this.MapToEntity(ref entity, model, userToken);
            //do this after mapping to enforce isactive is true on add
            entity.IsActive = true;

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            model.ID = entity.ID;
            // Return id for newly added user
            return entity.ID;
        }

        /// <summary>
        /// Get rule and related data
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public async Task<UserModel> Validate(string userName, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                // Null value is used on initial creation, therefore null may not be passed into this method.
                var ex = new ArgumentNullException(password, "Password required.");
                _logger.Error(ex); // Log this within all targets as an error.
                throw ex; // Throw an explicit exception, those using this should be aware this cannot be allowed.
            }

            //TBD - Mock for now, just validate username and passowrd is same. Add in real validation once we store user info
            //TBD - Mock user...addin some additional settings, permissions manually for now...
            if (_useMock)
            {
                var mock = _repoMock.FindByCondition(u => u.UserName.ToLower().Equals(userName.ToLower()) &&
                    u.UserName.ToLower().Equals(password.ToLower())).FirstOrDefault();
                if (mock == null) return null;
                var user = this.MapToModel(mock);
                //mock the permissions...
                user.PermissionNames = new List<string>() { Common.Enums.PermissionEnum.CanViewProfile.ToString(), Common.Enums.PermissionEnum.CanManageProfile.ToString(), Common.Enums.PermissionEnum.CanDeleteProfile.ToString() };
                user.PermissionIds = new List<int?>() { (int)Common.Enums.PermissionEnum.CanViewProfile, (int)Common.Enums.PermissionEnum.CanManageProfile, (int)Common.Enums.PermissionEnum.CanDeleteProfile };
                return user;
            }

            //1. Validate against our encryption. Because we use the existing user's settings, we get the 
            // existing pw, parse it into parts and encrypt the new pw with the same settings to see if it matches
            var result = _repo.FindByCondition(u => u.UserName.ToLower() == userName.ToLower() && u.IsActive && u.RegistrationComplete.HasValue)
                .Include(p => p.UserPermissions)
                .FirstOrDefault();
            if (result == null) return null;

            //test against our encryption, means we match 
            bool updateEncryptionLevel = false;
            if (PasswordUtils.ValidatePassword(_configUtil.PasswordConfigSettings.EncryptionSettings, result.Password, password, out updateEncryptionLevel))
            {
                //if the encryption level has been upgraded since original encryption, upgrade their pw now. 
                if (updateEncryptionLevel)
                {
                    result.Password = PasswordUtils.EncryptNewPassword(_configUtil.PasswordConfigSettings.EncryptionSettings, password);
                }
                result.LastLogin = DateTime.UtcNow;
                await _repo.UpdateAsync(result);
                await _repo.SaveChanges();
                return this.MapToModel(result);
            }

            // No user match found, username or password incorrect. 
            return null;
        }

        /// <summary>
        /// Complete user registration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public async Task<UserModel> CompleteRegistration(int id, string userName, string newPassword)
        {
            //get user - match on user id, user name and is active
            var result = _repo.FindByCondition(u => u.ID.Equals(id) && u.UserName.ToLower().Equals(userName) && u.IsActive) 
                .Include(p => p.UserPermissions)
                .FirstOrDefault();
            if (result == null) return null;

            //only allow completing registration if NOT already completed
            if (result.RegistrationComplete.HasValue)
            {
                // Null value is used on initial creation, therefore null may not be passed into this method.
                var ex = new InvalidOperationException("User has already completed registration.");
                _logger.Error(ex); // Log this within all targets as an error.
                throw ex; // Throw an explicit exception, those using this should be aware this cannot be allowed.
            }

            //encrypt and save password w/ profile
            result.Password = PasswordUtils.EncryptNewPassword(_configUtil.PasswordConfigSettings.EncryptionSettings, newPassword);
            result.LastLogin = DateTime.UtcNow;
            result.RegistrationComplete = DateTime.UtcNow;
            await _repo.UpdateAsync(result);
            await _repo.SaveChanges();
            return this.MapToModel(result);
        }

        /// <summary>
        /// Complete user registration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public async Task<UserModel> ResetPassword(int id, string userName, string newPassword)
        {
            //get user - match on user id, user name and is active
            var result = _repo.FindByCondition(u => u.ID.Equals(id) && u.UserName.ToLower().Equals(userName) && u.IsActive)
                .Include(p => p.UserPermissions)
                .FirstOrDefault();
            if (result == null) return null;

            //encrypt and save password w/ profile
            result.Password = PasswordUtils.EncryptNewPassword(_configUtil.PasswordConfigSettings.EncryptionSettings, newPassword);
            result.LastLogin = DateTime.UtcNow;
            await _repo.UpdateAsync(result);
            await _repo.SaveChanges();
            return this.MapToModel(result);
        }
        
        /// <summary>
        /// Update the user's pasword
        /// </summary>
        /// <remarks>
        /// This assumes the controller does all the proper validation and passes a non-encrypted value
        /// </remarks>
        /// <param name="user"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public async Task<int> ChangePassword(int id, string oldPassword, string newPassword)
        {
            var existingUser = _repo.GetByID(id);
            if (existingUser == null || !existingUser.IsActive) throw new ArgumentNullException($"User not found with id {id}");

            //validate existing password
            if (this.Validate(existingUser.UserName, oldPassword) == null)
            {
                throw new ArgumentNullException($"Change Password - Old Password does not match for user {id}");
            }

            //Encrypt new password 
            existingUser.Password = PasswordUtils.EncryptNewPassword(_configUtil.PasswordConfigSettings.EncryptionSettings, newPassword);
            //save changes
            await _repo.UpdateAsync(existingUser);
            return await _repo.SaveChanges();
        }

        /// <summary>
        /// Get user
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override UserModel GetById(int id, UserToken userToken)
        {
            //TBD - temp mock data
            if (_useMock)
            {
                var mock = _repoMock.GetByID(id);
                return MapToModel(mock);
            }

            var entity = _repo.FindByCondition(x => x.ID == id)
                .Include(u => u.UserPermissions)
                .FirstOrDefault();
            return MapToModel(entity, true);
        }

        /// <summary>
        /// Get all rules and related data
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override List<UserModel> GetAll(UserToken userToken, bool verbose = false)
        {
            var result = _repo.GetAll()
                //.Where(u => u.IsActive)  //TBD - ok to return inactive in the list of users?
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.UserName)
                .Include(u => u.UserPermissions)
                .ToList();
            return MapToModels(result, verbose);
        }

        /// <summary>
        /// This should be used when getting all items with some filter determined by the calling code.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<UserModel> Where(Expression<Func<User, bool>> predicate, UserToken user, int? skip, int? take, 
            bool returnCount = true, bool verbose = false)
        {
            return base.Where(predicate, user, skip, take, returnCount, verbose,
                q => q
                    .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.UserName)
                    //.Where(u => u.IsActive)  //TBD - ok to return inactive in the list of users?
                    .Include(u => u.UserPermissions));
            //var query = _repo.FindByCondition(predicate)
            //    .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.UserName)
            //    //.Where(u => u.IsActive)  //TBD - ok to return inactive in the list of users?
            //    .Include(u => u.UserPermissions);
            //var count = returnCount ? query.Count() : 0;
            ////query returns IincludableQuery. Jump through the following to find right combo of skip and take
            ////Goal is to have the query execute and not do in memory skip/take
            //IQueryable<User> data;
            //if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            //else if (skip.HasValue) data = query.Skip(skip.Value);
            //else if (take.HasValue) data = query.Take(take.Value);
            //else data = query;

            //DALResult<UserModel> result = new DALResult<UserModel>();
            //result.Count = count;
            //result.Data = MapToModels(data.ToList(), verbose);
            //result.SummaryData = null;
            //return result;
        }

        public override async Task<int?> Update(UserModel item, UserToken userToken)
        {
            //TBD - if userId is not same as item.id, then check permissions of userId before updating
            var entity = _repo.FindByCondition(x => x.ID == item.ID)
                .Include(p => p.UserPermissions)
                .FirstOrDefault();
            this.MapToEntity(ref entity, item, userToken);

            await _repo.UpdateAsync(entity);
            await _repo.SaveChanges();

            return entity.ID;
        }

        public async Task<int?> Delete(int id, UserToken userToken)
        {
            //perform a soft delete by setting active to false
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();
            entity.IsActive = false;

            await _repo.UpdateAsync(entity);
            await _repo.SaveChanges();

            return entity.ID;
        }


        protected override UserModel MapToModel(User entity, bool verbose = false)
        {
            if (entity != null)
            {
                return new UserModel
                {
                    ID = entity.ID,
                    Email = entity.Email,
                    UserName = entity.UserName,
                    PermissionNames = entity.UserPermissions == null ? new List<string>() : entity.UserPermissions.Select(s => s.Permission.Name).ToList(),
                    PermissionIds = entity.UserPermissions == null ? new List<int?>() : entity.UserPermissions.Select(s => s.Permission.ID).ToList(),
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    Organization = entity.Organization == null ? null : new OrganizationModel() { ID = entity.Organization.ID, Name = entity.Organization.Name },
                    Created = entity.Created,
                    LastLogin = entity.LastLogin,
                    IsActive = entity.IsActive,
                    RegistrationComplete = entity.RegistrationComplete,
                };
            }
            else
            {
                return null;
            }

        }

        protected override void MapToEntity(ref User entity, UserModel model, UserToken userToken)
        {
            entity.UserName = model.UserName;
            entity.Email = model.Email;
            entity.FirstName = model.FirstName;
            entity.LastName = model.LastName;
            entity.IsActive = model.IsActive;
            //entity.LastLogin = model.LastLogin;

            //handle update of user permissions
            MapToEntityPermissions(ref entity, model.PermissionIds);
        }

        /// <summary>
        /// This will reconcile the incoming permission list against the user's current list.
        /// The incoming list will be the one we are updating to match.
        /// </summary>
        /// <remarks>The permissions work off permission id.</remarks>
        /// <param name="entity"></param>
        /// <param name="permissions"></param>
        protected void MapToEntityPermissions(ref User entity, List<int?> permissions)
        {
            //init visit services for new scenario
            if (entity.UserPermissions == null) entity.UserPermissions = new List<UserPermission>();

            // Remove permissions no longer assigned
            // Use counter from end of collection so we can remove and not mess up loop iterator 
            if (entity.UserPermissions.Count > 0)
            {
                var length = entity.UserPermissions.Count - 1;
                for (var i = length; i >= 0; i--)
                {
                    var currentId = entity.UserPermissions[i].Permission.ID;

                    //remove if no longer present
                    if (!permissions.Contains(currentId))
                    {
                        entity.UserPermissions.RemoveAt(i);
                    }
                    else
                    {
                        //do nothing if still present
                    }
                }
            }

            // Loop over perms passed in and only add those not already there
            if (permissions != null)
            {
                foreach (var id in permissions)
                {
                    if (entity.UserPermissions.Find(s => s.PermissionId.Equals(id)) == null)
                    {
                        entity.UserPermissions.Add(new UserPermission
                        {
                            PermissionId = id,
                            UserId = entity.ID
                        });
                    }
                }
            }
        }

    }
}