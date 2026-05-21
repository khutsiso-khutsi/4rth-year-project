using Microsoft.AspNetCore.Mvc;

namespace _4th_year_set_up.Controllers
{
    public class ManagerDashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-manager@test.com";
            return View("~/Views/ManagerDashboard/Index.cshtml");
        }
    }
}
