using Microsoft.AspNetCore.Mvc;
using Patient.Models;

namespace Patient.Controllers
{
    public class DoctorController : Controller
    {
        public IActionResult Profile()
        {
            var vm = new DoctorProfileViewModel
            {
                DoctorID = 1,
                FirstName = "Dev",
                LastName = "Doctor",
                Email = "dev-doctor@test.com",
                LicenseNumber = "MP-2024-00123",
                DateOfBirth = new DateTime(1982, 4, 10),
                CellphoneNumber = "0831234567",
                HomeAddress = "45 Settler's Way, Port Elizabeth",
                Specialization = "Haematology",
                Department = "Haematology",
                PracticeAddress = "Greenacres Hospital, Port Elizabeth",
                RegistrationDate = DateTime.Now.AddYears(-3)
            };

            ViewBag.Email = vm.Email;
            return View(vm);
        }
        public IActionResult ShowProfile()
        {
            return View("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProfile(DoctorProfileViewModel model)
        {
            // Re-populate read-only fields not submitted by the form
            model.DoctorID = 1;
            model.Email = "dev-doctor@test.com";
            model.LicenseNumber = "MP-2024-00123";
            model.RegistrationDate = DateTime.Now.AddYears(-3);
            ViewBag.Email = model.Email;

            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Please fix the errors and try again.";
                return View("Profile", model);
            }

            // TODO: persist changes to database here
            ViewBag.Success = "Your profile has been updated successfully.";
            return View("Profile", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            var vm = new DoctorProfileViewModel
            {
                DoctorID = 1,
                FirstName = "Dev",
                LastName = "Doctor",
                Email = "dev-doctor@test.com",
                LicenseNumber = "MP-2024-00123",
                DateOfBirth = new DateTime(1982, 4, 10),
                CellphoneNumber = "0831234567",
                HomeAddress = "45 Settler's Way, Port Elizabeth",
                Specialization = "Haematology",
                Department = "Haematology",
                PracticeAddress = "Greenacres Hospital, Port Elizabeth",
                RegistrationDate = DateTime.Now.AddYears(-3)
            };

            ViewBag.Email = vm.Email;

            if (string.IsNullOrWhiteSpace(CurrentPassword))
            {
                ViewBag.Error = "Please enter your current password.";
                return View("Profile", vm);
            }
            if (NewPassword != ConfirmPassword)
            {
                ViewBag.Error = "New password and confirmation do not match.";
                return View("Profile", vm);
            }
            if (NewPassword.Length < 8)
            {
                ViewBag.Error = "New password must be at least 8 characters.";
                return View("Profile", vm);
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(NewPassword, "[A-Z]"))
            {
                ViewBag.Error = "New password must contain at least one uppercase letter.";
                return View("Profile", vm);
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(NewPassword, "[0-9]"))
            {
                ViewBag.Error = "New password must contain at least one number.";
                return View("Profile", vm);
            }
            if (NewPassword == CurrentPassword)
            {
                ViewBag.Error = "New password cannot be the same as your current password.";
                return View("Profile", vm);
            }

            // TODO: verify CurrentPassword hash and save NewPassword hash here
            ViewBag.Success = "Password changed successfully.";
            return View("Profile", vm);

        }
    }
}