using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Doctor.Repository;
using Doctor.Models;
using _4th_year_set_up.Services;
using Patient.Models;
using Patient.Repository;
using Rotativa.AspNetCore;

namespace _4th_year_set_up.Controllers
{
    /// <summary>
    /// Single "Doctor" controller. This used to be split across two classes
    /// (DoctorDashboardController in this namespace and a second, separately
    /// namespaced DoctorController in DoctorProfileController.cs) that both
    /// mapped to the same "Doctor" route name — that caused an
    /// AmbiguousMatchException the moment both assemblies loaded, because the
    /// default route has no area to tell them apart. Everything now lives
    /// here; DoctorProfileController.cs has been removed.
    /// </summary>
    public class DoctorController : Controller
    {
        private readonly UserRepository _userRepo;
        private readonly DoctorRepository _doctorData;
        private readonly EmailService _email;

        // Same RoleID convention HomeController.Register already uses for
        // patients created through public sign-up (see HomeController.cs).
        private const int PatientRoleId = 5;

        public DoctorController(UserRepository userRepo, DoctorRepository doctorData, EmailService email)
        {
            _userRepo = userRepo;
            _doctorData = doctorData;
            _email = email;
        }

        // ---- helpers --------------------------------------------------------

        private string CurrentEmail() => HttpContext.Session.GetString("Email") ?? "dev-doctor@test.com";

        private int CurrentUserId() => HttpContext.Session.GetInt32("UserID") ?? 1;

        /// <summary>
        /// Resolves the logged-in user to a DoctorID. Falls back to DoctorID 1
        /// in dev/demo sessions (mirrors the "dev-doctor@test.com" fallback the
        /// stub actions used everywhere before this) so the pages keep working
        /// before the Doctors table is fully populated/linked in your DB.
        /// </summary>
        private (int DoctorID, string DoctorName) CurrentDoctor()
        {
            var doctor = _doctorData.GetDoctorByUserId(CurrentUserId());
            return doctor.HasValue ? (doctor.Value.DoctorID, doctor.Value.DoctorName) : (1, "Dev Doctor");
        }

        private void Log(string activity) => _userRepo.LogActivity(activity, CurrentEmail());

