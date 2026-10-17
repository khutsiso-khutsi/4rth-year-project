using BCrypt.Net;
using LabManager.Models;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LabManager.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserProfileRepository _userProfileRepository;

        public UserController(
            IUserProfileRepository userProfileRepository)
        {
            _userProfileRepository = userProfileRepository;
        }

        // =========================================================
        // PROFILE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Login", "Home");

            var profile =
                await _userProfileRepository
                    .GetProfileByEmail(email);

            if (profile == null)
            {
                TempData["Error"] =
                    "Unable to load your profile.";

                return RedirectToAction(
                    "Index",
                    "ManagerDashboard");
            }

            ViewBag.Email = profile.Email;
            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAccountInfo(UserProfileViewModel model)
        {
            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Login", "Home");

            var profile = await _userProfileRepository.GetProfileByEmail(email);
            if (profile == null)
            {
                TempData["Error"] = "Your signed-in account could not be found.";
                return RedirectToAction(nameof(Profile));
            }

            if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
            {
                TempData["Error"] = "First name and last name are required.";
                return RedirectToAction(nameof(Profile));
            }

            // Keep account identity and contact details tied to the authenticated user.
            model.UserID = profile.UserID;
            model.Email = profile.Email;
            model.IDNumber = profile.IDNumber;
            model.CellphoneNumber = profile.CellphoneNumber;
            model.HomeAddress = profile.HomeAddress;

            var updated = await _userProfileRepository.UpdateProfile(model);
            TempData[updated ? "Success" : "Error"] = updated
                ? "Account information updated successfully."
                : "Your account information could not be updated.";
            return RedirectToAction(nameof(Profile));
        }



        // =========================================================
        // UPDATE PROFILE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            UserProfileViewModel model)
        {
            string? sessionEmail =
                HttpContext.Session.GetString("Email");

            if (string.IsNullOrWhiteSpace(sessionEmail))
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(model.FirstName))
            {
                TempData["Error"] =
                    "First name is required.";

                return RedirectToAction(nameof(Profile));
            }

            if (string.IsNullOrWhiteSpace(model.LastName))
            {
                TempData["Error"] =
                    "Last name is required.";

                return RedirectToAction(nameof(Profile));
            }

            if (string.IsNullOrWhiteSpace(model.CellphoneNumber))
            {
                TempData["Error"] =
                    "Cellphone number is required.";

                return RedirectToAction(nameof(Profile));
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    model.CellphoneNumber,
                    @"^0[6-8]\d{8}$"))
            {
                TempData["Error"] =
                    "Please enter a valid 10-digit South African cellphone number.";

                return RedirectToAction(nameof(Profile));
            }

            if (string.IsNullOrWhiteSpace(model.HomeAddress) ||
                model.HomeAddress.Trim().Length < 5)
            {
                TempData["Error"] =
                    "Please enter a valid home address.";

                return RedirectToAction(nameof(Profile));
            }

            try
            {
                /*
                 * Get the actual user from the session email.
                 * Do not trust UserID sent from the browser.
                 */

                var existingProfile =
                    await _userProfileRepository
                        .GetProfileByEmail(sessionEmail);

                if (existingProfile == null)
                {
                    TempData["Error"] =
                        "User account could not be found.";

                    return RedirectToAction(nameof(Profile));
                }

                model.UserID = existingProfile.UserID;
                model.Email = existingProfile.Email;
                model.FirstName = existingProfile.FirstName;
                model.LastName = existingProfile.LastName;
                model.IDNumber = existingProfile.IDNumber;
                model.DateOfBirth = existingProfile.DateOfBirth;

                bool updated =
                    await _userProfileRepository
                        .UpdateProfile(model);

                if (!updated)
                {
                    TempData["Error"] =
                        "Your profile could not be updated.";

                    return RedirectToAction(nameof(Profile));
                }

                TempData["Success"] =
                    "Your profile has been updated successfully.";

                return RedirectToAction(nameof(Profile));
            }
            catch
            {
                TempData["Error"] =
                    "An error occurred while updating your profile.";

                return RedirectToAction(nameof(Profile));
            }
        }


        // =========================================================
        // CHANGE PASSWORD
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            string CurrentPassword,
            string NewPassword,
            string ConfirmPassword)
        {
            string? email =
                HttpContext.Session.GetString("Email");

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction("Index", "Home");
            }


            // -----------------------------------------------------
            // VALIDATION
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(CurrentPassword))
            {
                TempData["Error"] =
                    "Please enter your current password.";

                return RedirectToAction(nameof(Profile));
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                TempData["Error"] =
                    "Please enter a new password.";

                return RedirectToAction(nameof(Profile));
            }

            if (NewPassword.Length < 8)
            {
                TempData["Error"] =
                    "New password must contain at least 8 characters.";

                return RedirectToAction(nameof(Profile));
            }

            if (!NewPassword.Any(char.IsUpper))
            {
                TempData["Error"] =
                    "New password must contain at least one uppercase letter.";

                return RedirectToAction(nameof(Profile));
            }

            if (!NewPassword.Any(char.IsDigit))
            {
                TempData["Error"] =
                    "New password must contain at least one number.";

                return RedirectToAction(nameof(Profile));
            }

            if (NewPassword != ConfirmPassword)
            {
                TempData["Error"] =
                    "New password and confirmation password do not match.";

                return RedirectToAction(nameof(Profile));
            }


            try
            {
                // -------------------------------------------------
                // GET STORED PASSWORD HASH
                // -------------------------------------------------

                string? storedHash =
                    await _userProfileRepository
                        .GetPasswordHash(email);

                if (string.IsNullOrWhiteSpace(storedHash))
                {
                    TempData["Error"] =
                        "Your password information could not be found.";

                    return RedirectToAction(nameof(Profile));
                }


                // -------------------------------------------------
                // VERIFY CURRENT PASSWORD
                // -------------------------------------------------

                bool currentPasswordCorrect =
                    BCrypt.Net.BCrypt.Verify(
                        CurrentPassword,
                        storedHash);

                if (!currentPasswordCorrect)
                {
                    TempData["Error"] =
                        "Your current password is incorrect.";

                    return RedirectToAction(nameof(Profile));
                }


                // -------------------------------------------------
                // PREVENT REUSING CURRENT PASSWORD
                // -------------------------------------------------

                bool samePassword =
                    BCrypt.Net.BCrypt.Verify(
                        NewPassword,
                        storedHash);

                if (samePassword)
                {
                    TempData["Error"] =
                        "New password cannot be the same as your current password.";

                    return RedirectToAction(nameof(Profile));
                }


                // -------------------------------------------------
                // HASH NEW PASSWORD
                // -------------------------------------------------

                string newPasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(NewPassword);


                // -------------------------------------------------
                // SAVE PASSWORD
                // -------------------------------------------------

                bool updated =
                    await _userProfileRepository
                        .UpdatePassword(
                            email,
                            newPasswordHash);

                if (!updated)
                {
                    TempData["Error"] =
                        "Your password could not be changed.";

                    return RedirectToAction(nameof(Profile));
                }


                // -------------------------------------------------
                // SUCCESS
                // -------------------------------------------------

                TempData["Success"] =
                    "Your password has been changed successfully.";

                return RedirectToAction(nameof(Profile));
            }
            catch
            {
                TempData["Error"] =
                    "An error occurred while changing your password.";

                return RedirectToAction(nameof(Profile));
            }



            //IEnumerable<TestCategory> category = await _repository.GetCategory();
            //ViewBag.Category = category.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.CategoryName });

            //IEnumerable<TestType> sample = await _repository.GetSampleType();
            //ViewBag.SampleType = sample.Select(c => new SelectListItem { Value = c.SampleTypeId.ToString(), Text = c.SampleName });
        }
    }
}
