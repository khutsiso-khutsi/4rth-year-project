//using Microsoft.AspNetCore.Mvc;
//using Patient.Models;
//using Patient.Repository;

//namespace _4th_year_set_up.Controllers
//{
//    public class PatientDashboardController : Controller
//    {
//        private readonly UserRepository _userRepo;

//        public PatientDashboardController(UserRepository userRepo)
//        {
//            _userRepo = userRepo;
//        }

//        // helper to log stuff - easier than typing it every time
//        private void Log(string activity, string entityType = "Patient", int entityId = 0)
//        {
//            var email = HttpContext.Session.GetString("Email") ?? "unknown";
//            _userRepo.LogActivity(activity, email);
//        }

//        // helper i made to stop repeating myself
//        private int? GetCurrentPatientId()
//        {
//            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
//            return _userRepo.GetPatientIdByUserId(userId);
//        }

//        private bool IsPatient()
//        {
//            return HttpContext.Session.GetString("RoleName") == "Patient";
//        }

//        public IActionResult Index()
//        {
//            var role = HttpContext.Session.GetString("RoleName");
//            if (role == null || role != "Patient")
//                return RedirectToAction("Login", "Home");

//            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
//            var patientId = _userRepo.GetPatientIdByUserId(userId);

//            // load dashboard counts if patient record exists
//            if (patientId != null)
//            {
//                var requests = _userRepo.GetPatientTestRequests(patientId.Value);
//                var history = _userRepo.GetMedicalHistory(patientId.Value);

//                ViewBag.TestResultCount = requests?.Count ?? 0;
//                ViewBag.ActiveConditionCount = history?.Conditions?.Count ?? 0;
//                ViewBag.ActiveMedicationCount = history?.Medications?.Count ?? 0;
//            }
//            else
//            {
//                // shouldnt really happen but just in case
//                ViewBag.TestResultCount = 0;
//                ViewBag.ActiveConditionCount = 0;
//                ViewBag.ActiveMedicationCount = 0;
//            }

//            ViewBag.Email = HttpContext.Session.GetString("Email");
//            ViewBag.UserID = HttpContext.Session.GetInt32("UserID");
//            Log("Accessed patient dashboard");
//            return View();
//        }

//        public IActionResult MyResults()
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();

//            if (patientId == null)
//                return RedirectToAction("Index");

//            var requests = _userRepo.GetPatientTestRequests(patientId.Value);

//            // load items for each request
//            foreach (var req in requests)
//            {
//                req.Items = _userRepo.GetTestRequestItems(req.RequestID);
//            }

//            ViewBag.Email = HttpContext.Session.GetString("Email");
//            ViewData["Requests"] = requests;
//            Log("Viewed test results", "TestRequest", patientId.Value);
//            return View("~/Views/PatientDashboard/MyResults.cshtml");
//        }

//        public IActionResult MedicalHistory()
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();
//            if (patientId == null)
//                return RedirectToAction("Index");

//            var vm = _userRepo.GetMedicalHistory(patientId.Value);
//            ViewBag.Email = HttpContext.Session.GetString("Email");

//            Log("Viewed medical history", "Patient", patientId.Value);
//            return View("~/Views/PatientDashboard/MedicalHistory.cshtml", vm);
//        }

//        [HttpPost]
//        public IActionResult AddCondition(int conditionId, DateTime? diagnosedDate, string? notes)
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();

//            if (patientId == null)
//                return RedirectToAction("Index");

//            // add the condition to this patient
//            _userRepo.AddPatientCondition(patientId.Value, conditionId, diagnosedDate, notes);
//            Log("Added medical condition", "MedicalCondition", conditionId);

//            return RedirectToAction("MedicalHistory");
//        }

//        [HttpPost]
//        public IActionResult RemoveCondition(int patientConditionId)
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            _userRepo.RemovePatientCondition(patientConditionId);
//            Log("Removed medical condition", "MedicalCondition", patientConditionId);
//            return RedirectToAction("MedicalHistory");
//        }

//        [HttpPost]
//        public IActionResult AddAllergy(int allergyId, string? severity, string? notes)
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();
//            if (patientId == null) return RedirectToAction("Index");

//            _userRepo.AddPatientAllergy(patientId.Value, allergyId, severity, notes);
//            Log("Added allergy", "Allergy", allergyId);

//            return RedirectToAction("MedicalHistory");
//        }

//        [HttpPost]
//        public IActionResult RemoveAllergy(int allergyId)
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();

//            // TODO: maybe show an error page instead of just redirecting
//            if (patientId == null) return RedirectToAction("Index");

//            _userRepo.RemovePatientAllergy(patientId.Value, allergyId);
//            Log("Removed allergy", "Allergy", allergyId);
//            return RedirectToAction("MedicalHistory");
//        }

