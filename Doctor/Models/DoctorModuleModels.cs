using System;
using System.Collections.Generic;

namespace Doctor.Models
{
    /* -----------------------------------------------------------------
       Shared status vocabulary (feature 3 in the spec).

       IMPORTANT: these string values are NOT the spec's literal wording —
       they are the exact values enforced by the real database's CHECK
       constraints (see TestRequests/TestRequestItems in the attached
       schema). Keep every controller/view that touches request/item
       status referencing these constants instead of hand-typed strings,
       so the values never drift out of sync with the database again.
       The C# member *names* below intentionally still read the way the
       spec described each stage (e.g. ReleasedByDoctor) even though the
       underlying string is the DB's shorter value ("Released") — that
       keeps every other file that already references
       TestRequestStatus.ReleasedByDoctor etc. compiling unchanged.
    ----------------------------------------------------------------- */
    public static class TestRequestStatus
    {
        public const string Submitted = "Submitted";
        public const string SamplesReceived = "Samples Received";
        public const string InProgress = "In Progress";
        public const string Completed = "Completed";
        public const string ReleasedByDoctor = "Released";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All =
        {
            Submitted, SamplesReceived, InProgress, Completed, ReleasedByDoctor, Cancelled
        };

        // A doctor may only cancel while the request hasn't reached the lab yet
        // (matches sp_CancelTestRequest's own check: 'Submitted' or 'Samples Received').
        public static bool CanDoctorCancel(string status) =>
            status == Submitted || status == SamplesReceived;
    }

    /// <summary>
    /// TestRequestItems.ItemStatus has its OWN, different CHECK constraint —
    /// it does NOT include "Cancelled", even though sp_CancelTestRequest
    /// (as written in the real database) tries to set cancelled items'
    /// ItemStatus to 'Cancelled'. That is an existing bug in the shared
    /// database, not something introduced here — flagged to the team
    /// rather than silently patched, since fixing it means altering a
    /// shared CHECK constraint or stored procedure everyone depends on.
    /// </summary>
    public static class TestItemStatus
    {
        public const string Submitted = "Submitted";
        public const string InProgress = "In Progress";
        public const string ToBeReviewed = "To Be Reviewed";
        public const string Verified = "Verified";
        public const string Completed = "Completed";

        public static readonly string[] All =
        {
            Submitted, InProgress, ToBeReviewed, Verified, Completed
        };
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
        public int SampleTypeID { get; set; }
        public string? UnitName { get; set; }
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
        public string? SampleTypeName { get; set; }
    }

    // The full SampleTypes lookup list, used to populate the per-barcode
    // sample-type dropdown in TestRequests.cshtml.
    public class SampleTypeOption
    {
        public int SampleTypeID { get; set; }
        public string SampleTypeName { get; set; } = "";
    }

    // One row of the "+ Add Sample" barcode UI: a barcode number paired with
    // the sample type it was collected into. sp_CreateTestRequest requires a
    // SampleTypeID per barcode (JSON: [{"BarcodeNumber":"...","SampleTypeID":n}]).
    public class SampleBarcodeInput
    {
        public string BarcodeNumber { get; set; } = "";
        public int SampleTypeID { get; set; }
    }

    public class CreateTestRequestViewModel
    {
        public int PatientID { get; set; }
        public string Urgency { get; set; } = "Routine";      // Routine / Urgent / STAT
        public string? ClinicalNotes { get; set; }
        public List<int> TestTypeIDs { get; set; } = new();
        public List<SampleBarcodeInput> Samples { get; set; } = new();

        public List<PatientRecordListItem> Patients { get; set; } = new();
        public List<TestTypeOption> TestTypes { get; set; } = new();
        public List<SampleTypeOption> SampleTypes { get; set; } = new();
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

    // Maps to the real dbo.SampleBarcodes table.
    public class TestRequestSample
    {
        public int BarcodeID { get; set; }
        public string BarcodeNumber { get; set; } = "";
        public string? SampleTypeName { get; set; }
        public DateTime CollectionDate { get; set; }
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