        private static string GenerateTemporaryPassword()
        {
            // Readable-but-random temp password; patient must change it at
            // first login (existing UI already enforces "change password at
            // first login" elsewhere in the Patient module).
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            var rng = Random.Shared;
            var pwd = new string(Enumerable.Range(0, 10).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
            return pwd + "!1";
        }

        // ---- Dashboard --------------------------------------------------------

        public IActionResult DoctorDashboard()
        {
            ViewBag.Email = CurrentEmail();
            var (doctorId, doctorName) = CurrentDoctor();
            ViewBag.DoctorName = doctorName;

            var requests = _doctorData.GetDoctorTestRequests(doctorId);
            ViewBag.TotalRequests = requests.Count;
            ViewBag.PendingRequests = requests.Count(r =>
                r.RequestStatus == TestRequestStatus.Submitted || r.RequestStatus == TestRequestStatus.SamplesReceived
                || r.RequestStatus == TestRequestStatus.InProgress);
            ViewBag.CompletedRequests = requests.Count(r => r.RequestStatus == TestRequestStatus.Completed);
            ViewBag.AlertCount = _doctorData.GetAbnormalAlerts(doctorId, DateTime.Today.AddDays(-5), DateTime.Today).Count;

            return View("~/Views/Doctor/DoctorDashboard.cshtml");
        }

        // ---- 1. Manage Patient Records ----------------------------------------

        public IActionResult PatientRecords(string? q)
        {
            ViewBag.Email = CurrentEmail();
            var model = _doctorData.SearchPatients(q);
            ViewBag.SearchTerm = q;

            // Lookup lists for the "Register New Patient" modal's condition/
            // allergy/medication checklists. GetMedicalHistory's patient-specific
            // parts are irrelevant here (patientId 0 has none), we only need
            // the AllConditions/AllAllergies/AllMedications master lists it loads.
            var lookups = _userRepo.GetMedicalHistory(0);
            ViewBag.AllConditions = lookups.AllConditions;
            ViewBag.AllAllergies = lookups.AllAllergies;
            ViewBag.AllMedications = lookups.AllMedications;

            return View("~/Views/Doctor/PatientRecords.cshtml", model);
        }

        [HttpGet]
        public IActionResult PatientDetail(int patientId)
        {
            var history = _userRepo.GetMedicalHistory(patientId);
            return Json(new
            {
                conditions = history.Conditions.Select(c => new { c.ConditionName, diagnosed = c.DiagnosedDate?.ToString("yyyy-MM-dd"), c.Notes }),
                allergies = history.Allergies.Select(a => new { a.AllergyName, a.Severity, a.Notes }),
                medications = history.Medications.Select(m => new { m.MedicationName, m.Dosage, m.Frequency })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterPatient(NewPatientViewModel model)
        {
            ViewBag.Email = CurrentEmail();

            if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName) ||
                string.IsNullOrWhiteSpace(model.IDNumber) || string.IsNullOrWhiteSpace(model.CellphoneNumber) ||
                string.IsNullOrWhiteSpace(model.Email))
            {
                TempData["Error"] = "Name, surname, SA ID number, cellphone number and e-mail are all required.";
                return RedirectToAction("PatientRecords");
            }

            if (model.IDNumber.Length != 13 || !model.IDNumber.All(char.IsDigit))
            {
                TempData["Error"] = "SA ID number must be exactly 13 digits.";
                return RedirectToAction("PatientRecords");
            }

            if (_doctorData.PatientIdNumberExists(model.IDNumber))
            {
                TempData["Error"] = "A patient with this SA ID number is already registered.";
                return RedirectToAction("PatientRecords");
            }

            var tempPassword = GenerateTemporaryPassword();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            // Username follows the existing convention of being distinct from Email;
            // we default it to the email's local part since the doctor doesn't pick one.
            var username = model.Email.Split('@')[0] + Random.Shared.Next(100, 999);

            // sp_RegisterPatientByDoctor (not the generic sp_RegisterUser) — this
            // is the proc that sets MustChangePassword = 1, matching the spec's
            // "must change password at first login" requirement.
            var (result, newUserId, newPatientId) = _doctorData.RegisterPatientByDoctor(
                username, model.Email.Trim(), passwordHash,
                model.FirstName.Trim(), model.LastName.Trim(), model.IDNumber.Trim(),
                model.DateOfBirth, model.CellphoneNumber.Trim());

            if (result != "SUCCESS")
            {
                TempData["Error"] = result.Contains("email", StringComparison.OrdinalIgnoreCase)
                    ? "A patient with this e-mail address is already registered."
                    : result.Contains("ID number", StringComparison.OrdinalIgnoreCase)
                        ? "A patient with this SA ID number is already registered."
                        : "Registration failed. Please try again.";
                return RedirectToAction("PatientRecords");
            }

            if (newPatientId > 0)
            {
                foreach (var conditionId in model.ConditionIDs)
                    _userRepo.AddPatientCondition(newPatientId, conditionId, null, null);
                foreach (var allergyId in model.AllergyIDs)
                    _userRepo.AddPatientAllergy(newPatientId, allergyId, null, null);
                foreach (var medicationId in model.MedicationIDs)
                    _userRepo.AddPatientMedication(newPatientId, medicationId, null, null, null, null, null);
            }

            _email.SendEmail(model.Email, "Your Student/Patient account — NMB-HLabSys",
                $"<p>Hi {model.FirstName},</p>" +
                $"<p>A doctor has registered you on NMB-HLabSys. Your temporary password is:</p>" +
                $"<p style='font-size:1.2rem;font-weight:700'>{tempPassword}</p>" +
                $"<p>You will be asked to change this password the first time you log in.</p>");

            Log($"Registered new patient: {model.FirstName} {model.LastName} ({model.IDNumber})");
            TempData["Success"] = $"{model.FirstName} {model.LastName} was registered and emailed their temporary password.";
            return RedirectToAction("PatientRecords");
        }

        // ---- 2. Create Test Requests -------------------------------------------

        public IActionResult TestRequests()
        {
            ViewBag.Email = CurrentEmail();
            var (doctorId, _) = CurrentDoctor();

            var model = new CreateTestRequestViewModel
            {
                Patients = _doctorData.SearchPatients(null),
                TestTypes = _doctorData.GetAllTestTypes(),
                SampleTypes = _doctorData.GetAllSampleTypes()
            };
            ViewBag.History = _doctorData.GetDoctorTestRequests(doctorId);

            return View("~/Views/Doctor/TestRequests.cshtml", model);
        }

        /// <summary>
        /// AJAX endpoint backing the "system must indicate required samples to
        /// doctor" requirement: as the doctor ticks test types, the page calls
        /// this to show which sample type(s) those tests need (from the real
        /// sp_GetRequiredSamplesForTestTypes).
        /// </summary>
        [HttpGet]
        public IActionResult RequiredSamples(List<int> testTypeIds)
        {
            var samples = _doctorData.GetRequiredSamplesForTestTypes(testTypeIds ?? new());
            return Json(samples.Select(s => new { s.SampleTypeID, s.SampleTypeName }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitTestRequest(CreateTestRequestViewModel model)
        {
            ViewBag.Email = CurrentEmail();
            var (doctorId, _) = CurrentDoctor();

            if (model.PatientID <= 0 || model.TestTypeIDs == null || model.TestTypeIDs.Count == 0)
            {
                TempData["Error"] = "A test request needs a patient and at least one test type.";
                return RedirectToAction("TestRequests");
            }

            var samples = (model.Samples ?? new())
                .Where(s => !string.IsNullOrWhiteSpace(s.BarcodeNumber) && s.SampleTypeID > 0)
                .ToList();
            if (samples.Count == 0)
            {
                TempData["Error"] = "A test request needs at least one sample barcode with its sample type selected.";
                return RedirectToAction("TestRequests");
            }

            int requestId;
            string requestNumber;
            try
            {
                (requestId, requestNumber) = _doctorData.CreateTestRequest(
                    model.PatientID, doctorId, model.Urgency, model.ClinicalNotes, model.TestTypeIDs, samples);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = $"Could not submit test request: {ex.Message}";
                return RedirectToAction("TestRequests");
            }

            var patient = _doctorData.SearchPatients(null).FirstOrDefault(p => p.PatientID == model.PatientID);
            if (patient != null)
            {
                _email.SendEmail(patient.Email, $"Test request {requestNumber} submitted — NMB-HLabSys",
                    $"<p>Hi {patient.FirstName},</p>" +
                    $"<p>Your doctor has submitted a test request ({requestNumber}) on {DateTime.Now:d MMM yyyy}. " +
                    "You will be notified again once results are available.</p>");
            }

            Log($"Submitted test request {requestNumber} for patient #{model.PatientID}");
            TempData["Success"] = $"Test request {requestNumber} submitted. Samples recorded: {samples.Count}.";
            return RedirectToAction("TestRequests");
        }

        // ---- 3/4. Track status + View results ----------------------------------

        public IActionResult ViewResults()
        {
            ViewBag.Email = CurrentEmail();
            var (doctorId, _) = CurrentDoctor();
            var requests = _doctorData.GetDoctorTestRequests(doctorId)
                .Where(r => r.RequestStatus == TestRequestStatus.Completed
                         || r.RequestStatus == TestRequestStatus.ReleasedByDoctor)
                .Select(r => _doctorData.GetTestRequestDetail(doctorId, r.RequestID))
                .Where(r => r != null)
                .Select(r => r!)
                .ToList();
            return View("~/Views/Doctor/ViewResults.cshtml", requests);
        }

        [HttpGet]
        public IActionResult RequestDetail(int requestId)
        {
            var (doctorId, _) = CurrentDoctor();
            var detail = _doctorData.GetTestRequestDetail(doctorId, requestId);
            if (detail == null) return NotFound();
            return PartialView("~/Views/Doctor/_RequestDetail.cshtml", detail);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReleaseResults(ReleaseResultsViewModel model)
        {
            var (doctorId, _) = CurrentDoctor();
            var detail = _doctorData.GetTestRequestDetail(doctorId, model.RequestID);
            if (detail == null) return NotFound();

            // sp_ReleaseTestResults itself requires every item to be
            // 'Verified' first — check that here so the doctor gets a clear
            // message instead of a raw SQL error.
            if (detail.Items.Any(i => i.ItemStatus != TestItemStatus.Verified))
            {
                TempData["Error"] = "All test items must be verified by the lab before results can be released.";
                return RedirectToAction("ViewResults");
            }

            var (success, message) = _doctorData.ReleaseResults(model.RequestID, doctorId, model.ReleaseNotes);
            if (!success)
            {
                TempData["Error"] = $"Could not release results: {message}";
                return RedirectToAction("ViewResults");
            }

            var subject = model.AskPatientToBookAppointment
                ? $"Please book an appointment — results for {detail.RequestNumber}"
                : $"Your results are ready — {detail.RequestNumber}";
            var body = model.AskPatientToBookAppointment
                ? $"<p>Hi,</p><p>Your doctor has reviewed your results for request {detail.RequestNumber} " +
                  "and would like to discuss them with you. Please book an appointment.</p>" +
                  $"<p>Note from your doctor: {model.ReleaseNotes}</p>"
                : $"<p>Hi,</p><p>Your results for request {detail.RequestNumber} have been released by your doctor.</p>" +
                  $"<p>Note from your doctor: {model.ReleaseNotes}</p>";

            _email.SendEmail(detail.PatientEmail, subject, body);
            Log($"Released results for request {detail.RequestNumber}");
            TempData["Success"] = $"Results for {detail.RequestNumber} released to patient.";
            return RedirectToAction("ViewResults");
        }

        [HttpGet]
        public IActionResult ResultsPdf(int requestId)
        {
            var (doctorId, _) = CurrentDoctor();
            var detail = _doctorData.GetTestRequestDetail(doctorId, requestId);
            if (detail == null) return NotFound();
            // TODO: requires the Rotativa wkhtmltopdf binary to be present locally
            // (already referenced via Rotativa.AspNetCore in the .csproj — see
            // ManagerDashboardController.cs for how it's used elsewhere in this
            // solution). Renders Views/Doctor/ResultsPdf.cshtml to PDF.
            return new ViewAsPdf("~/Views/Doctor/ResultsPdf.cshtml", detail)
            {
                FileName = $"{detail.RequestNumber}-results.pdf"
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelTestRequest(CancelTestRequestViewModel model)
        {
            var (doctorId, _) = CurrentDoctor();
            var detail = _doctorData.GetTestRequestDetail(doctorId, model.RequestID);
            if (detail == null) return NotFound();

            if (!detail.CanCancel)
            {
                TempData["Error"] = "This request can no longer be cancelled — it has already progressed past sample collection.";
                return RedirectToAction("TestRequests");
            }

            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                TempData["Error"] = "A cancellation reason is required.";
                return RedirectToAction("TestRequests");
            }

            var (success, message, _, _) = _doctorData.CancelTestRequest(
                model.RequestID, model.Reason, CurrentUserId(), "Doctor");

            if (!success)
            {
                // See the remarks on DoctorDataAccess.CancelTestRequest: the
                // real sp_CancelTestRequest has a known bug (it tries to set
                // TestRequestItems.ItemStatus = 'Cancelled', a value that
                // column's own CHECK constraint disallows), so a genuine
                // cancel attempt can currently fail here with a SQL
                // constraint-violation message. That needs a fix in the
                // shared database — surfacing the real message rather than
                // hiding it is intentional, so it gets reported instead of
                // silently swallowed.
                TempData["Error"] = $"Could not cancel request: {message}";
                return RedirectToAction("TestRequests");
            }

            Log($"Cancelled test request {detail.RequestNumber}: {model.Reason}");
            TempData["Success"] = $"Request {detail.RequestNumber} cancelled.";
            return RedirectToAction("TestRequests");
        }

        // ---- 5. View Alerts -----------------------------------------------------

        public IActionResult Alerts(DateTime? from, DateTime? to)
        {
            ViewBag.Email = CurrentEmail();
            var (doctorId, _) = CurrentDoctor();

            var fromDate = from ?? DateTime.Today.AddDays(-5);
            var toDate = to ?? DateTime.Today;

            var model = new AlertsPageViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Alerts = _doctorData.GetAbnormalAlerts(doctorId, fromDate, toDate)
            };

            return View("~/Views/Doctor/Alerts.cshtml", model);
        }

        // ---- 6. Doctor Reports (PDF) ---------------------------------------------

        public IActionResult Reports(DateTime? from, DateTime? to)
        {
            ViewBag.Email = CurrentEmail();
            var (doctorId, doctorName) = CurrentDoctor();
            var fromDate = from ?? DateTime.Today.AddMonths(-1);
            var toDate = to ?? DateTime.Today;
            var model = new DoctorReportViewModel
            {
                DoctorName = doctorName,
                FromDate = fromDate,
                ToDate = toDate,
                Requests = _doctorData.GetDoctorTestRequests(doctorId, fromDate, toDate)
            };
            return View("~/Views/Doctor/Reports.cshtml", model);
        }

        [HttpGet]
        public IActionResult ReportPreview(DateTime from, DateTime to)
        {
            var (doctorId, _) = CurrentDoctor();
            var requests = _doctorData.GetDoctorTestRequests(doctorId, from, to);
            return PartialView("~/Views/Doctor/_ReportPreview.cshtml", requests);
        }

        [HttpGet]
        public IActionResult ReportPdf(DateTime from, DateTime to)
        {
            var (doctorId, doctorName) = CurrentDoctor();
            var model = new DoctorReportViewModel
            {
                DoctorName = doctorName,
                FromDate = from,
                ToDate = to,
                Requests = _doctorData.GetDoctorTestRequests(doctorId, from, to)
            };
            // TODO: requires the Rotativa wkhtmltopdf binary locally (see note on
            // ResultsPdf above). Renders Views/Doctor/ReportPdf.cshtml to PDF.
            return new ViewAsPdf("~/Views/Doctor/ReportPdf.cshtml", model)
            {
                FileName = $"doctor-report-{from:yyyy-MM-dd}-to-{to:yyyy-MM-dd}.pdf"
            };
        }

        // ---- Profile (merged from the old DoctorProfileController) ---------------

        public IActionResult Profile()
        {
            var vm = new DoctorProfileViewModel
            {
                DoctorID = 1,
                FirstName = "Dev",
                LastName = "Doctor",
                Email = CurrentEmail(),
                LicenseNumber = "MP-2024-00123",
                DateOfBirth = new DateTime(1982, 4, 10),
                CellphoneNumber = "0831234567",
                HomeAddress = "45 Settler's Way, Port Elizabeth",
                Specialization = "Haematology",
                Department = "Haematology",
                PracticeAddress = "Greenacres Hospital, Port Elizabeth",
                RegistrationDate = DateTime.Now.AddYears(-3)
            };

            ViewBag.Email = vm.Email;
            return View(vm);
        }

        public IActionResult ShowProfile()
        {
            return View("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProfile(DoctorProfileViewModel model)
        {
            model.DoctorID = 1;
            model.Email = CurrentEmail();
            model.LicenseNumber = "MP-2024-00123";
            model.RegistrationDate = DateTime.Now.AddYears(-3);
            ViewBag.Email = model.Email;

            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Please fix the errors and try again.";
                return View("Profile", model);
            }

            // TODO: persist changes to database here — no sp_UpdateDoctorProfile
            // exists yet; add one following the sp_UpdatePatientProfile pattern
            // in Patient/DataAccess/UserDataAccess.cs once the Doctors table's
            // exact editable columns are confirmed.
            ViewBag.Success = "Your profile has been updated successfully.";
            return View("Profile", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            var vm = new DoctorProfileViewModel
            {
                DoctorID = 1,
                FirstName = "Dev",
                LastName = "Doctor",
                Email = CurrentEmail(),
                LicenseNumber = "MP-2024-00123",
                DateOfBirth = new DateTime(1982, 4, 10),
                CellphoneNumber = "0831234567",
                HomeAddress = "45 Settler's Way, Port Elizabeth",
                Specialization = "Haematology",
                Department = "Haematology",
                PracticeAddress = "Greenacres Hospital, Port Elizabeth",
                RegistrationDate = DateTime.Now.AddYears(-3)
            };

            ViewBag.Email = vm.Email;

            if (string.IsNullOrWhiteSpace(CurrentPassword))
            {
                ViewBag.Error = "Please enter your current password.";
                return View("Profile", vm);
            }
            if (NewPassword != ConfirmPassword)
            {
                ViewBag.Error = "New password and confirmation do not match.";
                return View("Profile", vm);
            }
            if (NewPassword.Length < 8)
            {
                ViewBag.Error = "New password must be at least 8 characters.";
                return View("Profile", vm);
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(NewPassword, "[A-Z]"))
            {
                ViewBag.Error = "New password must contain at least one uppercase letter.";
                return View("Profile", vm);
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(NewPassword, "[0-9]"))
            {
                ViewBag.Error = "New password must contain at least one number.";
                return View("Profile", vm);
            }
            if (NewPassword == CurrentPassword)
            {
                ViewBag.Error = "New password cannot be the same as your current password.";
                return View("Profile", vm);
            }

            // TODO: verify CurrentPassword hash and save NewPassword hash here —
            // needs a sp_ChangeDoctorPassword proc analogous to
            // sp_ChangePatientPassword once Doctors/Users linkage is finalised.
            ViewBag.Success = "Password changed successfully.";
            return View("Profile", vm);
        }
    }
}
