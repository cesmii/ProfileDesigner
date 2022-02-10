namespace CESMII.ProfileDesigner.Api.Shared.Models
{
    using System.Collections.Generic;
    using CESMII.ProfileDesigner.DAL.Models;

    public class ProfileTypeDefinitionSearchResult<T> : DALResult<T>
    {
        public List<ProfileModel> Profiles { get; set; }
    }

}
