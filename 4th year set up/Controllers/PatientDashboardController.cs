using Microsoft.AspNetCore.Mvc;
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

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("RoleName") == null)
                return RedirectToAction("Login", "Home");
            if (HttpContext.Session.GetString("RoleName") != "Patient")
                return RedirectToAction("Login", "Home");
            ViewBag.Email = HttpContext.Session.GetString("Email");
            ViewBag.UserID = HttpContext.Session.GetInt32("UserID");
            return View();
        }

        public IActionResult MyResults()
        {
            if (HttpContext.Session.GetString("RoleName") != "Patient")
                return RedirectToAction("Login", "Home");
            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            var patientId = _userRepo.GetPatientIdByUserId(userId);
            if (patientId == null)
                return RedirectToAction("Index");
            var requests = _userRepo.GetPatientTestRequests(patientId.Value);
            foreach (var req in requests)
                req.Items = _userRepo.GetTestRequestItems(req.RequestID);
            ViewBag.Email = HttpContext.Session.GetString("Email");
            ViewData["Requests"] = requests;
            return View("~/Views/PatientDashboard/MyResults.cshtml");
        }

        //public IActionResult MedicalHistory()
        //{
        //    if (HttpContext.Session.GetString("RoleName") != "Patient")
        //        return RedirectToAction("Login", "Home");
        //    ViewBag.Email = HttpContext.Session.GetString("Email");
        //    return View("~/Views/PatientDashboard/MedicalHistory.cshtml");
        //}

        public IActionResult Consent()
        {
            if (HttpContext.Session.GetString("RoleName") != "Patient")
                return RedirectToAction("Login", "Home");
            ViewBag.Email = HttpContext.Session.GetString("Email");
            return View("~/Views/PatientDashboard/Consent.cshtml");
        }

        public IActionResult Profile()
        {
            if (HttpContext.Session.GetString("RoleName") != "Patient")
                return RedirectToAction("Login", "Home");
            ViewBag.Email = HttpContext.Session.GetString("Email");
            return View("~/Views/PatientDashboard/Profile.cshtml");
        }
        public IActionResult MedicalHistory()
        {
            if (HttpContext.Session.GetString("RoleName") != "Patient")
                return RedirectToAction("Login", "Home");

            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            var patientId = _userRepo.GetPatientIdByUserId(userId);
            if (patientId == null) return RedirectToAction("Index");

            var vm = _userRepo.GetMedicalHistory(patientId.Value);
            ViewBag.Email = HttpContext.Session.GetString("Email");
            return View("~/Views/PatientDashboard/MedicalHistory.cshtml", vm);
        }

        [HttpPost]
        public IActionResult AddCondition(int conditionId, DateTime? diagnosedDate, string? notes)
        {
            if (HttpContext.Session.GetString("RoleName") != "Patient")
                return RedirectToAction("Login", "Home");
            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            var patientId = _userRepo.GetPatientIdByUserId(userId);
            if (patientId != null)
                _userRepo.AddPatientCondition(patientId.Value, conditionId, diagnosedDate, notes);
            return RedirectToAction("MedicalHistory");
        }

        [HttpPost]
        public IActionResult RemoveCondition(int patientConditionId)
        {
            if (HttpContext.Session.GetString("RoleName") != "Patient")
                return RedirectToAction("Login", "Home");
            _userRepo.RemovePatientCondition(patientConditionId);
            return RedirectToAction("MedicalHistory");
        }

        [HttpPost]
        public IActionResult AddAllergy(int allergyId, string? severity, string? notes)
        {
            if (HttpContext.Session.GetString("RoleName") != "Patient")
                return RedirectToAction("Login", "Home");
            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            var patientId = _userRepo.GetPatientIdByUserId(userId);
            if (patientId != null)
                _userRepo.AddPatientAllergy(patientId.Value, allergyId, severity, notes);
            return RedirectToAction("MedicalHistory");
        }

        [HttpPost]
        public IActionResult RemoveAllergy(int allergyId)
        {
            if (HttpContext.Session.GetString("RoleName") != "Patient")
                return RedirectToAction("Login", "Home");
            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            var patientId = _userRepo.GetPatientIdByUserId(userId);
            if (patientId != null)
                _userRepo.RemovePatientAllergy(patientId.Value, allergyId);
            return RedirectToAction("MedicalHistory");
        }

        [HttpPost]
        public IActionResult AddMedication(int medicationId, string? dosage, string? frequency, DateTime? startDate, DateTime? endDate, string? notes)
        {
            if (HttpContext.Session.GetString("RoleName") != "Patient")
                return RedirectToAction("Login", "Home");
            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            var patientId = _userRepo.GetPatientIdByUserId(userId);
            if (patientId != null)
                _userRepo.AddPatientMedication(patientId.Value, medicationId, dosage, frequency, startDate, endDate, notes);
            return RedirectToAction("MedicalHistory");
        }

        [HttpPost]
        public IActionResult RemoveMedication(int medicationId)
        {
            if (HttpContext.Session.GetString("RoleName") != "Patient")
                return RedirectToAction("Login", "Home");
            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            var patientId = _userRepo.GetPatientIdByUserId(userId);
            if (patientId != null)
                _userRepo.RemovePatientMedication(patientId.Value, medicationId);
            return RedirectToAction("MedicalHistory");
        }
    }
}