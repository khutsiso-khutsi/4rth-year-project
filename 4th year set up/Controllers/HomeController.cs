using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Patient.Repository;
using Patient.Models;
using _4th_year_set_up.Models;
using _4th_year_set_up.Services;

namespace _4th_year_set_up.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserRepository _userRepo;
        private readonly EmailService _emailService;

        public HomeController(ILogger<HomeController> logger, UserRepository userRepo, EmailService emailService)
        {
            _logger = logger;
            _userRepo = userRepo;
            _emailService = emailService;
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
                "Admin" => RedirectToAction("Dashboard", "Admin"),
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

        // ── FORGOT PASSWORD ───────────────────────────────────────────
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var newPassword = "NMB@" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var loginLink = Url.Action("Login", "Home", null, Request.Scheme);
            var result = _userRepo.ResetPasswordByEmail(email, newPassword);

            if (result)
            {
                var body = $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f4f7fb;font-family:DM Sans,Arial,sans-serif;'>
<div style='max-width:520px;margin:40px auto;background:white;border-radius:16px;overflow:hidden;border:1px solid #dde4f0;'>
    <div style='background:#0b1f3a;padding:28px 32px;'>
        <h1 style='color:white;margin:0;font-size:20px;font-weight:600;'>NMB-HLabSys</h1>
        <p style='color:rgba(255,255,255,0.45);margin:4px 0 0;font-size:12px;text-transform:uppercase;letter-spacing:0.08em;'>Haematology Laboratory System</p>
    </div>
    <div style='padding:36px 32px;'>
        <h2 style='color:#0b1f3a;font-size:22px;margin:0 0 12px;'>Your temporary password</h2>
        <p style='color:#6b7a99;font-size:14px;line-height:1.7;margin:0 0 28px;'>
            We received a request to reset your password for your NMB-HLabSys account. 
            Here is your temporary password — please use it to log in and change your password immediately.
        </p>
        <div style='background:#f4f7fb;border:1px solid #dde4f0;border-radius:12px;padding:24px;text-align:center;margin-bottom:28px;'>
            <p style='color:#6b7a99;font-size:11px;text-transform:uppercase;letter-spacing:0.1em;margin:0 0 10px;'>Your Temporary Password</p>
            <p style='color:#0d9488;font-size:28px;font-weight:700;letter-spacing:3px;margin:0;font-family:monospace;'>{newPassword}</p>
        </div>
        <div style='text-align:center;margin-bottom:28px;'>
            <a href='{loginLink}' 
               style='display:inline-block;background:#0d9488;color:white;padding:14px 36px;border-radius:8px;text-decoration:none;font-size:14px;font-weight:600;letter-spacing:0.02em;'>
                Sign In to NMB-HLabSys
            </a>
        </div>
        <div style='background:#fef2f2;border:1px solid #fecaca;border-radius:8px;padding:14px 18px;margin-bottom:24px;'>
            <p style='color:#dc2626;font-size:13px;margin:0;'>
                ⚠️ For your security, please change your password immediately after logging in.
            </p>
        </div>
        <p style='color:#6b7a99;font-size:13px;line-height:1.6;margin:0;'>
            If you did not request a password reset, please contact your system administrator immediately at 
            <a href='mailto:youngintellect23@gmail.com' style='color:#0d9488;'>youngintellect23@gmail.com</a>.
        </p>
    </div>
    <div style='background:#f4f7fb;border-top:1px solid #dde4f0;padding:20px 32px;text-align:center;'>
        <p style='color:#6b7a99;font-size:12px;margin:0;'>
            © 2026 NMB-HLabSys · Nelson Mandela Bay Haematology Laboratory System<br/>
            This is an automated message — please do not reply to this email.
        </p>
    </div>
</div>
</body>
</html>";

                _emailService.SendEmail(email, "Password Reset — NMB-HLabSys", body);
                TempData["Info"] = "A new temporary password has been sent to your email.";
            }
            else
            {
                TempData["Error"] = "No account found with that email address.";
            }

            return RedirectToAction("ForgotPassword");
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View("~/Views/Home/ForgotPassword.cshtml");
        }

        // ── RESET PASSWORD ────────────────────────────────────────────
        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            var valid = _userRepo.ValidateResetToken(token);
            if (!valid)
            {
                TempData["Error"] = "This reset link is invalid or has expired.";
                return RedirectToAction("ForgotPassword");
            }
            ViewData["Token"] = token;
            return View("~/Views/Home/ResetPassword.cshtml");
        }

        [HttpPost]
        public IActionResult ResetPassword(string token, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                ViewData["Token"] = token;
                return View("~/Views/Home/ResetPassword.cshtml");
            }

            var result = _userRepo.ResetPassword(token, newPassword);
            if (result)
            {
                TempData["Success"] = "Password reset successfully. You can now log in.";
                return RedirectToAction("Login");
            }

            TempData["Error"] = "Failed to reset password. Please try again.";
            ViewData["Token"] = token;
            return View("~/Views/Home/ResetPassword.cshtml");
        }
    }
}