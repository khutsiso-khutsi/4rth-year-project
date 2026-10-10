using System.Net;
using Microsoft.AspNetCore.Mvc;
using Patient.Models;
using Patient.Repository;
using _4th_year_set_up.Services;
using Rotativa.AspNetCore;

namespace _4th_year_set_up.Controllers
{
    public class PatientDashboardController : Controller
    {
        private readonly UserRepository _userRepo;
        private readonly EmailService _email;
        private readonly ILogger<PatientDashboardController> _logger;
        private readonly IWebHostEnvironment _env;

        public PatientDashboardController(UserRepository userRepo, EmailService email,
            ILogger<PatientDashboardController> logger, IWebHostEnvironment env)
        {
            _userRepo = userRepo;
            _email = email;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Spec: when a patient grants a doctor access, the doctor must be
        /// notified by e-mail. Returns false if the e-mail couldn't be sent,
        /// so the page can say so; the access itself is saved either way.
        /// </summary>
        private bool NotifyDoctor(int patientId, int doctorId, string whatWasShared)
        {
            try
            {
                var doctor = _userRepo.GetConsentData(patientId).AllDoctors
                    .FirstOrDefault(d => d.DoctorID == doctorId);
                if (doctor == null || string.IsNullOrWhiteSpace(doctor.Email))
                    return false;

                var profile = _userRepo.GetPatientProfile(patientId);
                var patientName = WebUtility.HtmlEncode($"{profile?.FirstName} {profile?.LastName}".Trim());
                if (string.IsNullOrEmpty(patientName))
                    patientName = "A patient";

                _email.SendEmail(doctor.Email,
                    $"Patient access granted: {patientName} — NMB-HLabSys",
                    $"<p>Dear Dr {WebUtility.HtmlEncode(doctor.DoctorName)},</p>" +
                    $"<p><strong>{patientName}</strong> has given you access to {whatWasShared} on NMB-HLabSys " +
                    $"({DateTime.Now:d MMM yyyy, HH:mm}).</p>" +
                    "<p>Sign in and open <em>Patient Records</em> to view it. The patient can change or " +
                    "revoke this access at any time.</p>" +
                    "<p>NMB-HLabSys — Haematology Lab System</p>");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Consent e-mail to doctor {DoctorId} failed", doctorId);
                return false;
            }
        }

        private static string Emailed(bool sent) =>
            sent ? " The doctor has been notified by e-mail." : " (The doctor could not be e-mailed right now.)";

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

        // Spec: patients only see results once their doctor has released
        // them. Until then the request and its progress are still shown,
        // but result values, notes and abnormal flags are blanked out.
        private static void HideUnreleasedResults(TestRequest req)
        {
            if (req.RequestStatus == "Released") return;

            foreach (var item in req.Items)
            {
                item.ResultValue = null;
                item.ResultNotes = null;
                item.IsAbnormal = false;
            }
        }

        public IActionResult Index()
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            ViewBag.Email = HttpContext.Session.GetString("Email");
            ViewBag.UserID = HttpContext.Session.GetInt32("UserID");

            var patientId = GetCurrentPatientId();
            if (patientId == null)
            {
                ViewBag.NoPatientRecord = true;
                return View();
            }

            // Everything the dashboard shows comes from the same calls the
            // other patient pages use, so the numbers always match them.
            var requests = _userRepo.GetPatientTestRequests(patientId.Value)
                .OrderByDescending(r => r.RequestDate).ToList();
            foreach (var req in requests)
            {
                req.Items = _userRepo.GetTestRequestItems(req.RequestID);
                HideUnreleasedResults(req);   // unreleased values never reach the page
            }

            var history = _userRepo.GetMedicalHistory(patientId.Value);
            var consents = _userRepo.GetConsentData(patientId.Value).Consents;
            var profile = _userRepo.GetPatientProfile(patientId.Value);

            ViewData["Requests"] = requests;
            ViewData["History"] = history;
            ViewData["Consents"] = consents;
            ViewBag.FirstName = string.IsNullOrWhiteSpace(profile?.FirstName) ? null : profile!.FirstName;

            Log("Accessed patient dashboard");
            return View();
        }

        public IActionResult MyResults()
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();

            if (patientId == null)
            {
                TempData["Error"] = $"No patient record found for the signed-in user ({HttpContext.Session.GetString("Email") ?? "unknown"}).";
                return RedirectToAction("Index");
            }

            var requests = _userRepo.GetPatientTestRequests(patientId.Value);

