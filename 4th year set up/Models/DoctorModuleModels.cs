using System;
using System.Collections.Generic;

namespace _4th_year_set_up.Models
{
    /* -----------------------------------------------------------------
       Shared status vocabulary (feature 3 in the spec). Keep every
       controller/view that touches request/item status referencing
       these constants instead of hand-typed strings, so the six values
       never drift out of sync again.
    ----------------------------------------------------------------- */
    public static class TestRequestStatus
    {
        public const string Submitted = "Submitted";
        public const string SamplesReceived = "Sample(s) received";
        public const string InProgress = "In progress";
        public const string Completed = "Completed";
        public const string ReleasedByDoctor = "Released by doctor";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All =
        {
            Submitted, SamplesReceived, InProgress, Completed, ReleasedByDoctor, Cancelled
        };

        // A doctor may only cancel while the request hasn't reached the lab yet.
        public static bool CanDoctorCancel(string status) =>
            status == Submitted || status == SamplesReceived;
    }

    // ---- 1. Manage Patient Records --------------------------------------

    public class PatientRecordListItem
    {
        public int PatientID { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string IDNumber { get; set; } = "";
        public DateTime DateOfBirth { get; set; }
        public string CellphoneNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName => $"{FirstName} {LastName}";
    }

    public class NewPatientViewModel
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string IDNumber { get; set; } = "";
        public DateTime DateOfBirth { get; set; }
        public string CellphoneNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public string? HomeAddress { get; set; }

        // Conditions/allergies/medications the doctor captures for a brand-new patient.
        public List<int> ConditionIDs { get; set; } = new();
        public List<int> AllergyIDs { get; set; } = new();
        public List<int> MedicationIDs { get; set; } = new();
    }

    // ---- 2. Create Test Requests ----------------------------------------

    public class TestTypeOption
    {
        public int TestTypeID { get; set; }
        public string TestName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string? UnitName { get; set; }
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
        public string? SampleTypeName { get; set; }
    }

    public class CreateTestRequestViewModel
    {
        public int PatientID { get; set; }
        public string Urgency { get; set; } = "Routine";      // Routine / Urgent / STAT
        public string? ClinicalNotes { get; set; }
        public List<int> TestTypeIDs { get; set; } = new();
        public List<string> Barcodes { get; set; } = new();

        public List<PatientRecordListItem> Patients { get; set; } = new();
        public List<TestTypeOption> TestTypes { get; set; } = new();
    }

    // ---- 3/4. Track status + View results ---------------------------------

    public class DoctorTestRequestSummary
    {
        public int RequestID { get; set; }
        public string RequestNumber { get; set; } = "";
        public DateTime RequestDate { get; set; }
        public string Urgency { get; set; } = "";
        public string RequestStatus { get; set; } = "";
        public string? ClinicalNotes { get; set; }
        public string? ReleaseNotes { get; set; }
        public DateTime? ReleasedDate { get; set; }
        public string? CancelReason { get; set; }
        public string? CancelledBy { get; set; }
        public int PatientID { get; set; }
        public string PatientName { get; set; } = "";
        public string PatientEmail { get; set; } = "";
        public List<TestRequestItemDetail> Items { get; set; } = new();
        public List<TestRequestSample> Samples { get; set; } = new();

        public bool HasAbnormalResult => Items.Exists(i => i.IsAbnormal);
        public bool CanCancel => TestRequestStatus.CanDoctorCancel(RequestStatus);
    }

    public class TestRequestItemDetail
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

    public class TestRequestSample
    {
        public int SampleID { get; set; }
        public string BarcodeValue { get; set; } = "";
        public DateTime? CollectedDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
    }

    public class ReleaseResultsViewModel
    {
        public int RequestID { get; set; }
        public string ReleaseNotes { get; set; } = "";
        // When true, the patient is emailed to book an appointment rather than
        // being sent the raw results directly ("View Results" feature, item 4).
        public bool AskPatientToBookAppointment { get; set; }
    }

    public class CancelTestRequestViewModel
    {
        public int RequestID { get; set; }
        public string Reason { get; set; } = "";
    }

    // ---- 5. Alerts ----------------------------------------------------------

    public class AbnormalAlertViewModel
    {
        public int RequestItemID { get; set; }
        public int RequestID { get; set; }
        public string RequestNumber { get; set; } = "";
        public DateTime RequestDate { get; set; }
        public int PatientID { get; set; }
        public string PatientName { get; set; } = "";
        public string TestName { get; set; } = "";
        public decimal? ResultValue { get; set; }
        public string? UnitName { get; set; }
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
        public string? ResultNotes { get; set; }
        public DateTime? CompletionDateTime { get; set; }
    }

    public class AlertsPageViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<AbnormalAlertViewModel> Alerts { get; set; } = new();
    }

    // ---- 6. Doctor Reports (PDF) --------------------------------------------

    public class DoctorReportViewModel
    {
        public string DoctorName { get; set; } = "";
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<DoctorTestRequestSummary> Requests { get; set; } = new();
    }
}
