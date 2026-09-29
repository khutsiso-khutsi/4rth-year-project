using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doctor.Models
{
    public class AlertViewModel
    {
        public int PatientID { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int TestRequestID { get; set; }
        public string TestTypeName { get; set; } = string.Empty;
        public decimal? ResultValue { get; set; }
        public DateTime DateFlagged { get; set; }
        public bool IsAbnormal { get; set; }
    }
}