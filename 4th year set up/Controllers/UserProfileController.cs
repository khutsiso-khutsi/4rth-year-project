using LabManager.Models;
using LabManager.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace LabManager.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // =====================================================
        // PASSWORD HASHING
        // IMPORTANT: replace these two methods with the SAME scheme your
        // login uses. If login hashes differently, ChangePassword will
        // reject the correct current password or lock the user out.
        // =====================================================
      
        private static readonly object HashUser = new object();

        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private static bool VerifyPassword(string hash, string password)
        {
            if (string.IsNullOrWhiteSpace(hash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (ArgumentException)
            {
                // The stored hash is malformed or is not a valid BCrypt hash.
                return false;
            }
        }
        // =====================================================
        // HELPERS
        // =====================================================
        private string SignedInEmail =>
            HttpContext.Session.GetString("Email");

        // First validation message from the request model's data annotations
        private string FirstModelError() =>
            ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
            ?? "Please check the form and try again.";

        private async Task<IActionResult> ShowProfile(
            string email,
            string success = null,
            string error = null)
        {
            var model = await _userRepository.GetProfile(email);

            if (model == null)
                return RedirectToAction("Index", "Home");

            ViewBag.Email = email;
            ViewBag.Success = success;
            ViewBag.Error = error;

            return View("Profile", model);
        }

        // Stricter than [EmailAddress], which accepts things like "a@b"
        private static bool IsValidEmail(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            try
            {
                var address = new MailAddress(value.Trim());
                return address.Address == value.Trim() &&
                       address.Host.Contains('.');
            }
            catch
            {
                return false;
            }
        }

        // =====================================================
        // PROFILE PAGE
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var email = SignedInEmail;

            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Index", "Home");

            return await ShowProfile(email);
        }

        // =====================================================
        // UPDATE ACCOUNT INFORMATION
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAccountInfo(UpdateAccountInfoRequest request)
        {
            var currentEmail = SignedInEmail;

            if (string.IsNullOrWhiteSpace(currentEmail))
                return RedirectToAction("Index", "Home");

            // 1. Data-annotation rules (required, lengths, formats, DOB range)
            if (!ModelState.IsValid)
                return await ShowProfile(currentEmail, error: FirstModelError());

            var newEmail = request.Email.Trim();
            var idNumber = request.IDNumber.Trim();
            var dob = request.DateOfBirth.Value;

            // 2. Stricter email check
            if (!IsValidEmail(newEmail))
                return await ShowProfile(currentEmail, error: "Please enter a valid email address.");

            // 3. South African ID number: citizenship digit + checksum
            if (!SaIdNumber.IsValid(idNumber))
                return await ShowProfile(currentEmail,
                    error: "That is not a valid South African ID number.");

            // 4. ID number must agree with the date of birth
            if (!SaIdNumber.MatchesDateOfBirth(idNumber, dob))
                return await ShowProfile(currentEmail,
                    error: "The ID number does not match the date of birth entered.");

            // 5. If the email is changing, nobody else may already have it
            if (!string.Equals(newEmail, currentEmail, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userRepository.GetProfile(newEmail);

                if (existing != null)
                    return await ShowProfile(currentEmail, error: "That email address is already in use.");
            }

            var model = new UserProfileViewModel
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = newEmail,
                IDNumber = idNumber,
                DateOfBirth = dob
            };

            bool result = await _userRepository.UpdateAccountInfo(currentEmail, model);

            if (!result)
                return await ShowProfile(currentEmail, error: "Unable to update account information.");

            // Keep the session in step with the new email
            HttpContext.Session.SetString("Email", newEmail);

            return await ShowProfile(newEmail, success: "Account information updated successfully.");
        }

        // =====================================================
        // UPDATE CELLPHONE + HOME ADDRESS
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateContactRequest request)
        {
            var email = SignedInEmail;

            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Index", "Home");

            if (!ModelState.IsValid)
                return await ShowProfile(email, error: FirstModelError());

            bool result = await _userRepository.UpdateContactDetails(
                email,
                request.CellphoneNumber.Trim(),
                request.HomeAddress.Trim());

            return await ShowProfile(
                email,
                success: result ? "Profile updated successfully." : null,
                error: result ? null : "Unable to update profile.");
        }

        // =====================================================
        // CHANGE PASSWORD
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var email = SignedInEmail;

            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Index", "Home");

            if (!ModelState.IsValid)
                return await ShowProfile(email, error: FirstModelError());

            if (request.NewPassword == request.CurrentPassword)
                return await ShowProfile(email,
                    error: "New password cannot be the same as your current password.");

            var storedHash = await _userRepository.GetPasswordHash(email);

            if (!VerifyPassword(storedHash, request.CurrentPassword))
                return await ShowProfile(email, error: "Your current password is incorrect.");

            bool result = await _userRepository.UpdatePasswordHash(email, HashPassword(request.NewPassword));

            return await ShowProfile(
                email,
                success: result ? "Password changed successfully." : null,
                error: result ? null : "Unable to change password.");
        }
    }
}
