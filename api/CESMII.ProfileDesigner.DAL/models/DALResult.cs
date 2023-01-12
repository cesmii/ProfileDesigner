namespace CESMII.ProfileDesigner.DAL.Models
{
    using System.Collections.Generic;

    public class DALResult<T>
    {
        // Record count
        public int Count { get; set; }

        // The actual data as a list of type <T>
        public List<T> Data { get; set; }

        // A list of the summary of the data, but could be average etc. hence list.
        public List<T> SummaryData { get; set; }
        public string StartCursor { get; set; }
        public string EndCursor { get; set; }
        public bool? HasNextPage { get; set; }
        public bool? HasPreviousPage { get; set; }
    }
}