            // load items for each request
            foreach (var req in requests)
            {
                req.Items = _userRepo.GetTestRequestItems(req.RequestID);
                HideUnreleasedResults(req);
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
        public IActionResult RemoveCondition(int conditionId)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToAction("Login", "Home");

            var patientId = _userRepo.GetPatientIdByUserId(userId.Value);
            if (patientId == null)
                return RedirectToAction("Login", "Home");

            _userRepo.RemovePatientCondition(patientId.Value, conditionId);
            Log("Removed medical condition", "MedicalCondition", conditionId);
            return RedirectToAction("MedicalHistory");
        }

        [HttpPost]
        public IActionResult AddAllergy(int allergyId, string? severity, string? notes)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null)
            {
                TempData["Error"] = $"No patient record found for the signed-in user ({HttpContext.Session.GetString("Email") ?? "unknown"}).";
                return RedirectToAction("Index");
            }

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
            if (patientId == null)
            {
                TempData["Error"] = $"No patient record found for the signed-in user ({HttpContext.Session.GetString("Email") ?? "unknown"}).";
                return RedirectToAction("Index");
            }

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
            if (patientId == null)
            {
                TempData["Error"] = $"No patient record found for the signed-in user ({HttpContext.Session.GetString("Email") ?? "unknown"}).";
                return RedirectToAction("Index");
            }

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

        /// <summary>
        /// Grants a doctor access. scope = "all" shares every medical
        /// condition; scope = "selected" shares only the ticked conditionIds.
        /// The on/off toggle in the doctor list posts here with no scope,
        /// which keeps whatever condition choice was saved before.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GrantConsent(int doctorId, string? scope, List<int>? conditionIds)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null)
            {
                TempData["Error"] = $"No patient record found for the signed-in user ({HttpContext.Session.GetString("Email") ?? "unknown"}).";
                return RedirectToAction("Index");
            }

            if (doctorId <= 0)
            {
                TempData["Error"] = "Please choose a doctor.";
                return RedirectToAction("Consent");
            }

            if (scope == "selected" && (conditionIds == null || conditionIds.Count == 0))
            {
                TempData["Error"] = "Tick at least one condition, or choose \"All conditions\".";
                return RedirectToAction("Consent", new { selectedDoctorId = doctorId });
            }

            _userRepo.GrantConsent(patientId.Value, doctorId);
            if (!string.IsNullOrEmpty(scope))
                SaveScope(patientId.Value, doctorId, scope, conditionIds);

            Log("Granted consent to doctor", "Doctor", doctorId);