//        [HttpPost]
//        public IActionResult AddMedication(int medicationId, string? dosage, string? frequency,
//            DateTime? startDate, DateTime? endDate, string? notes)
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();
//            if (patientId == null)
//                return RedirectToAction("Index");

//            _userRepo.AddPatientMedication(patientId.Value, medicationId, dosage,
//                frequency, startDate, endDate, notes);
//            Log("Added medication", "Medication", medicationId);

//            return RedirectToAction("MedicalHistory");
//        }

//        [HttpPost]
//        public IActionResult RemoveMedication(int medicationId)
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();
//            if (patientId == null) return RedirectToAction("Index");

//            _userRepo.RemovePatientMedication(patientId.Value, medicationId);
//            Log("Removed medication", "Medication", medicationId);
//            return RedirectToAction("MedicalHistory");
//        }

//        public IActionResult Consent(int selectedDoctorId = 0)
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();
//            if (patientId == null)
//                return RedirectToAction("Index");

//            var vm = _userRepo.GetConsentData(patientId.Value, selectedDoctorId);
//            ViewBag.Email = HttpContext.Session.GetString("Email");

//            Log("Viewed consent management");
//            return View("~/Views/PatientDashboard/Consent.cshtml", vm);
//        }

//        [HttpPost]
//        public IActionResult GrantConsent(int doctorId)
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();
//            if (patientId == null) return RedirectToAction("Index");

//            _userRepo.GrantConsent(patientId.Value, doctorId);
//            Log("Granted consent to doctor", "Doctor", doctorId);

//            return RedirectToAction("Consent", new { selectedDoctorId = doctorId });
//        }

//        [HttpPost]
//        public IActionResult RevokeConsent(int doctorId)
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();
//            if (patientId == null) return RedirectToAction("Index");

//            _userRepo.RevokeConsent(patientId.Value, doctorId);
//            Log("Revoked consent from doctor", "Doctor", doctorId);
//            return RedirectToAction("Consent");
//        }

//        [HttpPost]
//        public IActionResult GrantTestRequestConsent(int doctorId, int requestId)
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();
//            if (patientId == null) return RedirectToAction("Index");

//            _userRepo.GrantTestRequestConsent(patientId.Value, doctorId, requestId);
//            Log("Granted test request access", "TestRequest", requestId);

//            return RedirectToAction("Consent", new { selectedDoctorId = doctorId });
//        }

//        [HttpPost]
//        public IActionResult RevokeTestRequestConsent(int doctorId, int requestId)
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();
//            if (patientId == null) return RedirectToAction("Index");

//            _userRepo.RevokeTestRequestConsent(patientId.Value, doctorId, requestId);
//            Log("Revoked test request access", "TestRequest", requestId);
//            return RedirectToAction("Consent", new { selectedDoctorId = doctorId });
//        }

//        public IActionResult Profile()
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();
//            if (patientId == null) return RedirectToAction("Index");

//            var vm = _userRepo.GetPatientProfile(patientId.Value);

//            ViewBag.Email = HttpContext.Session.GetString("Email");

//            // pass success/error messages from tempdata if any
//            ViewBag.Success = TempData["Success"];
//            ViewBag.Error = TempData["Error"];

//            Log("Viewed profile");
//            return View("~/Views/PatientDashboard/Profile.cshtml", vm);
//        }

//        [HttpPost]
//        public IActionResult UpdateProfile(ProfileViewModel model)
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();
//            if (patientId == null) return RedirectToAction("Index");

//            _userRepo.UpdatePatientProfile(patientId.Value, model.FirstName, model.LastName,
//                    model.DateOfBirth, model.CellphoneNumber, model.HomeAddress);

//            Log("Updated profile");
//            TempData["Success"] = "Profile updated successfully.";
//            return RedirectToAction("Profile");
//        }

//        [HttpPost]
//        public IActionResult ChangePassword(ProfileViewModel model)
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;

//            // check that both new passwords match first
//            if (model.NewPassword != model.ConfirmPassword)
//            {
//                TempData["Error"] = "New passwords do not match.";
//                return RedirectToAction("Profile");
//            }

//            var user = _userRepo.GetUserById(userId);

//            // verify current password before allowing change
//            if (user == null || !BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.Value.PasswordHash))
//            {
//                TempData["Error"] = "Current password is incorrect.";
//                return RedirectToAction("Profile");
//            }

//            string newHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
//            _userRepo.ChangePatientPassword(userId, newHash);

//            Log("Changed password");
//            TempData["Success"] = "Password changed successfully.";
//            return RedirectToAction("Profile");
//        }

//        public IActionResult PrintResult(int requestId)
//        {
//            if (!IsPatient())
//                return RedirectToAction("Login", "Home");

