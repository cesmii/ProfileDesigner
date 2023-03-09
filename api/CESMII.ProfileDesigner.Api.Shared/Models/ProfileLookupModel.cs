namespace CESMII.ProfileDesigner.Api.Shared.Models
{
    using System.Collections.Generic;
    using CESMII.ProfileDesigner.DAL.Models;

    public class ProfileLookupModel
    {
        public List<ProfileTypeDefinitionSimpleModel> Compositions { get; set; }
        public List<ProfileTypeDefinitionModel> Interfaces { get; set; }
        public List<ProfileTypeDefinitionSimpleModel> VariableTypes { get; set; }
    }

}
