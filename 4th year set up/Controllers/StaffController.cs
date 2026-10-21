<<<<<<< Updated upstream
﻿using System.Text;
=======
using System.Text;
>>>>>>> Stashed changes
using System.Text.RegularExpressions;
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
        private const string DoctorRole = "Doctor";
        private const string TechnicianRole = "Laboratory Technician";

        // Serialises "work out next number + insert" so two managers saving at the same
        // moment cannot be given the same HPCSA / employee number (single app instance).
        // Also add a UNIQUE index on both columns (see StaffMultiTestType.sql).
        private static readonly SemaphoreSlim _numberLock = new SemaphoreSlim(1, 1);

        private readonly IStaffRepository _staffRepository;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public StaffController(IStaffRepository staffRepository, IEmailService emailService, IConfiguration configuration)
        {
            _staffRepository = staffRepository;
            _emailService = emailService;
            _configuration = configuration;
        }

        // ─────────────────────────────────────────────────────────────
        // INDEX
        // ─────────────────────────────────────────────────────────────
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

                IEnumerable<TestType> test = await _staffRepository.GetTestType();
                ViewBag.TestType = test
                    .Select(c => new SelectListItem { Value = c.TestId.ToString(), Text = c.TestName })
                    .ToList();

                // TechnicianId -> assigned TestTypeIds. If the join table has not been created yet,
                // keep the page working (the view falls back to the old single TestTypeID).
                try
                {
                    ViewBag.TechnicianTestTypes = await _staffRepository.GetTechnicianTestTypeMap();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to load technician test types: {ex}");
                    ViewBag.TechnicianTestTypes = new Dictionary<int, List<int>>();
                }

                ViewBag.Email =
                    HttpContext.Session.GetString("Email")
                    ?? User.Identity?.Name
                    ?? "Manager";

                return View(model);
            }
            catch (Exception ex)
            {
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

        // ─────────────────────────────────────────────────────────────
        // DOCTORS
        // ─────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctor(
            string firstName,
            string lastName,
            string email,
            string contactNumber)
        {
            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email))
            {
                if (string.IsNullOrWhiteSpace(firstName))
                {
                    TempData["Error"] =
                        "First name is required.";

                    return RedirectToAction(nameof(Index));
                }
                TempData["Error"] = "First name, last name and email are required.";
                return RedirectToAction(nameof(Index));
            }

            string temporaryPassword = TemporaryPasswordGenerator.Generate();
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
            DateTime expiryUtc = DateTime.UtcNow.AddMinutes(30);

                    return RedirectToAction(nameof(Index));
                }
            Doctor doctor = null;
            int userId = 0;

            await _numberLock.WaitAsync();
            try
            {
                var existing = await _staffRepository.GetAllDoctor();

                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(email))
                doctor = new Doctor
                {
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    HCPSANumber = NextNumber(existing?.Select(d => d.HCPSANumber), "HPCSA-"),
                    EmailAddress = email.Trim(),
                    ContactNumber = contactNumber?.Trim(),
                    IsActive = true,
                    Status = "Active"
                };

                userId = await _staffRepository.CreateDoctorAccount(doctor, passwordHash, expiryUtc);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating doctor: {ex}");
                TempData["Error"] = "The doctor account could not be created. Check the staff list before trying again.";
                return RedirectToAction(nameof(Index));
            }
            finally
            {
                _numberLock.Release();
            }

                return RedirectToAction(nameof(Index));
            if (userId <= 0)
            {
                TempData["Error"] = "The doctor account could not be created.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await SendInvitationEmail(
                    doctor.EmailAddress,
                    doctor.FullName,
                    temporaryPassword,
                    expiryUtc,
                    DoctorRole,
                    "HPCSA number",
                    doctor.HCPSANumber,
                    null);

                TempData["Success"] = $"Doctor account created (HPCSA number {doctor.HCPSANumber}) and invitation emailed.";
            }
            catch (Exception ex)
            {
                // The account exists; do NOT create it again.
                Console.WriteLine($"Error emailing doctor invitation: {ex}");
                TempData["Error"] =
                    $"Doctor account created (HPCSA number {doctor.HCPSANumber}) but the invitation email could not be sent.";
            }

                return RedirectToAction(nameof(Index));
            }
        }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(
            int id,
            string firstName,
            string lastName,
            string email,
            string contactNumber)
        {
            if (id <= 0 ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email))
            {
                if (id <= 0)
                {
                    TempData["Error"] =
                        "Invalid doctor.";

                    return RedirectToAction(nameof(Index));
                }
                TempData["Error"] = "Please provide valid doctor details.";
                return RedirectToAction(nameof(Index));
            }

                if (string.IsNullOrWhiteSpace(firstName))
                {
                    TempData["Error"] =
                        "First name is required.";

                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(lastName))
            try
            {
                // The HPCSA number is system-assigned, so always keep the stored one.
                var current = (await _staffRepository.GetAllDoctor())?.FirstOrDefault(d => d.Id == id);
                if (current == null)
                {
                    TempData["Error"] =
                        "Last name is required.";

                    return RedirectToAction(nameof(Index));
                    TempData["Error"] = "Doctor not found.";
                    return RedirectToAction(nameof(Index));
                }

                var doctor = new Doctor
                {
                    Id = id,
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    HCPSANumber = current.HCPSANumber,
                    EmailAddress = email.Trim(),
                    ContactNumber = contactNumber?.Trim(),
                    IsActive = current.IsActive,
                    Status = current.Status
                };

                await _staffRepository.UpdateDoctor(doctor);
                TempData["Success"] = "Doctor updated successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating doctor: {ex}");
                TempData["Error"] = "Unable to update doctor.";
            }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to update doctor: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }

                return RedirectToAction(nameof(Index));
            }
        }


        // ============================================================
        // ADD TECHNICIAN
        // ============================================================

        // ─────────────────────────────────────────────────────────────
        // TECHNICIANS
        // ─────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTechnician(
            string firstName,
            string lastName,
            string email,
            int[] testTypeIDs)
        {
            var selected = CleanIds(testTypeIDs);

                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(lastName))
                {
                    TempData["Error"] =
                        "Last name is required.";

                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(employeeNumber))
                {
                    TempData["Error"] =
                        "Employee number is required.";

                    return RedirectToAction(nameof(Index));
                }
            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) ||
                selected.Count == 0)
            {
                TempData["Error"] = "Complete all required technician fields and select at least one test type.";
                return RedirectToAction(nameof(Index));
            }

            string temporaryPassword = TemporaryPasswordGenerator.Generate();
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
            DateTime expiryUtc = DateTime.UtcNow.AddMinutes(30);

                    return RedirectToAction(nameof(Index));
                }
            Technician technician = null;
            int userId = 0;

                if (testTypeID <= 0)
                {
                    TempData["Error"] =
                        "Please select a test type.";

                    return RedirectToAction(nameof(Index));
                }

            await _numberLock.WaitAsync();
            try
            {
                var existing = await _staffRepository.GetTechnician();

                technician = new Technician
                {
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    EmployeeNumber = NextNumber(existing?.Select(t => t.EmployeeNumber), "EMP-"),
                    EmailAddress = email.Trim(),
                    TestTypeID = selected[0],      // primary test type, kept for the existing single-value column
                    IsActive = true,
                    Status = "Active"
                };

                userId = await _staffRepository.CreateTechnicianAccount(technician, passwordHash, expiryUtc);

                // All selected test types go to the join table.
                // NOTE: userId must be the technician's id (see StaffMultiTestType.sql notes).
                if (userId > 0)
                {
                    await _staffRepository.SetTechnicianTestTypes(userId, selected);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating technician: {ex}");
                TempData["Error"] = "The technician account could not be fully created. Check the staff list before trying again.";
                return RedirectToAction(nameof(Index));
            }
            finally
            {
                _numberLock.Release();
            }

            if (userId <= 0)
            {
                TempData["Error"] = "The technician account could not be created.";
                return RedirectToAction(nameof(Index));
            }

                return RedirectToAction(nameof(Index));
            try
            {
                var allTests = await _staffRepository.GetTestType();
                var testNames = allTests
                    .Where(t => selected.Contains(t.TestId))
                    .Select(t => t.TestName)
                    .ToList();

                await SendInvitationEmail(
                    technician.EmailAddress,
                    technician.FullName,
                    temporaryPassword,
                    expiryUtc,
                    TechnicianRole,
                    "Employee number",
                    technician.EmployeeNumber,
                    testNames);

                TempData["Success"] = $"Technician account created (employee number {technician.EmployeeNumber}) and invitation emailed.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error emailing technician invitation: {ex}");
                TempData["Error"] =
                    $"Technician account created (employee number {technician.EmployeeNumber}) but the invitation email could not be sent.";
            }

                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTechnician(
            int id,
            string firstName,
            string lastName,
            string email,
            int[] testTypeIDs)
        {
            var selected = CleanIds(testTypeIDs);

            if (id <= 0 ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) ||
                selected.Count == 0)
            {
                TempData["Error"] = "Please provide valid technician details and select at least one test type.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // The employee number is system-assigned, so always keep the stored one.
                var current = (await _staffRepository.GetTechnician())?.FirstOrDefault(t => t.Id == id);
                if (current == null)
                {
                    TempData["Error"] = "Technician not found.";
                    return RedirectToAction(nameof(Index));
                }

                var technician = new Technician
                {
                    Id = id,
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    EmployeeNumber = current.EmployeeNumber,
                    EmailAddress = email.Trim(),
                    TestTypeID = selected[0],
                    IsActive = current.IsActive,
                    Status = current.Status
                };

                await _staffRepository.UpdateTechnician(technician);
                await _staffRepository.SetTechnicianTestTypes(id, selected);

                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(firstName))
                {
                    TempData["Error"] =
                        "First name is required.";
                TempData["Success"] = "Technician updated successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating technician: {ex}");
                TempData["Error"] = "Unable to update technician.";
            }

                    return RedirectToAction(nameof(Index));
                }
            return RedirectToAction(nameof(Index));
        }

                if (string.IsNullOrWhiteSpace(lastName))
                {
                    TempData["Error"] =
                        "Last name is required.";

                    return RedirectToAction(nameof(Index));
                }
        // ─────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────
        private static List<int> CleanIds(IEnumerable<int> ids)
        {
            return (ids ?? Enumerable.Empty<int>()).Where(i => i > 0).Distinct().ToList();
        }

        /// <summary>
        /// Finds the highest number already used with the given prefix (e.g. EMP-0007) and returns the next one.
        /// Values that don't follow the pattern (older records) are ignored.
        /// </summary>
        private static string NextNumber(IEnumerable<string> existing, string prefix, int width = 4)
        {
            var pattern = new Regex("^" + Regex.Escape(prefix) + "(\\d+)$", RegexOptions.IgnoreCase);
            int max = 0;

                    return RedirectToAction(nameof(Index));
                }
            if (existing != null)
            {
                foreach (var value in existing)
                {
                    if (string.IsNullOrWhiteSpace(value)) continue;

                    var match = pattern.Match(value.Trim());
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int n) && n > max)
                    {
                        max = n;
                    }
                }
            }

                    return RedirectToAction(nameof(Index));
                }
            return prefix + (max + 1).ToString("D" + width);
        }

        private async Task SendInvitationEmail(
            string email,
            string fullName,
            string temporaryPassword,
            DateTime expiryUtc,
            string role,
            string numberLabel,
            string numberValue,
            IEnumerable<string> testTypes)
        {
            string loginUrl =
                _configuration["AppSettings:LoginUrl"]
                ?? throw new InvalidOperationException("The login URL has not been configured.");

            string Enc(string s) => System.Net.WebUtility.HtmlEncode(s ?? "");

            // Role and identifying details, shown clearly near the top of the email
            var details = new StringBuilder();
            details.Append($"<p><strong>Role:</strong> {Enc(role)}</p>");
            details.Append($"<p><strong>{Enc(numberLabel)}:</strong> {Enc(numberValue)}</p>");

                    return RedirectToAction(nameof(Index));
                }
            if (testTypes != null && testTypes.Any())
            {
                details.Append($"<p><strong>Assigned test types:</strong> {Enc(string.Join(", ", testTypes))}</p>");
            }

            details.Append($"<p><strong>Username:</strong> {Enc(email)}</p>");
            details.Append($"<p><strong>Temporary password:</strong> {Enc(temporaryPassword)}</p>");

            string subject = $"Your NMB-HLabSys {role} account";

            string body = $"""
            <html>
            <body style="font-family: Arial, sans-serif; color: #333;">
                <h2>Welcome to NMB-HLabSys</h2>

                <p>Hello {Enc(fullName)},</p>

                <p>A <strong>{Enc(role)}</strong> account has been created for you on NMB-HLabSys.</p>

                {details}

                <p>
                    <a href="{Enc(loginUrl)}"
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

                if (result)
                {
                    TempData["Success"] =
                        "Laboratory technician updated successfully.";
                }
                else
                {
                    TempData["Error"] =
                        "Unable to update technician.";
                }
                <p>
                    If you did not expect this account, contact your
                    system administrator.
                </p>

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to update technician: " + ex.Message;
                <p>Regards,<br/>NMB-HLabSys Administration</p>
            </body>
            </html>
            """;

                return RedirectToAction(nameof(Index));
            }
            await _emailService.SendEmailAsync(email, subject, body);
        }
    }
}
