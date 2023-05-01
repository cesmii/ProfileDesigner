
namespace CESMII.ProfileDesigner.DAL.Models
{
    using CESMII.Common.SelfServiceSignUp.Models;
    using CESMII.ProfileDesigner.DAL.Models;

    public class UserSignUpData : IUserSignUpData
    {
        private readonly UserDAL _dalUser;

        public UserSignUpData(UserDAL dal)
        {
            _dalUser = dal;
        }

        /// <summary>
        /// Search for user by email address.
        /// </summary>
        /// <param name="strEmail"></param>
        /// <returns></returns>
        public int Where(string strEmail)
        {
            var mylist = _dalUser.Where(x => x.EmailAddress.ToLower().Equals(strEmail.ToLower()), null).Data;
            return mylist.Count;
        }

        /// <summary>
        /// Add user to database.
        /// </summary>
        /// <param name="usersum"></param>
        public async void AddUser(UserSignUpModel usersum)
        {
            UserModel um = new UserModel()
            {
                DisplayName = usersum.DisplayName,
                Email = usersum.Email,
                SelfServiceSignUp_Organization_Name = usersum.Organization,
                SelfServiceSignUp_IsCesmiiMember = usersum.IsCesmiiMember
            };

            await _dalUser.AddAsync(um);
        }
    }
}