//            var patientId = GetCurrentPatientId();
//            if (patientId == null) return RedirectToAction("Index");

//            var requests = _userRepo.GetPatientTestRequests(patientId.Value);

//            // find the specific request they want to print
//            var req = requests.FirstOrDefault(r => r.RequestID == requestId);
//            if (req == null)
//                return NotFound();

//            req.Items = _userRepo.GetTestRequestItems(req.RequestID);
//            Log("Downloaded test result PDF", "TestRequest", requestId);
//            return View("~/Views/PatientDashboard/PrintResult.cshtml", req);

//        }


//    }

//}
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

        // DEV MODE: no DB, no session checks
        private bool IsPatient() => true;
        private int? GetCurrentPatientId() => 1;
        private void Log(string activity, string entityType = "Patient", int entityId = 0) { }

        public IActionResult Index()
        {
            ViewBag.TestResultCount = 3;
            ViewBag.ActiveConditionCount = 2;
            ViewBag.ActiveMedicationCount = 1;
            ViewBag.AppointmentCount = 1;
            ViewBag.AssignedDoctorCount = 1;
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-patient@test.com";
            ViewBag.UserID = 1;
            return View();
        }

        public IActionResult MyResults()
        {
            var requests = new List<TestRequest>
            {
                new TestRequest
                {
                    RequestID = 1,
                    RequestNumber = "REQ-001",
                    RequestDate = DateTime.Now.AddDays(-10),
                    RequestStatus = "Completed",
                    Urgency = "Routine",
                    DoctorName = "Dr. John Smith",
                    ClinicalNotes = "Routine blood work",
                    Items = new List<TestRequestItem>
                    {
                        new TestRequestItem { RequestItemID = 1, TestName = "Full Blood Count", CategoryName = "Haematology", ItemStatus = "Verified", ResultValue = 13.5m, UnitName = "g/dL", NormalRangeMin = 12.0m, NormalRangeMax = 17.5m, IsAbnormal = false },
                        new TestRequestItem { RequestItemID = 2, TestName = "Haemoglobin", CategoryName = "Haematology", ItemStatus = "Verified", ResultValue = 13.5m, UnitName = "g/dL", NormalRangeMin = 12.0m, NormalRangeMax = 17.5m, IsAbnormal = false }
                    }
                },
                new TestRequest
                {
                    RequestID = 2,
                    RequestNumber = "REQ-002",
                    RequestDate = DateTime.Now.AddDays(-3),
                    RequestStatus = "Pending",
                    Urgency = "Urgent",
                    DoctorName = "Dr. Sarah Mokoena",
                    Items = new List<TestRequestItem>
                    {
                        new TestRequestItem { RequestItemID = 3, TestName = "Platelet Count", CategoryName = "Haematology", ItemStatus = "Pending", IsAbnormal = false }
                    }
                }
            };

            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-patient@test.com";
            ViewData["Requests"] = requests;
            return View("~/Views/PatientDashboard/MyResults.cshtml");
        }

        public IActionResult MedicalHistory()
        {
            var vm = new MedicalHistoryViewModel
            {
                Conditions = new List<PatientCondition>
                {
                    new PatientCondition { PatientConditionID = 1, ConditionID = 1, ConditionName = "Anaemia", DiagnosedDate = DateTime.Now.AddYears(-2), Notes = "Mild iron deficiency" },
                    new PatientCondition { PatientConditionID = 2, ConditionID = 2, ConditionName = "Hypertension", DiagnosedDate = DateTime.Now.AddYears(-1), Notes = "Managed with medication" }
                },
                Allergies = new List<PatientAllergy>
                {
                    new PatientAllergy { AllergyID = 1, AllergyName = "Penicillin", Severity = "Severe", Notes = "Causes rash" }
                },
                Medications = new List<PatientMedication>
                {
                    new PatientMedication { MedicationID = 1, MedicationName = "Ferrous Sulphate", Dosage = "200mg", Frequency = "Once daily", StartDate = DateTime.Now.AddMonths(-6) }
                },
                AllConditions = new List<(int Id, string Name)>
                {
                    (1, "Anaemia"), (2, "Hypertension"), (3, "Diabetes")
                },
                AllAllergies = new List<(int Id, string Name)>
                {
                    (1, "Penicillin"), (2, "Aspirin"), (3, "Latex")
                },
                AllMedications = new List<(int Id, string Name)>
                {
                    (1, "Ferrous Sulphate"), (2, "Amlodipine"), (3, "Metformin")
                }
            };

            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-patient@test.com";
            return View("~/Views/PatientDashboard/MedicalHistory.cshtml", vm);
        }

        [HttpPost]
        public IActionResult AddCondition(int conditionId, DateTime? diagnosedDate, string? notes)
            => RedirectToAction("MedicalHistory");

        [HttpPost]
        public IActionResult RemoveCondition(int patientConditionId)
            => RedirectToAction("MedicalHistory");

        [HttpPost]
        public IActionResult AddAllergy(int allergyId, string? severity, string? notes)
            => RedirectToAction("MedicalHistory");

        [HttpPost]
        public IActionResult RemoveAllergy(int allergyId)
            => RedirectToAction("MedicalHistory");

        [HttpPost]
        public IActionResult AddMedication(int medicationId, string? dosage, string? frequency,
            DateTime? startDate, DateTime? endDate, string? notes)
            => RedirectToAction("MedicalHistory");

        [HttpPost]
        public IActionResult RemoveMedication(int medicationId)
            => RedirectToAction("MedicalHistory");

        public IActionResult Consent(int selectedDoctorId = 0)
        {
            var vm = new ConsentViewModel
            {
                SelectedDoctorID = selectedDoctorId,
                AllDoctors = new List<DoctorOption>
                {
                    new DoctorOption { DoctorID = 1, DoctorName = "Dr. John Smith", Email = "john.smith@nmb.com" },
                    new DoctorOption { DoctorID = 2, DoctorName = "Dr. Sarah Mokoena", Email = "sarah.mokoena@nmb.com" }
                },
                Consents = new List<DoctorConsent>
                {
                    new DoctorConsent { ConsentID = 1, DoctorID = 1, DoctorName = "Dr. John Smith", DoctorEmail = "john.smith@nmb.com", ConsentGranted = true, GrantedDate = DateTime.Now.AddMonths(-1) }
                },
                TestRequests = new List<ConsentTestRequest>
                {
                    new ConsentTestRequest { RequestID = 1, RequestNumber = "REQ-001", RequestDate = DateTime.Now.AddDays(-10), RequestStatus = "Completed", IsShared = true },
                    new ConsentTestRequest { RequestID = 2, RequestNumber = "REQ-002", RequestDate = DateTime.Now.AddDays(-3), RequestStatus = "Pending", IsShared = false }
                }
            };

            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-patient@test.com";
            return View("~/Views/PatientDashboard/Consent.cshtml", vm);
        }

        [HttpPost]
        public IActionResult GrantConsent(int doctorId)
            => RedirectToAction("Consent", new { selectedDoctorId = doctorId });

        [HttpPost]
        public IActionResult RevokeConsent(int doctorId)
            => RedirectToAction("Consent");

        [HttpPost]
        public IActionResult GrantTestRequestConsent(int doctorId, int requestId)
            => RedirectToAction("Consent", new { selectedDoctorId = doctorId });

        [HttpPost]
        public IActionResult RevokeTestRequestConsent(int doctorId, int requestId)
            => RedirectToAction("Consent", new { selectedDoctorId = doctorId });

        public IActionResult Profile()
        {
            var vm = new ProfileViewModel
            {
                PatientID = 1,
                FirstName = "Dev",
                LastName = "Patient",
                Email = "dev-patient@test.com",
                IDNumber = "9506150123456",
                DateOfBirth = new DateTime(1995, 6, 15),
                CellphoneNumber = "0821234567",
                HomeAddress = "123 Test Street, Port Elizabeth",
                RegistrationDate = DateTime.Now.AddYears(-1)
            };

            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-patient@test.com";
            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];
            return View("~/Views/PatientDashboard/Profile.cshtml", vm);
        }

        [HttpPost]
        public IActionResult UpdateProfile(ProfileViewModel model)
        {
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public IActionResult ChangePassword(ProfileViewModel model)
        {
            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction("Profile");
        }

        public IActionResult PrintResult(int requestId)
        {
            var req = new TestRequest
            {
                RequestID = requestId,
                RequestNumber = "REQ-001",
                RequestDate = DateTime.Now.AddDays(-10),
                RequestStatus = "Completed",
                Urgency = "Routine",
                DoctorName = "Dr. John Smith",
                Items = new List<TestRequestItem>
                {
                    new TestRequestItem { RequestItemID = 1, TestName = "Full Blood Count", CategoryName = "Haematology", ItemStatus = "Verified", ResultValue = 13.5m, UnitName = "g/dL", NormalRangeMin = 12.0m, NormalRangeMax = 17.5m, IsAbnormal = false },
                    new TestRequestItem { RequestItemID = 2, TestName = "Haemoglobin", CategoryName = "Haematology", ItemStatus = "Verified", ResultValue = 13.5m, UnitName = "g/dL", NormalRangeMin = 12.0m, NormalRangeMax = 17.5m, IsAbnormal = false }
                }
            };

            return View("~/Views/PatientDashboard/PrintResult.cshtml", req);
        }
    }
}