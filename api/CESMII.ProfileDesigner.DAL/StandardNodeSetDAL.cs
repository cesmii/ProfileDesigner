namespace CESMII.ProfileDesigner.DAL
{
    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Data.Entities;
    using CESMII.ProfileDesigner.Data.Repositories;
    using System.Linq;
    using System.Threading.Tasks;

    public class StandardNodeSetDAL : BaseDAL<StandardNodeSet, StandardNodeSetModel>, IDal<StandardNodeSet, StandardNodeSetModel>
    {
        public StandardNodeSetDAL(IRepository<StandardNodeSet> repo) : base(repo)
        {
        }

        public override async Task<int?> AddAsync(StandardNodeSetModel model, UserToken userToken)
        {
            StandardNodeSet entity = new StandardNodeSet
            {
                ID = null,
            };

            this.MapToEntity(ref entity, model, userToken);
            await base.AddAsync(entity, model, userToken);
            model.ID = entity.ID;
            return entity.ID;
        }

        public override async Task<int?> UpdateAsync(StandardNodeSetModel model, UserToken userToken)
        {
            StandardNodeSet entity = base.FindByCondition(userToken, x => x.ID == model.ID).FirstOrDefault();
            this.MapToEntity(ref entity, model, userToken);

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();
            return entity.ID;
        }

        public async Task<int?> DeleteAsync(int id, UserToken userToken)
        {
            StandardNodeSet entity = base.FindByCondition(userToken, x => x.ID == id).FirstOrDefault();

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();
            return entity.ID;
        }


        protected override StandardNodeSetModel MapToModel(StandardNodeSet entity, bool verbose = false)
        {
            if (entity != null)
            {
                return new StandardNodeSetModel
                {
                    ID = entity.ID,
                    Namespace = entity.Namespace,
                    Version = entity.Version,
                    Filename = entity.Filename,
                    PublishDate=entity.PublishDate
                };
            }
            else
            {
                return null;
            }

        }

        protected override void MapToEntity(ref StandardNodeSet entity, StandardNodeSetModel model, UserToken userToken)
        {
            entity.Namespace = model.Namespace;
            entity.Version = model.Version;
            entity.Filename = model.Filename;
            entity.PublishDate = model.PublishDate;
        }
    }
}