using _4th_year_set_up.Services;
using BCrypt.Net;
using Doctor.Models;
using Doctor.Repository;
using Microsoft.AspNetCore.Mvc;

namespace _4th_year_set_up.Controllers
{
    public class DoctorController : Controller
    {
        private readonly DoctorRepository _doctorRepo;
        private readonly EmailService _emailService;

        public DoctorController(DoctorRepository doctorRepo, EmailService emailService)
        {
            _doctorRepo = doctorRepo;
            _emailService = emailService;
        }

        private int CurrentDoctorId => HttpContext.Session.GetInt32("DoctorID") ?? 0;

        // ---------- Feature 1: Manage Patient Records ----------

        [HttpGet]
        public IActionResult RegisterPatient()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RegisterPatient(PatientRecordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string generatedPassword = GenerateRandomPassword();
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(generatedPassword);

            var (result, newUserId, newPatientId) = _doctorRepo.RegisterPatientByDoctor(
                username: model.Email,
                email: model.Email,
                passwordHash: hashedPassword,
                firstName: model.Name,
                lastName: model.Surname,
                idNumber: model.IDNumber,
                dateOfBirth: model.DateOfBirth,
                cellphone: model.CellphoneNumber
            );

            if (result != "SUCCESS")
            {
                TempData["Error"] = result;
                return View(model);
            }

            _emailService.SendEmail(
                model.Email,
                "Your NMB-HLabSys Account",
                $"Hello {model.Name},\n\nAn account has been created for you.\n" +
                $"Username: {model.Email}\nTemporary Password: {generatedPassword}\n\n" +
                "Please log in and change your password immediately."
            );

            TempData["Success"] = "Patient registered successfully.";
            return RedirectToAction("PatientDetails", new { patientId = newPatientId });
        }

        [HttpGet]
        public IActionResult PatientDetails(int patientId)
        {
            // Reuses existing medical history lookups via DoctorRepository if extended,
            // or shares Patient module's UserRepository for conditions/allergies/medications
            return View(patientId);
        }

        // ---------- Feature 2: Create Test Requests ----------

        [HttpGet]
        public IActionResult CreateTestRequest()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GetRequiredSamples([FromBody] List<int> testTypeIds)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(testTypeIds);
            var samples = _doctorRepo.GetRequiredSamplesForTestTypes(json);
            return Json(samples);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTestRequest(TestRequestViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var testTypesJson = System.Text.Json.JsonSerializer.Serialize(model.SelectedTestTypeIDs);
            var barcodesJson = System.Text.Json.JsonSerializer.Serialize(
                model.Barcodes.Select(b => new { BarcodeNumber = b, SampleTypeID = 0 }) // adjust once barcode/sampletype pairing is modeled
            );

            var (result, newRequestId) = _doctorRepo.CreateTestRequest(
                model.PatientID, CurrentDoctorId, model.Urgency, model.ClinicalNotes,
                testTypesJson, barcodesJson);

            if (result != "SUCCESS")
            {
                TempData["Error"] = result;
                return View(model);
            }

            // Feature 2: notify patient via email that a test request was submitted
            // (requires patient email lookup — add to DoctorRepository if not already available)

            TempData["Success"] = $"Test request #{newRequestId} submitted.";
            return RedirectToAction("TrackRequests");
        }

        // ---------- Feature 3: Track Test Request Status ----------

        [HttpGet]
        public IActionResult TrackRequests(int patientId)
        {
            var requests = _doctorRepo.GetPatientTestRequests(patientId);
            return View(requests);
        }

        [HttpPost]
        public IActionResult CancelRequest(int requestId, string reason)
        {
            var (result, notifyEmail, notifyName) = _doctorRepo.CancelTestRequest(
                requestId, reason, CurrentDoctorId, "Doctor");

            if (result != "SUCCESS")
                TempData["Error"] = result;
            else
                TempData["Success"] = "Test request cancelled.";

            return RedirectToAction("TrackRequests");
        }

        // ---------- Feature 4: View Results ----------

        [HttpGet]
        public IActionResult ViewResults(int requestId)
        {
            var results = _doctorRepo.GetTestResultsForDoctor(requestId, CurrentDoctorId);
            return View(results);
        }

        [HttpPost]
        public async Task<IActionResult> ReleaseResults(int requestId, string? note)
        {
            var result = _doctorRepo.ReleaseTestResults(requestId, CurrentDoctorId, note);

            if (result != "SUCCESS")
            {
                TempData["Error"] = result;
                return RedirectToAction("ViewResults", new { requestId });
            }

            // Feature 4: email patient with results/note
            // (requires patient email + PDF attachment — wire up Rotativa here)

            TempData["Success"] = "Results released to patient.";
            return RedirectToAction("ViewResults", new { requestId });
        }

        // ---------- Feature 5: View Alerts ----------

        [HttpGet]
        public IActionResult Alerts(DateTime? fromDate)
        {
            var alerts = _doctorRepo.GetDoctorAlerts(CurrentDoctorId, fromDate);
            return View(alerts);
        }

        // ---------- Feature 6: Doctor Reports (PDF) ----------

        [HttpGet]
        public IActionResult Reports()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GenerateReport(DateTime startDate, DateTime endDate)
        {
            var data = _doctorRepo.GetDoctorTestRequestsByDateRange(CurrentDoctorId, startDate, endDate);
            // TODO: wire up Rotativa to render this as PDF
            return View("ReportResults", data);
        }

        // ---------- Feature 7: Doctor Dashboard ----------

        [HttpGet]
        public IActionResult DoctorDashboard()
        {
            int doctorId = CurrentDoctorId;

            // Populate ViewBag with dashboard statistics for the view
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "doctor@test.com";
            ViewBag.TestResultCount = 0;      // TODO: wire up _doctorRepo.GetDoctorTestResultCount(doctorId) if method exists
            ViewBag.AppointmentCount = 0;     // TODO: wire up _doctorRepo.GetDoctorAppointmentCount(doctorId) if method exists
            ViewBag.ActiveConditionCount = 0; // TODO: wire up _doctorRepo.GetDoctorActiveConditionCount(doctorId) if method exists
            ViewBag.PatientCount = 0;         // TODO: wire up _doctorRepo.GetDoctorPatientCount(doctorId) if method exists

            return View();
        }

        // ---------- Helpers ----------

        private static string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
