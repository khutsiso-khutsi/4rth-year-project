using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Patient.Repository;
using Patient.Models;
using _4th_year_set_up.Models;


namespace _4th_year_set_up.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserRepository _userRepo;

        public HomeController(ILogger<HomeController> logger, UserRepository userRepo)
        {
            _logger = logger;
            _userRepo = userRepo;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("RoleName") != null)
                return RedirectToDashboard(HttpContext.Session.GetString("RoleName")!);

            return View();
        }

        [HttpPost]
        public IActionResult Login(string Username, string Password, bool RememberMe = false)
        {
            _logger.LogInformation("==> Username: '{U}' | Password: '{P}'", Username, Password);

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError("", "Username and password are required.");
                return View();
            }

            var (user, passwordHash) = _userRepo.GetUserLoginData(Username.Trim());
            _logger.LogInformation("==> User found: {F} | Hash: '{H}'", user != null, passwordHash);

            if (user == null || passwordHash == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View();
            }

            bool passwordValid = BCrypt.Net.BCrypt.Verify(Password, passwordHash);
            _logger.LogInformation("==> Password valid: {V}", passwordValid);

            if (!passwordValid)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View();
            }

            _userRepo.UpdateLastLogin(user.UserID);
            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("RoleName", user.RoleName);
            HttpContext.Session.SetInt32("RoleID", user.RoleID);
            return RedirectToDashboard(user.RoleName);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private IActionResult RedirectToDashboard(string roleName)
        {
            return roleName switch
            {
                "Patient" => RedirectToAction("Index", "PatientDashboard"),
                "Doctor" => RedirectToAction("Index", "DoctorDashboard"),
                "Lab Technician" => RedirectToAction("Index", "TechnicianDashboard"),
                "Lab Manager" => RedirectToAction("Index", "ManagerDashboard"),
                "Admin" => RedirectToAction("Index", "AdminDashboard"),
                _ => RedirectToAction("Index")
            };
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("RoleName") != null)
                return RedirectToDashboard(HttpContext.Session.GetString("RoleName")!);
            ViewBag.Roles = _userRepo.GetAllRoles();
            return View();
        }

        [HttpPost]
        public IActionResult Register(string Username, string Email, string Password, string ConfirmPassword, int RoleID)
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ModelState.AddModelError("", "All fields are required.");
                ViewBag.Roles = _userRepo.GetAllRoles();
                return View();
            }

            if (Password != ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                ViewBag.Roles = _userRepo.GetAllRoles();
                return View();
            }

            if (Password.Length < 6)
            {
                ModelState.AddModelError("", "Password must be at least 6 characters.");
                ViewBag.Roles = _userRepo.GetAllRoles();
                return View();
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);
            string result = _userRepo.RegisterUser(Username.Trim(), Email.Trim(), passwordHash, RoleID);

            switch (result)
            {
                case "SUCCESS":
                    TempData["RegisterSuccess"] = "Account created successfully. Please sign in.";
                    return RedirectToAction("Login");
                case "USERNAME_TAKEN":
                    ModelState.AddModelError("", "That username is already taken.");
                    ViewBag.Roles = _userRepo.GetAllRoles();
                    return View();
                case "EMAIL_TAKEN":
                    ModelState.AddModelError("", "That email is already registered.");
                    ViewBag.Roles = _userRepo.GetAllRoles();
                    return View();
                default:
                    ModelState.AddModelError("", "Something went wrong. Please try again.");
                    ViewBag.Roles = _userRepo.GetAllRoles();
                    return View();
            }
        }
    }
}