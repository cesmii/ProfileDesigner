namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    public class ProfileTypeDefinitionFavorite : AbstractEntityWithTenant
    {
        [Column(name: "profile_type_definition_id")]
        public int ProfileTypeDefinitionId { get; set; }

        [Column(name: "is_favorite")]
        public bool IsFavorite { get; set; }

        public virtual ProfileTypeDefinition ProfileTypeDefinition { get; set; }
    }
}
