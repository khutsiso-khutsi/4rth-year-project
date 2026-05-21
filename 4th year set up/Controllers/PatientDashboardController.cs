using Microsoft.AspNetCore.Mvc;
using Patient.Models;
using Patient.Repository;

namespace _4th_year_set_up.Controllers
{
    public class PatientDashboardController : Controller
    {
        private readonly UserRepository _userRepo;

        public PatientDashboardController(UserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        // helper to log stuff - easier than typing it every time
        private void Log(string activity, string entityType = "Patient", int entityId = 0)
        {
            var email = HttpContext.Session.GetString("Email") ?? "unknown";
            _userRepo.LogActivity(activity, email);
        }

        // helper i made to stop repeating myself
        private int? GetCurrentPatientId()
        {
            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            return _userRepo.GetPatientIdByUserId(userId);
        }

        private bool IsPatient()
        {
            return HttpContext.Session.GetString("RoleName") == "Patient";
        }

        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("RoleName");
            if (role == null || role != "Patient")
                return RedirectToAction("Login", "Home");

            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            var patientId = _userRepo.GetPatientIdByUserId(userId);

            // load dashboard counts if patient record exists
            if (patientId != null)
            {
                var requests = _userRepo.GetPatientTestRequests(patientId.Value);
                var history = _userRepo.GetMedicalHistory(patientId.Value);

                ViewBag.TestResultCount = requests?.Count ?? 0;
                ViewBag.ActiveConditionCount = history?.Conditions?.Count ?? 0;
                ViewBag.ActiveMedicationCount = history?.Medications?.Count ?? 0;
            }
            else
            {
                // shouldnt really happen but just in case
                ViewBag.TestResultCount = 0;
                ViewBag.ActiveConditionCount = 0;
                ViewBag.ActiveMedicationCount = 0;
            }

            ViewBag.Email = HttpContext.Session.GetString("Email");
            ViewBag.UserID = HttpContext.Session.GetInt32("UserID");
            Log("Accessed patient dashboard");
            return View();
        }

        public IActionResult MyResults()
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();

            if (patientId == null)
                return RedirectToAction("Index");

            var requests = _userRepo.GetPatientTestRequests(patientId.Value);

            // load items for each request
            foreach (var req in requests)
            {
                req.Items = _userRepo.GetTestRequestItems(req.RequestID);
            }

            ViewBag.Email = HttpContext.Session.GetString("Email");
            ViewData["Requests"] = requests;
            Log("Viewed test results", "TestRequest", patientId.Value);
            return View("~/Views/PatientDashboard/MyResults.cshtml");
        }

