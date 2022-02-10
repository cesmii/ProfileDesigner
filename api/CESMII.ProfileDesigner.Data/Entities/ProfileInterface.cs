namespace CESMII.ProfileDesigner.Data.Entities
{
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Join table between a profile and an interface (which is a type of profile)
    /// </summary>
    /// <remarks>A profile can belong to many interfaces and an interface can have many profiles.</remarks>
    public class ProfileInterface : AbstractEntity
    {
        [Column(name: "profile_type_definition_id")]
        public int? ProfileTypeDefinitionId { get; set; }

        [Column(name: "interface_id")]
        public int? InterfaceId { get; set; }

        public virtual ProfileTypeDefinition ProfileTypeDefinition { get; set; }

        public virtual ProfileTypeDefinition Interface { get; set; }
    }
}