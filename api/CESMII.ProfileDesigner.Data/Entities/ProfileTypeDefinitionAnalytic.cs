namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    public class ProfileTypeDefinitionAnalytic : AbstractEntity
    {
        [Column(name: "profile_type_definition_id")]
        public int ProfileTypeDefinitionId { get; set; }

        [Column(name: "page_visit_count")]
        public int PageVisitCount { get; set; }

        [Column(name: "extend_count")]
        public int ExtendCount { get; set; }

        /// <summary>
        /// Provide a mechanism to manually increase popularity separate from the page visit and extend tracking
        /// </summary>
        [Column(name: "manual_rank")]
        public int ManualRank { get; set; }

        public virtual ProfileTypeDefinition ProfileTypeDefinition { get; set; }
    }
}
