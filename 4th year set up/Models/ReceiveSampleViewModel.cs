using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace _4th_year_set_up.Models    
{
        /// <summary>
        /// Page model for GET /TechnicianDashboard/ReceiveSamples
        /// </summary>
        public class ReceiveSamplesViewModel
        {
            public List<PendingSampleViewModel> PendingSamples { get; set; } = new();

            // Stat card figures for the top of the page
            public int AwaitingReceiptCount { get; set; }
            public int ReceivedTodayCount { get; set; }
            public int RejectedTodayCount { get; set; }
        }

        /// <summary>
        /// One row on the "Samples Awaiting Check-In" table.
        /// Represents a Sample record that the doctor registered at request
        /// creation but that has not yet been physically received/rejected.
        /// </summary>
        public class PendingSampleViewModel
        {
            public int SampleId { get; set; }
            public int RequestId { get; set; }
            public string RequestNumber { get; set; } = string.Empty;

            public string PatientName { get; set; } = string.Empty;
            public int PatientAge { get; set; }

            // What the doctor logged when the request was submitted.
            // The technician confirms the physical sample against these.
            public string ExpectedBarcode { get; set; } = string.Empty;
            public string ExpectedSampleType { get; set; } = string.Empty;

            public string TestTypeNames { get; set; } = string.Empty; // comma-separated, for display only
            public string OrderingDoctorName { get; set; } = string.Empty;
            public string Urgency { get; set; } = "Routine"; // Routine / Urgent / Stat
        }

        /// <summary>
        /// Posted from the "Receive Sample" modal.
        /// </summary>
        public class ReceiveSampleRequest
        {
            [Required]
            public int SampleId { get; set; }

            [Required(ErrorMessage = "Please scan or enter the sample barcode.")]
            [StringLength(50)]
            public string ScannedBarcode { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm the sample type received.")]
            public string ConfirmedSampleType { get; set; } = string.Empty;
        }

        /// <summary>
        /// Posted from the "Reject Sample" modal.
        /// </summary>
        public class RejectSampleRequest
        {
            [Required]
            public int SampleId { get; set; }

            [Required(ErrorMessage = "A reason is required to reject a sample.")]
            [StringLength(500, ErrorMessage = "Reason must be 500 characters or fewer.")]
            public string Reason { get; set; } = string.Empty;
        }

        /// <summary>
        /// Uniform result shape returned by the service layer so the controller
        /// can translate either outcome into a JSON response for the page's fetch() calls.
        /// </summary>
        public class SampleActionResult
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }

            // Only relevant on successful receipt - lets the front end know
            // whether the whole TestRequest flipped to "Samples Received"
            // (i.e. this was the last outstanding sample) or is still partial.
            public string? NewRequestStatus { get; set; }

            public static SampleActionResult Fail(string message) => new() { Success = false, ErrorMessage = message };
            public static SampleActionResult Ok(string? newRequestStatus = null) => new() { Success = true, NewRequestStatus = newRequestStatus };
        }
    }