            // What the doctor can now see, for the e-mail
            var what = scope == "selected"
                ? $"{conditionIds!.Count} selected medical condition{(conditionIds.Count == 1 ? "" : "s")}, " +
                  "plus their allergies, medication and any test results they share with you"
                : "their medical records and any test results they share with you";
            var sent = NotifyDoctor(patientId.Value, doctorId, what);
            TempData["Success"] = "Access granted." + Emailed(sent);
            return RedirectToAction("Consent", new { selectedDoctorId = doctorId });
        }

        /// <summary>Changes which conditions a doctor who already has access can see.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveConditionAccess(int doctorId, string scope, List<int>? conditionIds)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToAction("Index");

            if (scope == "selected" && (conditionIds == null || conditionIds.Count == 0))
            {
                TempData["Error"] = "Tick at least one condition, or choose \"All conditions\". To stop sharing completely, switch the doctor off.";
                return RedirectToAction("Consent", new { selectedDoctorId = doctorId });
            }

            SaveScope(patientId.Value, doctorId, scope, conditionIds);
            Log("Changed condition access for doctor", "Doctor", doctorId);
            TempData["Success"] = "Condition access saved.";
            return RedirectToAction("Consent", new { selectedDoctorId = doctorId });
        }

        private void SaveScope(int patientId, int doctorId, string scope, List<int>? conditionIds)
        {
            var shareAll = scope != "selected";
            // Only keep conditions that are really on this patient's record
            var mine = _userRepo.GetMedicalHistory(patientId).Conditions.Select(c => c.ConditionID).ToHashSet();
            var ids = (conditionIds ?? new List<int>()).Where(mine.Contains).ToList();
            _userRepo.SaveConditionAccess(patientId, doctorId, shareAll, ids);
        }

        /// <summary>"Share All" button for test requests.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GrantAllConsent(int doctorId)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToAction("Index");

            var toShare = _userRepo.GetConsentData(patientId.Value, doctorId).TestRequests.Where(r => !r.IsShared).ToList();
            foreach (var r in toShare)
                _userRepo.GrantTestRequestConsent(patientId.Value, doctorId, r.RequestID);

            Log("Shared all test requests with doctor", "Doctor", doctorId);
            if (toShare.Count > 0)
            {
                var sent = NotifyDoctor(patientId.Value, doctorId,
                    $"{toShare.Count} of their previous test request{(toShare.Count == 1 ? "" : "s")} " +
                    $"({WebUtility.HtmlEncode(string.Join(", ", toShare.Select(r => r.RequestNumber)))})");
                TempData["Success"] = $"Shared {toShare.Count} test request{(toShare.Count == 1 ? "" : "s")}." + Emailed(sent);
            }
            return RedirectToAction("Consent", new { selectedDoctorId = doctorId });
        }

        /// <summary>"Revoke All" button for test requests.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RevokeAllConsent(int doctorId)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToAction("Index");

            foreach (var r in _userRepo.GetConsentData(patientId.Value, doctorId).TestRequests.Where(r => r.IsShared))
                _userRepo.RevokeTestRequestConsent(patientId.Value, doctorId, r.RequestID);

            Log("Hid all test requests from doctor", "Doctor", doctorId);
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
            TempData["Success"] = "Access revoked.";
            return RedirectToAction("Consent", new { selectedDoctorId = doctorId });
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

            var number = _userRepo.GetConsentData(patientId.Value, doctorId).TestRequests
                .FirstOrDefault(r => r.RequestID == requestId)?.RequestNumber ?? $"#{requestId}";
            var sent = NotifyDoctor(patientId.Value, doctorId,
                $"their previous test request <strong>{WebUtility.HtmlEncode(number)}</strong>");
            TempData["Success"] = $"Test request {number} shared." + Emailed(sent);

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
        model.DateOfBirth, model.CellphoneNumber, model.HomeAddress, model.Email);

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

        /// <summary>
        /// Spec 6 "Patient Reports (PDF)": every released result in a date
        /// range, grouped by test category. Downloads as a real PDF through
        /// Rotativa; if wkhtmltopdf.exe isn't installed (wwwroot\Rotativa),
        /// shows the same report as a printable page instead of crashing.
        /// </summary>
        [HttpGet]
        public IActionResult Report(DateTime? from, DateTime? to)
        {
            if (!IsPatient())
                return RedirectToAction("Login", "Home");

            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToAction("Index");

            var toDate = (to ?? DateTime.Today).Date;
            var fromDate = (from ?? toDate.AddMonths(-12)).Date;
            if (fromDate > toDate)
            {
                TempData["Error"] = "The 'From' date must be on or before the 'To' date.";
                return RedirectToAction("MyResults");
            }

            // Released requests whose release (or request) date falls in the range
            var requests = _userRepo.GetPatientTestRequests(patientId.Value)
                .Where(r => r.RequestStatus == "Released")
                .Where(r =>
                {
                    var d = (r.ReleasedDate ?? r.RequestDate).Date;
                    return d >= fromDate && d <= toDate;
                })
                .OrderBy(r => r.ReleasedDate ?? r.RequestDate)
                .ToList();
            foreach (var r in requests)
                r.Items = _userRepo.GetTestRequestItems(r.RequestID);

            var model = new _4th_year_set_up.Models.PatientReportViewModel
            {
                Profile = _userRepo.GetPatientProfile(patientId.Value),
                Requests = requests,
                From = fromDate,
                To = toDate
            };
            Log($"Downloaded results report {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");

            var fileName = $"my-results-{fromDate:yyyy-MM-dd}-to-{toDate:yyyy-MM-dd}.pdf";
            if (!_4th_year_set_up.Services.PdfSupport.Ready)
            {
                model.PrintMode = true;   // browser "Save as PDF" fallback
                return View("~/Views/PatientDashboard/ReportPdf.cshtml", model);
            }

            return new ViewAsPdf("~/Views/PatientDashboard/ReportPdf.cshtml", model)
            {
                FileName = fileName,
                CustomSwitches = "--footer-center \"Page [page] of [topage]\" --footer-font-size 8 --footer-font-name Arial"
            };
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

            if (req.RequestStatus != "Released")
            {
                TempData["Error"] = "Results can be downloaded once your doctor has released them.";
                return RedirectToAction("MyResults");
            }

            req.Items = _userRepo.GetTestRequestItems(req.RequestID);
            Log("Downloaded test result PDF", "TestRequest", requestId);
            return View("~/Views/PatientDashboard/PrintResult.cshtml", req);

        }


    }

}