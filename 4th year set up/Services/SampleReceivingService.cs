//using _4th_year_set_up.Models;
////using Microsoft.EntityFrameworkCore;

//namespace _4th_year_set_up.Services
//{
//    public interface ISampleReceivingService
//    {
//        Task<ReceiveSamplesViewModel> GetPendingSamplesAsync();

//        Task<SampleActionResult> ReceiveSampleAsync(
//            int sampleId,
//            string scannedBarcode,
//            string confirmedSampleType,
//            int receivingTechnicianId);

//        Task<SampleActionResult> RejectSampleAsync(
//            int sampleId,
//            string reason,
//            int rejectingTechnicianId);
//    }

//    /// <summary>
//    /// Business logic for Lab Technician Subsystem function 1:
//    /// Receive Test Request & Sample.
//    /// </summary>
//    public class SampleReceivingService : ISampleReceivingService
//    {
//       // private readonly ApplicationDbContext _db;
//        private readonly IEmailService _emailService;

//        public SampleReceivingService(
//            ApplicationDbContext db,
//            IEmailService emailService)
//        {
//            _db = db;
//            _emailService = emailService;
//        }

//        public async Task<ReceiveSamplesViewModel> GetPendingSamplesAsync()
//        {
//            var today = DateTime.Today;

//            var pending = await _db.Samples
//                .Where(s => s.Status == SampleStatus.Pending)
//                .Include(s => s.TestRequest)
//                    .ThenInclude(r => r.Patient)
//                .Include(s => s.TestRequest)
//                    .ThenInclude(r => r.Doctor)
//                .Include(s => s.TestRequest)
//                    .ThenInclude(r => r.RequestedTestTypes)
//                        .ThenInclude(rt => rt.TestType)
//                .OrderBy(s => s.TestRequest.Urgency)
//                .ThenBy(s => s.TestRequest.RequestDate)
//                .Select(s => new PendingSampleViewModel
//                {
//                    SampleId = s.SampleId,
//                    RequestId = s.TestRequestId,
//                    RequestNumber = s.TestRequest.RequestNumber,

//                    PatientName =
//                        s.TestRequest.Patient.Name + " " +
//                        s.TestRequest.Patient.Surname,

//                    PatientAge =
//                        CalculateAge(s.TestRequest.Patient.DateOfBirth),

//                    ExpectedBarcode = s.BarcodeNumber,
//                    ExpectedSampleType = s.SampleType,

//                    TestTypeNames =
//                        string.Join(
//                            ", ",
//                            s.TestRequest.RequestedTestTypes
//                                .Select(rt => rt.TestType.TestName)),

//                    OrderingDoctorName =
//                        "Dr. " + s.TestRequest.Doctor.Surname,

//                    Urgency =
//                        s.TestRequest.Urgency.ToString()
//                })
//                .ToListAsync();

//            var receivedToday = await _db.Samples.CountAsync(s =>
//                s.Status == SampleStatus.Received &&
//                s.DateTimeReceived != null &&
//                s.DateTimeReceived.Value.Date == today);

//            var rejectedToday = await _db.Samples.CountAsync(s =>
//                s.Status == SampleStatus.Rejected &&
//                s.DateTimeRejected != null &&
//                s.DateTimeRejected.Value.Date == today);

//            return new ReceiveSamplesViewModel
//            {
//                PendingSamples = pending,
//                AwaitingReceiptCount = pending.Count,
//                ReceivedTodayCount = receivedToday,
//                RejectedTodayCount = rejectedToday
//            };
//        }

//        public async Task<SampleActionResult> ReceiveSampleAsync(
//            int sampleId,
//            string scannedBarcode,
//            string confirmedSampleType,
//            int receivingTechnicianId)
//        {
//            var sample = await _db.Samples
//                .Include(s => s.TestRequest)
//                    .ThenInclude(r => r.Samples)
//                .FirstOrDefaultAsync(s => s.SampleId == sampleId);

//            if (sample == null)
//            {
//                return SampleActionResult.Fail(
//                    "Sample record not found.");
//            }

