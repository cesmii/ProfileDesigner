using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Data.Entities;
using CESMII.ProfileDesigner.Data.Repositories;
using System.Linq;
using System.Threading.Tasks;

namespace CESMII.ProfileDesigner.DAL
{
    public class OrganizationDAL : BaseDAL<Organization, OrganizationModel>, IDal<Organization, OrganizationModel>
    {
        public OrganizationDAL(IRepository<Organization> repo) : base(repo)
        {
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

        public override async Task<int?> AddAsync(OrganizationModel model, UserToken userToken)
        {
            Organization entity = MapToEntity(model);

            // This will add and call saveChanges
            await _repo.AddAsync(entity);

            // TODO: Have repo return Id of newly created entity
            return entity.ID;
        }

        public override OrganizationModel GetById(int id, UserToken userToken)
        {
            var entity = _repo.FindByCondition(u => u.ID == id).FirstOrDefault();
            return MapToModel(entity);
        }

        protected OrganizationModel MapToModel(Organization org)
        {
            var entity = new OrganizationModel();
            entity.ID = org.ID;
            entity.Name = org.Name;
            return entity;
        }

        protected override OrganizationModel MapToModel(Organization entity, bool verbose = true)
        {
            return MapToModel(entity);
        }
        protected void MapToModel(ref OrganizationModel entity, Organization org)
        {
            entity.ID = org.ID;
            entity.Name = org.Name;
        }

        public Organization MapToEntity(OrganizationModel org) 
        { 
            var entity = new Organization();
            entity.ID = org.ID;
            entity.Name = org.Name;
            return entity;
        }
    }
}
