namespace CESMII.ProfileDesigner.DAL.Models
{
    public class ProfileTypeDefinitionFavoriteModel : AbstractModel
    {
        public int OwnerId { get; set; }

        public int ProfileTypeDefinitionId { get; set; }

        public bool IsFavorite { get; set; }
    }
}