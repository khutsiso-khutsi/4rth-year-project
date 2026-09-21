using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doctor.Models
{
    public class TestRequestViewModel
    {
        public int RequestID { get; set; }
        [Required] public int PatientID { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.Now;
        [Required] public string Urgency { get; set; } = string.Empty; // routine/urgent/stat
        public string? ClinicalNotes { get; set; }
        public List<int> SelectedTestTypeIDs { get; set; } = new();
        public List<string> Barcodes { get; set; } = new();
        public string Status { get; set; } = "Submitted";
    }
}
