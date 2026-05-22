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
       
    }
}
