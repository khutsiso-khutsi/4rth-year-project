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
        private readonly IWebHostEnvironment _env;

        public HomeController(ILogger<HomeController> logger, UserRepository userRepo, EmailService emailService,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _userRepo = userRepo;
            _emailService = emailService;
            _env = env;
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
        public IActionResult Login(string Username, string Password)
        {
            var (user, passwordHash) = _userRepo.GetUserLoginData(Username.Trim());

            if (user == null || passwordHash == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View();
            }

            if (!BCrypt.Net.BCrypt.Verify(Password, passwordHash))
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View();
            }

            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetInt32("RoleID", user.RoleID);
            HttpContext.Session.SetString("RoleName", user.RoleName);
            HttpContext.Session.SetString("Username", Username.Trim());
            // Every dashboard, the activity log and ViewBag.Email read "Email"
            // from the session, so it must be set here at real login.
            HttpContext.Session.SetString("Email", user.Email);

            _userRepo.UpdateLastLogin(user.UserID);

            // Temporary password (doctor-registered patient, new staff member,
            // or forgot-password): must pick their own before going anywhere.
            if (_userRepo.MustChangePassword(user.UserID))
            {
                HttpContext.Session.SetString("MustChangePassword", "1");
                return RedirectToAction("ChangePassword");
            }

            return RedirectToDashboard(user.RoleName);
        }

        // ── FIRST-LOGIN PASSWORD CHANGE ──
        // FirstLoginPasswordFilter sends flagged users here from every page.
        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
                return RedirectToAction("Login");
            if (HttpContext.Session.GetString("MustChangePassword") != "1")
                return RedirectToDashboard(HttpContext.Session.GetString("RoleName") ?? "");

            ViewBag.Email = HttpContext.Session.GetString("Email");
            return View("~/Views/Home/ChangePassword.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string NewPassword, string ConfirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToAction("Login");

            ViewBag.Email = HttpContext.Session.GetString("Email");
            NewPassword ??= "";

            var ruleError = PasswordRuleError(NewPassword);
            if (ruleError != null)
            {
                ModelState.AddModelError("", ruleError);
                return View("~/Views/Home/ChangePassword.cshtml");
            }
            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View("~/Views/Home/ChangePassword.cshtml");
            }
            if (_userRepo.PasswordMatches(userId.Value, NewPassword))
            {
                ModelState.AddModelError("", "Choose a new password, not the temporary one you were sent.");
                return View("~/Views/Home/ChangePassword.cshtml");
            }

            _userRepo.SetOwnPassword(userId.Value, NewPassword);
            HttpContext.Session.Remove("MustChangePassword");
            _userRepo.LogActivity("Changed temporary password at first login",
                HttpContext.Session.GetString("Email") ?? "");

            TempData["Success"] = "Password changed. Welcome!";
            return RedirectToDashboard(HttpContext.Session.GetString("RoleName") ?? "");
        }

        // ── DEV SHORTCUT LOGIN (bypasses password) ──
        // Only works when running locally in Development. On the SICT server
        // (Production) it returns 404, so nobody can skip the password there.
        public IActionResult DevLogin(string role)
        {
            if (!_env.IsDevelopment())
                return NotFound();

            HttpContext.Session.SetString("RoleName", role);
            HttpContext.Session.SetString("Email", $"dev-{role.ToLower()}@test.com");

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
                "Doctor" => RedirectToAction("DoctorDashboard", "Doctor"),
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
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string Username, string Email, string Password, string ConfirmPassword,
            string FirstName, string LastName, string IDNumber,
            DateTime DateOfBirth, string CellphoneNumber,
            string AddressLine1, string? AddressLine2, string City, string PostalCode)
        {
            // Same rules as the checks on the Register page, repeated here because
            // anything in the browser can be bypassed.
            if (string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword) ||
                string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) ||
                string.IsNullOrWhiteSpace(IDNumber) || string.IsNullOrWhiteSpace(CellphoneNumber) ||
                string.IsNullOrWhiteSpace(AddressLine1) || string.IsNullOrWhiteSpace(City) ||
                string.IsNullOrWhiteSpace(PostalCode))
            {
                ModelState.AddModelError("", "Please fill in all the required fields.");
                return View();
            }

            // Spec: the e-mail address is the username.
            var email = Email.Trim().ToLowerInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$"))
            {
                ModelState.AddModelError("", "Please enter a valid e-mail address.");
                return View();
            }

            var passwordError = PasswordRuleError(Password);
            if (passwordError != null)
            {
                ModelState.AddModelError("", passwordError);
                return View();
            }

            if (Password != ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View();
            }

            var idNumber = IDNumber.Trim();
            var idError = SaIdError(idNumber, DateOfBirth);
            if (idError != null)
            {
                ModelState.AddModelError("", idError);
                return View();
            }

            // Cellphone: SA number as 10 digits starting with 0 (accepts +27 / spaces)
            var cell = new string(CellphoneNumber.Where(char.IsDigit).ToArray());
            if (cell.StartsWith("27") && cell.Length == 11) cell = "0" + cell.Substring(2);
            if (!System.Text.RegularExpressions.Regex.IsMatch(cell, @"^0[6-8]\d{8}$"))
            {
                ModelState.AddModelError("", "Please enter a 10-digit SA cellphone number, e.g. 0821234567.");
                return View();
            }

            // SA postal codes are 4 digits
            var postalCode = PostalCode.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(postalCode, @"^\d{4}$"))
            {
                ModelState.AddModelError("", "Please enter a 4-digit postal code, e.g. 6045.");
                return View();
            }

            // The database keeps the address in one column (Patients.HomeAddress),
            // so the parts are joined: "12 Cape Road, Mill Park, Gqeberha, 6045"
            var homeAddress = string.Join(", ", new[] { AddressLine1, AddressLine2, City, postalCode }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim()));

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);

            var (result, newUserId) = _userRepo.RegisterUser(
                email, email, passwordHash, 5,
                FirstName.Trim(), LastName.Trim(), idNumber,
                DateOfBirth, cell, homeAddress);

            switch (result)
            {
                case "SUCCESS":
                    SendWelcomeEmail(email, FirstName.Trim());
                    TempData["Success"] = "Your account has been created. You can now sign in with your e-mail address.";
                    return RedirectToAction("Login");

                case "USERNAME_TAKEN":
                case "EMAIL_TAKEN":
                case "DUPLICATE_EMAIL":
                    ModelState.AddModelError("", "An account with this e-mail address already exists.");
                    return View();

                case "DUPLICATE_ID":
                    ModelState.AddModelError("", "An account with this ID number already exists.");
                    return View();

                default:
                    // Patients.IDNumber is unique in the database, so a repeated ID
                    // comes back from sp_RegisterUser as a UNIQUE KEY error.
                    if (result.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) &&
                        result.Contains("Patients", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("", "An account with this ID number already exists.");
                        return View();
                    }
                    _logger.LogWarning("Registration failed for {Email}: {Result}", email, result);
                    ModelState.AddModelError("", "Registration failed. Please try again.");
                    return View();
            }
        }

        /// <summary>
        /// "Welcome, your account is ready" e-mail after self-registration.
        /// The account already exists at this point, so a failed e-mail
        /// (wrong SMTP settings, no internet) is logged but never stops
        /// the registration.
        /// </summary>
        private void SendWelcomeEmail(string email, string firstName)
        {
            try
            {
                var loginLink = Url.Action("Login", "Home", null, Request.Scheme);
                var name = System.Net.WebUtility.HtmlEncode(firstName);
                var safeEmail = System.Net.WebUtility.HtmlEncode(email);
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
        <h2 style='color:#0b1f3a;font-size:22px;margin:0 0 12px;'>Welcome, {name}!</h2>
        <p style='color:#6b7a99;font-size:14px;line-height:1.7;margin:0 0 20px;'>
            Your patient account has been created. From your patient portal you can:
        </p>
        <ul style='color:#0b1f3a;font-size:14px;line-height:1.9;margin:0 0 24px;padding-left:20px;'>
            <li>Track your test requests</li>
            <li>View and download your results once your doctor releases them</li>
            <li>Keep your conditions, allergies and medication up to date</li>
            <li>Choose which doctors can see your history</li>
        </ul>
        <div style='background:#f4f7fb;border:1px solid #dde4f0;border-radius:12px;padding:16px 20px;margin-bottom:28px;'>
            <p style='color:#6b7a99;font-size:11px;text-transform:uppercase;letter-spacing:0.1em;margin:0 0 6px;'>Your username</p>
            <p style='color:#0b1f3a;font-size:15px;font-weight:600;margin:0;'>{safeEmail}</p>
        </div>
        <div style='text-align:center;margin-bottom:28px;'>
            <a href='{loginLink}'
               style='display:inline-block;background:#0d9488;color:white;padding:14px 36px;border-radius:8px;text-decoration:none;font-size:14px;font-weight:600;'>
                Sign In to NMB-HLabSys
            </a>
        </div>
        <p style='color:#6b7a99;font-size:13px;line-height:1.6;margin:0;'>
            If you didn't create this account, please ignore this e-mail or contact the laboratory.
        </p>
    </div>
    <div style='background:#f4f7fb;border-top:1px solid #dde4f0;padding:20px 32px;text-align:center;'>
        <p style='color:#6b7a99;font-size:12px;margin:0;'>
            © {DateTime.Now.Year} NMB-HLabSys · Nelson Mandela Bay Haematology Laboratory System<br/>
            This is an automated message, please do not reply.
        </p>
    </div>
</div>
</body>
</html>";
                _emailService.SendEmail(email, "Welcome to NMB-HLabSys — your account is ready", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Welcome e-mail to {Email} could not be sent", email);
            }
        }

        /// <summary>
        /// Spec password rules: at least 8 characters, one uppercase letter,
        /// one number and one special character. Returns null when fine.
        /// </summary>
        private static string? PasswordRuleError(string password)
        {
            if (password.Length < 8) return "Password must be at least 8 characters.";
            if (!password.Any(char.IsUpper)) return "Password must contain at least one uppercase letter.";
            if (!password.Any(char.IsDigit)) return "Password must contain at least one number.";
            if (password.All(char.IsLetterOrDigit)) return "Password must contain at least one special character.";
            return null;
        }

        /// <summary>
        /// Checks a South African ID number: 13 digits, a real date of birth in
        /// the first six (YYMMDD) that matches the one entered, and a valid
        /// check digit (Luhn). Returns null when fine.
        /// </summary>
        private static string? SaIdError(string id, DateTime dateOfBirth)
        {
            if (id.Length != 13 || !id.All(char.IsDigit))
                return "SA ID number must be exactly 13 digits.";

            if (!DateTime.TryParseExact(id.Substring(0, 6), "yyMMdd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out _))
                return "The first 6 digits of the ID number must be a valid date of birth (YYMMDD).";

            if (id.Substring(0, 6) != dateOfBirth.ToString("yyMMdd"))
                return "Your ID number doesn't match your date of birth.";

            if (dateOfBirth.Date > DateTime.Today)
                return "Date of birth can't be in the future.";

            int sum = 0;
            for (int i = 0; i < id.Length; i++)
            {
                int d = id[id.Length - 1 - i] - '0';
                if (i % 2 == 1) { d *= 2; if (d > 9) d -= 9; }
                sum += d;
            }
            if (sum % 10 != 0)
                return "That isn't a valid SA ID number. Please check it.";

            return null;
        }

        // FORGOT PASSWORD
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View("~/Views/Home/ForgotPassword.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string email)
        {
            email = (email ?? "").Trim().ToLowerInvariant();
            if (email == "")
            {
                TempData["Error"] = "Please enter your e-mail address.";
                return RedirectToAction("ForgotPassword");
            }

            // Same message whether or not the account exists, so this page can't be
            // used to find out which e-mail addresses are registered.
            const string sentMessage = "If an account exists for that e-mail address, a temporary password has been sent to it. Check your inbox (and spam folder).";

            if (!_userRepo.EmailExists(email))
            {
                TempData["Info"] = sentMessage;
                return RedirectToAction("ForgotPassword");
            }

            var newPassword = GenerateTemporaryPassword();
            var loginLink = Url.Action("Login", "Home", null, Request.Scheme);
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

            // Send the e-mail FIRST and only change the password once it has gone.
            // (It used to change the password, then crash if the e-mail failed,
            // leaving the user locked out with a password nobody knew.)
            try
            {
                _emailService.SendEmail(email, "Password Reset — NMB-HLabSys", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset e-mail to {Email} could not be sent", email);
                TempData["Error"] = "We couldn't send the e-mail right now, so your password has not been changed. Please try again in a few minutes.";
                return RedirectToAction("ForgotPassword");
            }

            if (!_userRepo.ResetPasswordByEmail(email, newPassword))
            {
                TempData["Error"] = "Something went wrong resetting your password. Please try again.";
                return RedirectToAction("ForgotPassword");
            }

            TempData["Info"] = sentMessage;
            return RedirectToAction("ForgotPassword");
        }

        /// <summary>
        /// Random temporary password that meets the spec rules (8+ characters,
        /// an uppercase letter, a number and a special character).
        /// </summary>
        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%?";
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            char Pick(string set)
            {
                var b = new byte[4];
                rng.GetBytes(b);
                return set[(int)(BitConverter.ToUInt32(b, 0) % (uint)set.Length)];
            }
            var chars = new List<char> { Pick(upper), Pick(digits), Pick(special) };
            while (chars.Count < 10) chars.Add(Pick(lower + upper + digits));
            // shuffle so the required characters aren't always first
            return new string(chars.OrderBy(_ => Pick(digits + lower)).ToArray());
        }

        // RESET PASSWORD
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