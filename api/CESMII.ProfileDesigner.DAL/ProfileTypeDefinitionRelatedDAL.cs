namespace CESMII.ProfileDesigner.DAL
{
    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Data.Entities;
    using CESMII.ProfileDesigner.Data.Repositories;

    /// <summary>
    /// Used to call a stored function in DB and return related profile type defs
    /// Get descendants, get ancestors, get dependencies
    /// Calling code needs knowledge of query and columns returned. 
    /// Order by values should use raw column names returned by stored function
    /// For instance, see <see cref="Data.Entities.ProfileTypeDefinitionSimple"/> for column names to use in order by expression.
    /// </summary>
    public class ProfileTypeDefinitionRelatedDAL : 
        BaseStoredProcedureDAL<ProfileTypeDefinitionSimple, ProfileTypeDefinitionSimpleModel>, 
        IStoredProcedureDal<ProfileTypeDefinitionSimpleModel>
    {
        public ProfileTypeDefinitionRelatedDAL(IRepositoryStoredProcedure<ProfileTypeDefinitionSimple> repo) : base(repo)
        {
        }

        ////use base class methods as-is

        protected override ProfileTypeDefinitionSimpleModel MapToModel(ProfileTypeDefinitionSimple entity)
        {
            if (entity != null)
            {
                return new ProfileTypeDefinitionSimpleModel
                {
                    ID = entity.ID,
                    Name = entity.Name,
                    BrowseName = entity.BrowseName,
                    Profile = new ProfileModel() { ID = entity.ProfileId, Namespace = entity.ProfileNamespace, Version = entity.ProfileVersion },
                    Description = entity.Description,
                    Type = new LookupItemModel() { ID = entity.TypeId, Name = entity.TypeName } ,
                    Author = new UserSimpleModel() { ID = entity.AuthorId },
                    OpcNodeId = entity.OpcNodeId,
                    IsAbstract = entity.IsAbstract,
                    Level = entity.Level
                };
            }
            else
            {
                return null;
            }

        }
    }

}