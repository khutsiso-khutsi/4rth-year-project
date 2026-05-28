using System;
using System.Collections.Generic;

namespace Patient.Models
{
    public class ConsentViewModel
    {
        public int SelectedDoctorID { get; set; }
        public List<DoctorOption> AllDoctors { get; set; } = new();
        public List<DoctorConsent> Consents { get; set; } = new();
        public List<ConsentTestRequest> TestRequests { get; set; } = new();
    }

    public class DoctorOption
    {
        public int DoctorID { get; set; }
        public string DoctorName { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public class DoctorConsent
    {
        public int ConsentID { get; set; }
        public int DoctorID { get; set; }
        public string DoctorName { get; set; } = "";
        public string DoctorEmail { get; set; } = "";
        public bool ConsentGranted { get; set; }
        public DateTime GrantedDate { get; set; }
        public DateTime? RevokedDate { get; set; }
    }

    public class ConsentTestRequest
    {
        public int RequestID { get; set; }
        public string RequestNumber { get; set; } = "";
        public DateTime RequestDate { get; set; }
        public string RequestStatus { get; set; } = "";
        public bool IsShared { get; set; }
        public string? Urgency { get; set; }
    }
}