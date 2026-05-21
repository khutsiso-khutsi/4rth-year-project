using Microsoft.AspNetCore.Mvc;

namespace _4th_year_set_up.Controllers
{
    public class TechnicianDashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-tech@test.com";
            return View("~/Views/TechnicianDashboard/Index.cshtml");
        }
    }
}
