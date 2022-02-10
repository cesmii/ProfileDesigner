namespace CESMII.ProfileDesigner.DAL.Models
{
    public class PermissionModel : AbstractModel 
    {
        public string Name { get; set; }

        public int CodeName { get; set; }

        public string Description{ get; set; }

        public string NameConcatenated { get; set; }
    }
}