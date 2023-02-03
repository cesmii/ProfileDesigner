namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Used to call a stored function but only return a count value
    /// </summary>
    public class StoredProcedureCount
    {
        [Column(name: "count")]
        public int NumRows { get; set; }
    }
}