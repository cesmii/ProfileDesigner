namespace CESMII.ProfileDesigner.Api.Shared.Models
{
    using System.Collections.Generic;
    using CESMII.ProfileDesigner.DAL.Models;

    public class ProfileExplorerModel
    {
        public ProfileTypeDefinitionModel Profile { get; set; }

        public List<ProfileTypeDefinitionSimpleModel> Dependencies { get; set; }

        //public ProfileItemSimpleModel TreeView { get; set; }
        public List<ProfileTypeDefinitionAncestoryModel> Tree { get; set; }

        public List<ProfileTypeDefinitionModel> Interfaces { get
            {
                return this.Profile.Interfaces == null ? new List<ProfileTypeDefinitionModel>() : this.Profile.Interfaces;
            }
        }

        public List<ProfileTypeDefinitionRelatedModel> Compositions
        {
            get
            {
                return this.Profile.Compositions == null ? new List<ProfileTypeDefinitionRelatedModel>() : this.Profile.Compositions;
            }
        }
    }

}
