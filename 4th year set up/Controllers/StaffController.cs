using BCrypt.Net;
using LabManager.Models;
using LabManager.Repositories;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LabManager.Controllers
{
    public class StaffController : Controller
    {
        private readonly IStaffRepository _staffRepository;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;



        public StaffController(IStaffRepository staffRepository,IEmailService emailService,IConfiguration configuration)
        {
            _staffRepository = staffRepository;
            _emailService = emailService;
            _configuration = configuration;

        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var doctors = await _staffRepository.GetAllDoctor();
                var technicians = await _staffRepository.GetTechnician();
              

                var model = new StaffViewModel
                {
                    Doctors = doctors?.ToList() ?? new List<Doctor>(),
                    Technicians = technicians?.ToList() ?? new List<Technician>(),
                  
                };


                // Reuse the already-fetched testTypes for the ViewBag
                IEnumerable<TestType> test = await _staffRepository.GetTestType ();
                ViewBag.TestType = test.Select(c => new SelectListItem { Value = c.TestId.ToString(), Text = c.TestName });
                ViewBag.Email =
                    HttpContext.Session.GetString("Email")
                    ?? User.Identity?.Name
                    ?? "Manager";

                return View(model);
            }
            catch (Exception ex)
            {
                // Log the real error while preserving the existing page.
                Console.WriteLine($"Error loading staff tables: {ex}");

                TempData["Error"] = "Unable to load staff information.";

                return View(new StaffViewModel
                {
                    Doctors = new List<Doctor>(),
                    Technicians = new List<Technician>(),
                    TestTypes = new List<TestType>()
                });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctor(
            string firstName,
            string lastName,
            string hpcsaNumber,
            string email,
            string contactNumber)
        {
            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(hpcsaNumber) ||
                string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] =
                    "First name, last name, HPCSA number and email are required.";

                return RedirectToAction(nameof(Index));
            }

            string temporaryPassword =
                TemporaryPasswordGenerator.Generate();

            string passwordHash =
                BCrypt.Net.BCrypt.HashPassword(temporaryPassword);

            DateTime expiryUtc =
                DateTime.UtcNow.AddMinutes(30);

            var doctor = new Doctor
            {
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                HCPSANumber = hpcsaNumber.Trim(),
                EmailAddress = email.Trim(),
                ContactNumber = contactNumber?.Trim(),
                IsActive = true,
                Status = "Active"
            };

            try
            {
                int userId =
                    await _staffRepository.CreateDoctorAccount(
                        doctor, passwordHash, expiryUtc);

                if (userId <= 0)
                    throw new InvalidOperationException(
                        "The account could not be created.");

                await SendInvitationEmail(
                    doctor.EmailAddress,
                    doctor.FullName,
                    temporaryPassword,
                    expiryUtc,
                    "Doctor");

                TempData["Success"] =
                    "Doctor account created and invitation emailed.";
            }
            catch
            {
                // The account might already exist if email delivery failed.
                // Do not retry account creation blindly.
                TempData["Error"] =
                    "The doctor account could not be fully processed. " +
                    "Check the staff list before trying again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(
            int id,
            string firstName,
            string lastName,
            string hpcsaNumber,
            string email,
            string contactNumber)
        {
            if (id <= 0 ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(hpcsaNumber) ||
                string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Please provide valid doctor details.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var doctor = new Doctor
                {
                    Id = id,
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    HCPSANumber = hpcsaNumber.Trim(),
                    EmailAddress = email.Trim(),
                    ContactNumber = contactNumber?.Trim()
                };

                await _staffRepository.UpdateDoctor(doctor);
                TempData["Success"] = "Doctor updated successfully.";
            }
            catch
            {
                TempData["Error"] = "Unable to update doctor.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTechnician(
            string firstName,
            string lastName,
            string employeeNumber,
            string email,
            int testTypeID)
        {
            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(employeeNumber) ||
                string.IsNullOrWhiteSpace(email) ||
                testTypeID <= 0)
            {
                TempData["Error"] =
                    "Complete all required technician fields.";

                return RedirectToAction(nameof(Index));
            }

            string temporaryPassword =
                TemporaryPasswordGenerator.Generate();

            string passwordHash =
                BCrypt.Net.BCrypt.HashPassword(temporaryPassword);

            DateTime expiryUtc =
                DateTime.UtcNow.AddMinutes(30);

            var technician = new Technician
            {
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                EmployeeNumber = employeeNumber.Trim(),
                EmailAddress = email.Trim(),
                TestTypeID = testTypeID,
                IsActive = true,
                Status = "Active"
            };

            try
            {
                int userId =
                    await _staffRepository.CreateTechnicianAccount(
                        technician, passwordHash, expiryUtc);

                if (userId <= 0)
                    throw new InvalidOperationException(
                        "The account could not be created.");

                await SendInvitationEmail(
                    technician.EmailAddress,
                    technician.FullName,
                    temporaryPassword,
                    expiryUtc,
                    "Technician");

                TempData["Success"] =
                    "Technician account created and invitation emailed.";
            }
            catch
            {
                TempData["Error"] =
                    "The technician account could not be fully processed. " +
                    "Check the staff list before trying again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTechnician(
            int id,
            string firstName,
            string lastName,
            string employeeNumber,
            string email,
            int testTypeID)
        {
            if (id <= 0 ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(employeeNumber) ||
                string.IsNullOrWhiteSpace(email) ||
                testTypeID <= 0)
            {
                TempData["Error"] =
                    "Please provide valid technician details.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                var technician = new Technician
                {
                    Id = id,
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    EmployeeNumber = employeeNumber.Trim(),
                    EmailAddress = email.Trim(),
                    TestTypeID = testTypeID
                };

                await _staffRepository.UpdateTechnician(technician);
                TempData["Success"] =
                    "Technician updated successfully.";
            }
            catch
            {
                TempData["Error"] =
                    "Unable to update technician.";
            }

            return RedirectToAction(nameof(Index));
        }


        private async Task SendInvitationEmail(
            string email,
            string fullName,
            string temporaryPassword,
            DateTime expiryUtc,
            string role)
        {
            string loginUrl =
                _configuration["AppSettings:LoginUrl"]
                ?? throw new InvalidOperationException(
                    "The login URL has not been configured.");

            string safeName =
                System.Net.WebUtility.HtmlEncode(fullName);

            string safeEmail =
                System.Net.WebUtility.HtmlEncode(email);

            string safePassword =
                System.Net.WebUtility.HtmlEncode(temporaryPassword);

            string safeLoginUrl =
                System.Net.WebUtility.HtmlEncode(loginUrl);

            string subject = "Your NMB-HLabSys account";

            string safeRole = System.Net.WebUtility.HtmlEncode(role);

            string body = $"""
        <html>
        <body style="font-family: Arial, sans-serif; color: #333;">
            <h2>Welcome to NMB-HLabSys</h2>

            <p>Hello {safeName},</p>

            <p>Your staff account ({safeRole}) has been created successfully.</p>

            <p><strong>Username:</strong> {safeEmail}</p>
            <p><strong>Temporary password:</strong> {safePassword}</p>

            <p>
                <a href="{safeLoginUrl}"
                   style="background-color: #0d9488; color: white;
                          padding: 10px 16px; text-decoration: none;
                          border-radius: 5px;">
                    Sign in to NMB-HLabSys
                </a>
            </p>

            <p>
                This temporary password expires at
                {expiryUtc:yyyy-MM-dd HH:mm} UTC.
            </p>

            <p>
                Change your password after signing in.
            </p>

            <p>
                If you did not expect this account, contact your
                system administrator.
            </p>

            <p>Regards,<br/>NMB-HLabSys Administration</p>
        </body>
        </html>
        """;

            await _emailService.SendEmailAsync(
                email,
                subject,
                body);
        }

    }
}
