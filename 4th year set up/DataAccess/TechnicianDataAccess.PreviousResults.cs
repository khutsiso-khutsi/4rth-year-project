using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Microsoft.Data.SqlClient;
using _4th_year_set_up.Models;

namespace _4th_year_set_up.DataAccess
{
    // Second half of TechnicianDataAccess (the class is declared "partial"), so the main file does not need editing.
    // Reads the patient's last verified result of the same test type, for the Previous and Change columns on Capture Results.
    public partial class TechnicianDataAccess
    {
        // The patient's most recent verified result of the same test type on an earlier request, per parameter.
        // Null when there is none, so the Previous and Change columns stay hidden.
        public (Dictionary<string, decimal> Values, DateTime At)? GetPreviousResults(TestItem item)
        {
            if (_sample) return null;
            try
            {
                using var db = Open();
                var rows = db.Query<PrevRow>("dbo.sp_Tech_GetPreviousResults", new { RequestItemID = item.ItemId },
                                             commandType: CommandType.StoredProcedure).ToList();
                if (rows.Count == 0) return null;
                return (rows.GroupBy(r => r.ParameterName)
                            .ToDictionary(g => g.Key, g => g.First().ResultValue, StringComparer.OrdinalIgnoreCase),
                        rows[0].VerifiedAt);
            }
            catch (SqlException) { return null; }      // procedure not created yet
        }

        private class PrevRow
        {
            public string ParameterName { get; set; }
            public decimal ResultValue { get; set; }
            public DateTime VerifiedAt { get; set; }
        }
    }
}
