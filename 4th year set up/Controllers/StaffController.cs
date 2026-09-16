using LabManager.Models;
using LabManager.Repositories;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;

namespace LabManager.Controllers
{
    public class StaffController : Controller
    {
        private readonly IStaffRepository _staffRepository;

        public StaffController(
            IStaffRepository staffRepository)
        {
            _staffRepository = staffRepository;
        }


        // ============================================================
        // STAFF PAGE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var doctors =
                    await _staffRepository.GetAllDoctor();

                var technicians =
                    await _staffRepository.GetTechnician();

                var testTypes =
                    await _staffRepository.GetAllTestTypes();

                var model = new StaffViewModel
                {
                    Doctors = doctors,
                    Technicians = technicians,
                    TestTypes = testTypes
                };

                ViewBag.Email =
                    HttpContext.Session.GetString("Email")
                    ?? User.Identity?.Name
                    ?? "Manager";

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load staff information: "
                    + ex.Message;

                return View(new StaffViewModel());
            }
        }


        // ============================================================
        // ADD DOCTOR
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctor(
            string firstName,
            string lastName,
            string hpcsaNumber,
            string email,
            string contactNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(firstName))
                {
                    TempData["Error"] =
                        "First name is required.";

                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(lastName))
                {
                    TempData["Error"] =
                        "Last name is required.";

                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(hpcsaNumber))
                {
                    TempData["Error"] =
                        "HPCSA number is required.";

                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    TempData["Error"] =
                        "Email address is required.";

                    return RedirectToAction(nameof(Index));
                }


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


                bool result =
                    await _staffRepository.AddDoctor(doctor);


                if (result)
                {
                    TempData["Success"] =
                        "Doctor added successfully.";
                }
                else
                {
                    TempData["Error"] =
                        "Unable to add doctor.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to add doctor: " + ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }


        // ============================================================
        // EDIT DOCTOR
        // ============================================================

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
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] =
                        "Invalid doctor.";

                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(firstName))
                {
                    TempData["Error"] =
                        "First name is required.";

                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(lastName))
                {
                    TempData["Error"] =
                        "Last name is required.";

                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(hpcsaNumber))
                {
                    TempData["Error"] =
                        "HPCSA number is required.";

                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    TempData["Error"] =
                        "Email address is required.";

                    return RedirectToAction(nameof(Index));
                }


                var doctor = new Doctor
                {
                    Id = id,
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    HCPSANumber = hpcsaNumber.Trim(),
                    EmailAddress = email.Trim(),
                    ContactNumber = contactNumber?.Trim()
                };


                bool result =
                    await _staffRepository.UpdateDoctor(doctor);


                if (result)
                {
                    TempData["Success"] =
                        "Doctor updated successfully.";
                }
                else
                {
                    TempData["Error"] =
                        "Unable to update doctor.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to update doctor: " + ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }


        // ============================================================
        // ADD TECHNICIAN
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTechnician(
            string firstName,
            string lastName,
            string employeeNumber,
            string email,
            int testTypeID)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(firstName))
                {
                    TempData["Error"] =
                        "First name is required.";

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

                if (string.IsNullOrWhiteSpace(email))
                {
                    TempData["Error"] =
                        "Email address is required.";

                    return RedirectToAction(nameof(Index));
                }

                if (testTypeID <= 0)
                {
                    TempData["Error"] =
                        "Please select a test type.";

                    return RedirectToAction(nameof(Index));
                }


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


                bool result =
                    await _staffRepository.AddTechnician(
                        technician);


                if (result)
                {
                    TempData["Success"] =
                        "Laboratory technician added successfully.";
                }
                else
                {
                    TempData["Error"] =
                        "Unable to add laboratory technician.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to add technician: " + ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }


        // ============================================================
        // EDIT TECHNICIAN
        // ============================================================

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
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] =
                        "Invalid technician.";

                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(firstName))
                {
                    TempData["Error"] =
                        "First name is required.";

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

                if (string.IsNullOrWhiteSpace(email))
                {
                    TempData["Error"] =
                        "Email address is required.";

                    return RedirectToAction(nameof(Index));
                }

                if (testTypeID <= 0)
                {
                    TempData["Error"] =
                        "Please select a test type.";

                    return RedirectToAction(nameof(Index));
                }


                var technician = new Technician
                {
                    Id = id,
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    EmployeeNumber = employeeNumber.Trim(),
                    EmailAddress = email.Trim(),
                    TestTypeID = testTypeID
                };


                bool result =
                    await _staffRepository.UpdateTechnician(
                        technician);


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

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to update technician: " + ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }
    }
}