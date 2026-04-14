using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patient.Models
{
    public class TestRequest
    {
        public int RequestID { get; set; }
        public string RequestNumber { get; set; } = "";
        public DateTime RequestDate { get; set; }
        public string Urgency { get; set; } = "";
        public string RequestStatus { get; set; } = "";
        public string? ClinicalNotes { get; set; }
        public string? ReleaseNotes { get; set; }
        public DateTime? ReleasedDate { get; set; }
        public string DoctorName { get; set; } = "";
        public List<TestRequestItem> Items { get; set; } = new();
    }

    public class TestRequestItem
    {
        public int RequestItemID { get; set; }
        public int RequestID { get; set; }
        public string TestName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string ItemStatus { get; set; } = "";
        public decimal? ResultValue { get; set; }
        public string? ResultNotes { get; set; }
        public bool IsAbnormal { get; set; }
        public DateTime? CompletionDateTime { get; set; }
        public DateTime? VerificationDateTime { get; set; }
        public string? UnitName { get; set; }
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
    }
}
