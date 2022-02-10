namespace CESMII.ProfileDesigner.Api.Shared.Models
{
    public class LoginModel
    {
        public string UserName { get; set; }

        public string Password { get; set; }
    }

    public class LoginResultModel
    {
        public string Token { get; set; }

        public bool IsImpersonating { get; set; }

        public DAL.Models.UserModel User { get; set; }
    }

}
