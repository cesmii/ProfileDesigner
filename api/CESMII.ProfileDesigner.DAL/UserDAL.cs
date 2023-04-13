namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    
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
        public override async Task<int?> AddAsync(UserModel model, UserToken userToken = null)
        {
            var entity = new User
            {
                ID = null
                ,Created = DateTime.UtcNow
                //not actually used during registration but keep it here because db expects non null.
                //if future, the user sets their own pw on register complete
                //,Password = PasswordUtils.EncryptNewPassword(_configUtil.PasswordConfigSettings.EncryptionSettings, password)
            };

            if (userToken == null)
                userToken = new UserToken();

            this.MapToEntity(ref entity, model, userToken);
            //do this after mapping to enforce isactive is true on add

            //this will add and call saveChanges
            await _repo.AddAsync(entity);

            model.ID = entity.ID;
            // Return id for newly added user
            return entity.ID;
        }

        /// <summary>
        /// Get user
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override UserModel GetById(int id, UserToken userToken)
        {
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();
            return MapToModel(entity, true);
        }

        /// <summary>
        /// Get user by user's Azure AAD
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public UserModel GetByIdAAD(string userIdAAD)
        {
            var entity = _repo.FindByCondition(x => x.ObjectIdAAD.ToLower().Equals(userIdAAD))
                .FirstOrDefault();
            return MapToModel(entity, true);
        }

        /// <summary>
        /// Get all rules and related data
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override DALResult<UserModel> GetAllPaged(UserToken userToken, int? skip, int? take, bool returnCount = false, bool verbose = false)
        {
            var query = _repo.GetAll()
                .OrderBy(u => u.DisplayName);

            var count = returnCount ? query.Count() : 0;

            IQueryable<User> data;
            if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            else if (skip.HasValue) data = query.Skip(skip.Value);
            else if (take.HasValue) data = query.Take(take.Value);
            else data = query;

            var result = new DALResult<UserModel>
            {
                Count = count,
                Data = MapToModels(data.ToList(), verbose),
                SummaryData = null
            };
            return result;
        }

        /// <summary>
        /// Get all rules and related data
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        public override List<UserModel> GetAll(UserToken userToken, bool verbose = false)
        {
            var result = _repo.GetAll()
                .OrderBy(u => u.DisplayName)
                .ToList();
            return MapToModels(result, verbose);
        }

        /// <summary>
        /// This should be used when getting all items with some filter determined by the calling code.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public override DALResult<UserModel> Where(Expression<Func<User, bool>> predicate, UserToken user, int? skip = null, int? take = null, 
            bool returnCount = false, bool verbose = false)
        {
            return base.Where(predicate, user, skip, take, returnCount, verbose,
                q => q
                    .OrderBy(u => u.DisplayName));
        }

        public override async Task<int?> UpdateAsync(UserModel item, UserToken userToken)
        {
            //TBD - if userId is not same as item.id, then check permissions of userId before updating
            var entity = _repo.FindByCondition(x => x.ID == item.ID)
                .FirstOrDefault();
            this.MapToEntity(ref entity, item, userToken);

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return entity.ID;
        }

        public async Task<int?> DeleteAsync(int id, UserToken userToken)
        {
            //perform a soft delete by setting active to false
            var entity = _repo.FindByCondition(x => x.ID == id)
                .FirstOrDefault();

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return entity.ID;
        }


        protected override UserModel MapToModel(User entity, bool verbose = true)
        {
            if (entity != null)
            {
                return new UserModel
                {
                    ID = entity.ID,
                    ObjectIdAAD = entity.ObjectIdAAD,
                    DisplayName = entity.DisplayName,
                    //AAD - certain data no longer stored in db
                    Organization = entity.Organization == null ? null : new OrganizationModel() { ID = entity.Organization.ID, Name = entity.Organization.Name },
                    Created = entity.Created,
                    LastLogin = entity.LastLogin,
                    Email = entity.EmailAddress,
                    SelfServiceSignUp_Organization_Name = entity.Oranization_Name,
                    SelfServiceSignUp_IsCesmiiMember = entity.CesmiiMember,
                };
            }
            else
            {
                return null;
            }

        }

        protected override void MapToEntity(ref User entity, UserModel model, UserToken userToken)
        {
            entity.ObjectIdAAD = model.ObjectIdAAD;
            entity.LastLogin = model.LastLogin;
            entity.DisplayName = model.DisplayName;
            entity.OrganizationId = model.Organization?.ID;
            entity.EmailAddress = model.Email;

            entity.Oranization_Name = model.SelfServiceSignUp_Organization_Name;
            entity.CesmiiMember = model.SelfServiceSignUp_IsCesmiiMember;
        }
    }
}