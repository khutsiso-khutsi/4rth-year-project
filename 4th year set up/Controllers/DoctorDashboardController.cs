using Microsoft.AspNetCore.Mvc;

namespace _4th_year_set_up.Controllers
{
    public class DoctorController : Controller
    {
        public IActionResult DoctorDashboard()
        {
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-doctor@test.com";
            return View("~/Views/Doctor/DoctorDashboard.cshtml");
        }

        public IActionResult PatientRecords()
        {
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-doctor@test.com";
            return View("~/Views/Doctor/PatientRecords.cshtml");
        }

        public IActionResult TestRequests()
        {
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-doctor@test.com";
            return View("~/Views/Doctor/TestRequests.cshtml");
        }

        public IActionResult ViewResults()
        {
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-doctor@test.com";
            return View("~/Views/Doctor/ViewResults.cshtml");
        }

        public IActionResult Reports()
        {
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-doctor@test.com";
            return View("~/Views/Doctor/Reports.cshtml");
        }

        public IActionResult Alerts()
        {
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-doctor@test.com";
            return View("~/Views/Doctor/Alerts.cshtml");
        }
       
    }
}