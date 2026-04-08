using Microsoft.AspNetCore.Mvc;

namespace _4th_year_set_up.Controllers
{
    public class PatientDashboardController : Controller
    {
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
    }
}