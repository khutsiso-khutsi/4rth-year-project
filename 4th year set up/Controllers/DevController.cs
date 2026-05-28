using Microsoft.AspNetCore.Mvc;

namespace _4th_year_set_up.Controllers
{
    public class DevController : Controller
    {
        public IActionResult Login(string role)
        {
            HttpContext.Session.SetString("RoleName", role);
            HttpContext.Session.SetString("Email", $"dev-{role.ToLower()}@test.com");
            HttpContext.Session.SetString("Username", $"dev_{role.ToLower()}");
            HttpContext.Session.SetInt32("UserID", 1);
            HttpContext.Session.SetInt32("RoleID", 1);

            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Doctor" => RedirectToAction("DoctorDashboard", "Doctor"),
                "Lab Manager" => RedirectToAction("Index", "ManagerDashboard"),
                "Lab Technician" => RedirectToAction("Index", "TechnicianDashboard"),
                "Patient" => RedirectToAction("Index", "PatientDashboard"),
                _ => RedirectToAction("Login", "Home")
            };
        }
    }
}