        public IActionResult MedicalHistory()
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null)
                return RedirectToAction("Index");

            var vm = _userRepo.GetMedicalHistory(patientId.Value);
            ViewBag.Email = HttpContext.Session.GetString("Email");

            Log("Viewed medical history", "Patient", patientId.Value);
            return View("~/Views/PatientDashboard/MedicalHistory.cshtml", vm);
        }

        [HttpPost]
        public IActionResult AddCondition(int conditionId, DateTime? diagnosedDate, string? notes)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();

            if (patientId == null)
                return RedirectToAction("Index");

            // add the condition to this patient
            _userRepo.AddPatientCondition(patientId.Value, conditionId, diagnosedDate, notes);
            Log("Added medical condition", "MedicalCondition", conditionId);

            return RedirectToAction("MedicalHistory");
        }

        [HttpPost]
        public IActionResult RemoveCondition(int patientConditionId)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            _userRepo.RemovePatientCondition(patientConditionId);
            Log("Removed medical condition", "MedicalCondition", patientConditionId);
            return RedirectToAction("MedicalHistory");
        }

        [HttpPost]
        public IActionResult AddAllergy(int allergyId, string? severity, string? notes)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToAction("Index");

            _userRepo.AddPatientAllergy(patientId.Value, allergyId, severity, notes);
            Log("Added allergy", "Allergy", allergyId);

            return RedirectToAction("MedicalHistory");
        }

        [HttpPost]
        public IActionResult RemoveAllergy(int allergyId)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();

            // TODO: maybe show an error page instead of just redirecting
            if (patientId == null) return RedirectToAction("Index");

            _userRepo.RemovePatientAllergy(patientId.Value, allergyId);
            Log("Removed allergy", "Allergy", allergyId);
            return RedirectToAction("MedicalHistory");
        }

        [HttpPost]
        public IActionResult AddMedication(int medicationId, string? dosage, string? frequency,
            DateTime? startDate, DateTime? endDate, string? notes)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null)
                return RedirectToAction("Index");

            _userRepo.AddPatientMedication(patientId.Value, medicationId, dosage,
                frequency, startDate, endDate, notes);
            Log("Added medication", "Medication", medicationId);

            return RedirectToAction("MedicalHistory");
        }

        [HttpPost]
        public IActionResult RemoveMedication(int medicationId)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToAction("Index");

            _userRepo.RemovePatientMedication(patientId.Value, medicationId);
            Log("Removed medication", "Medication", medicationId);
            return RedirectToAction("MedicalHistory");
        }

        public IActionResult Consent(int selectedDoctorId = 0)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null)
                return RedirectToAction("Index");

            var vm = _userRepo.GetConsentData(patientId.Value, selectedDoctorId);
            ViewBag.Email = HttpContext.Session.GetString("Email");

            Log("Viewed consent management");
            return View("~/Views/PatientDashboard/Consent.cshtml", vm);
        }

        [HttpPost]
        public IActionResult GrantConsent(int doctorId)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToAction("Index");

            _userRepo.GrantConsent(patientId.Value, doctorId);
            Log("Granted consent to doctor", "Doctor", doctorId);

            return RedirectToAction("Consent", new { selectedDoctorId = doctorId });
        }

        [HttpPost]
        public IActionResult RevokeConsent(int doctorId)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToAction("Index");

            _userRepo.RevokeConsent(patientId.Value, doctorId);
            Log("Revoked consent from doctor", "Doctor", doctorId);
            return RedirectToAction("Consent");
        }

        [HttpPost]
        public IActionResult GrantTestRequestConsent(int doctorId, int requestId)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToAction("Index");

            _userRepo.GrantTestRequestConsent(patientId.Value, doctorId, requestId);
            Log("Granted test request access", "TestRequest", requestId);

            return RedirectToAction("Consent", new { selectedDoctorId = doctorId });
        }

        [HttpPost]
        public IActionResult RevokeTestRequestConsent(int doctorId, int requestId)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToAction("Index");

            _userRepo.RevokeTestRequestConsent(patientId.Value, doctorId, requestId);
            Log("Revoked test request access", "TestRequest", requestId);
            return RedirectToAction("Consent", new { selectedDoctorId = doctorId });
        }

        public IActionResult Profile()
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToAction("Index");

            var vm = _userRepo.GetPatientProfile(patientId.Value);

            ViewBag.Email = HttpContext.Session.GetString("Email");

            // pass success/error messages from tempdata if any
            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            Log("Viewed profile");
            return View("~/Views/PatientDashboard/Profile.cshtml", vm);
        }

        [HttpPost]
        public IActionResult UpdateProfile(ProfileViewModel model)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToAction("Index");

            _userRepo.UpdatePatientProfile(patientId.Value, model.FirstName, model.LastName,
                    model.DateOfBirth, model.CellphoneNumber, model.HomeAddress);

            Log("Updated profile");
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public IActionResult ChangePassword(ProfileViewModel model)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;

            // check that both new passwords match first
            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["Error"] = "New passwords do not match.";
                return RedirectToAction("Profile");
            }

            var user = _userRepo.GetUserById(userId);

            // verify current password before allowing change
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.Value.PasswordHash))
            {
                TempData["Error"] = "Current password is incorrect.";
                return RedirectToAction("Profile");
            }

            string newHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            _userRepo.ChangePatientPassword(userId, newHash);

            Log("Changed password");
            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction("Profile");
        }

        public IActionResult PrintResult(int requestId)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToAction("Index");

            var requests = _userRepo.GetPatientTestRequests(patientId.Value);

            // find the specific request they want to print
            var req = requests.FirstOrDefault(r => r.RequestID == requestId);
            if (req == null)
                return NotFound();

            req.Items = _userRepo.GetTestRequestItems(req.RequestID);
            Log("Downloaded test result PDF", "TestRequest", requestId);
            return View("~/Views/PatientDashboard/PrintResult.cshtml", req);
        }
    }
}