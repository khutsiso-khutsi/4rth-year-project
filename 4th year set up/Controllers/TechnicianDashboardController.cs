using _4th_year_set_up.Models;
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

        public IActionResult RegisterSample()
        {
            ViewBag.Email = "dev-tech@test.com";

            return View();
        }

        public IActionResult EnterResults()
        {
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-tech@test.com";
            return View();
        }

        public IActionResult ViewResults()
        {
            ViewBag.Email = "dev-tech@test.com";

            return View();
        }

        public IActionResult TestQueue()
        {
            ViewBag.Email = "dev-tech@test.com";

            return View();
        }

        public IActionResult Profile()
        {
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-tech@test.com";
            return View();
        }

        public IActionResult ReceiveSamples()
        {
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-tech@test.com";
            return View();
        }

        [HttpPost]
        public IActionResult ReceiveSamples(ReceiveSampleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // TODO: Save received sample

            return RedirectToAction(nameof(ReceiveSamples));
        }

        //public IActionResult Dashboard()
        //{

        //}
    }
}
