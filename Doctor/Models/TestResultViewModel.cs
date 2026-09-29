using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doctor.Models
{
    public class TestResultViewModel
    {
        public int RequestItemID { get; set; }
        public string TestTypeName { get; set; } = string.Empty;
        public decimal? ResultValue { get; set; }
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
        public bool IsAbnormal { get; set; }
        public string? ResultNotes { get; set; }
        public DateTime? ReleasedDate { get; set; }
    }
}