//            if (sample.Status != SampleStatus.Pending)
//            {
//                return SampleActionResult.Fail(
//                    "This sample has already been received or rejected.");
//            }

//            if (sample.TestRequest.Status == RequestStatus.Cancelled)
//            {
//                return SampleActionResult.Fail(
//                    "This test request has been cancelled and cannot accept samples.");
//            }

//            // Check that the scanned barcode matches the expected barcode.
//            if (!string.Equals(
//                    sample.BarcodeNumber,
//                    scannedBarcode?.Trim(),
//                    StringComparison.OrdinalIgnoreCase))
//            {
//                return SampleActionResult.Fail(
//                    "Scanned barcode does not match the expected barcode for this request. Please re-check the sample.");
//            }

//            // Check that the confirmed sample type matches the expected type.
//            if (!string.Equals(
//                    sample.SampleType,
//                    confirmedSampleType,
//                    StringComparison.OrdinalIgnoreCase))
//            {
//                return SampleActionResult.Fail(
//                    $"Sample type mismatch - expected {sample.SampleType}, confirmed {confirmedSampleType}.");
//            }

//            // Mark the sample as received.
//            sample.Status = SampleStatus.Received;
//            sample.ReceivedByTechnicianId = receivingTechnicianId;
//            sample.DateTimeReceived = DateTime.Now;

//            // The request moves to SamplesReceived only when
//            // all samples belonging to the request have been received.
//            var allSamplesReceived = sample.TestRequest.Samples.All(s =>
//                s.SampleId == sample.SampleId ||
//                s.Status == SampleStatus.Received);

//            if (allSamplesReceived &&
//                sample.TestRequest.Status == RequestStatus.Submitted)
//            {
//                sample.TestRequest.Status = RequestStatus.SamplesReceived;
//            }

//            await _db.SaveChangesAsync();

//            return SampleActionResult.Ok(
//                sample.TestRequest.Status.ToString());
//        }

//        public async Task<SampleActionResult> RejectSampleAsync(
//            int sampleId,
//            string reason,
//            int rejectingTechnicianId)
//        {
//            var sample = await _db.Samples
//                .Include(s => s.TestRequest)
//                    .ThenInclude(r => r.Doctor)
//                .FirstOrDefaultAsync(s => s.SampleId == sampleId);

//            if (sample == null)
//            {
//                return SampleActionResult.Fail(
//                    "Sample record not found.");
//            }

//            if (sample.Status != SampleStatus.Pending)
//            {
//                return SampleActionResult.Fail(
//                    "This sample has already been received or rejected.");
//            }

//            if (string.IsNullOrWhiteSpace(reason))
//            {
//                return SampleActionResult.Fail(
//                    "A rejection reason is required.");
//            }

//            // Mark the sample as rejected.
//            sample.Status = SampleStatus.Rejected;
//            sample.RejectionReason = reason;
//            sample.RejectedByTechnicianId = rejectingTechnicianId;
//            sample.DateTimeRejected = DateTime.Now;

//            await _db.SaveChangesAsync();

//            // Notify the ordering doctor.
//            if (sample.TestRequest.Doctor != null &&
//                !string.IsNullOrWhiteSpace(sample.TestRequest.Doctor.Email))
//            {
//                await _emailService.SendAsync(
//                    to: sample.TestRequest.Doctor.Email,
//                    subject:
//                        $"Sample rejected - Request {sample.TestRequest.RequestNumber}",
//                    body:
//                        $"The sample ({sample.BarcodeNumber}, {sample.SampleType}) " +
//                        $"for request {sample.TestRequest.RequestNumber} " +
//                        $"was rejected on receipt.\n\n" +
//                        $"Reason: {reason}\n\n" +
//                        "Please arrange for a new sample to be collected and submitted.");
//            }

//            return SampleActionResult.Ok();
//        }

//        private static int CalculateAge(DateTime dateOfBirth)
//        {
//            var today = DateTime.Today;

//            var age = today.Year - dateOfBirth.Year;

//            if (dateOfBirth.Date > today.AddYears(-age))
//            {
//                age--;
//            }

//            return age;
//        }
//    }
//}
