using Microsoft.AspNetCore.Mvc;
using System.Linq;
using admin.Repository;
using Patient.Repository;

namespace _4th_year_set_up.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminRepository _adminRepo;
        private readonly UserRepository _userRepo;   // used for the admin's own password check/change

        public AdminController(AdminRepository adminRepo, UserRepository userRepo)
        {
            _adminRepo = adminRepo;
            _userRepo = userRepo;
        }

        // ── DASHBOARD ─────────────────────────────────────────
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            var (conditions, allergies, medications, conditionCats, allergyCats, medicationCats) = _adminRepo.GetDashboardCounts();

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
            ViewData["ConditionCount"] = conditions;
            ViewData["AllergyCount"] = allergies;
            ViewData["MedicationCount"] = medications;
            ViewData["ConditionCatCount"] = conditionCats;
            ViewData["AllergyCatCount"] = allergyCats;
            ViewData["MedCatCount"] = medicationCats;
            ViewData["CategoryCount"] = conditionCats + allergyCats + medicationCats;
            ViewData["RecentActivity"] = _adminRepo.GetActivityLog().OrderByDescending(l => l.LogDate).Take(5).ToList();

            return View("~/Views/Admin/AdminDashboard.cshtml");
        }

        // ── CONDITION CATEGORIES ──────────────────────────────
        public IActionResult ConditionCategories(bool showInactive = false)
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
            ViewData["Categories"] = _adminRepo.GetAllConditionCategories(showInactive);
            ViewData["ShowInactive"] = showInactive;
            return View("~/Views/Admin/ConditionCategories.cshtml");
        }

        [HttpPost]
        public IActionResult CreateConditionCategory(string CategoryName, string? Description)
        {
            var result = _adminRepo.CreateConditionCategory(CategoryName, Description);
            if (result == "SUCCESS")
            {
                _adminRepo.LogActivity($"Created condition category: {CategoryName}", HttpContext.Session.GetString("Email")!);
                TempData["Success"] = "Category created successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to create category: " + result;
            }
            return RedirectToAction("ConditionCategories");
        }

        [HttpPost]
        public IActionResult EditConditionCategory(int ConditionCategoryID, string CategoryName, string? Description)
        {
            var result = _adminRepo.UpdateConditionCategory(ConditionCategoryID, CategoryName, Description);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Category updated successfully."
                : "Failed to update category: " + result;
            return RedirectToAction("ConditionCategories");
        }

        [HttpPost]
        public IActionResult DeleteConditionCategory(int id)
        {
            var result = _adminRepo.DeleteConditionCategory(id);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Category deleted successfully."
                : "Failed to delete category: " + result;
            return RedirectToAction("ConditionCategories");
        }

        // ── MEDICAL CONDITIONS ────────────────────────────────
        public IActionResult MedicalConditions()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
            ViewData["Conditions"] = _adminRepo.GetAllConditions();
            ViewData["Categories"] = _adminRepo.GetAllConditionCategories(false);
            return View("~/Views/Admin/MedicalConditions.cshtml");
        }

        [HttpPost]
        public IActionResult CreateCondition(string ConditionName, string? Description, int ConditionCategoryID)
        {
            var result = _adminRepo.CreateCondition(ConditionName, Description, ConditionCategoryID);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Condition created successfully."
                : "Failed to create condition: " + result;
            return RedirectToAction("MedicalConditions");
        }

        [HttpPost]
        public IActionResult EditCondition(int ConditionID, string ConditionName, string? Description, int ConditionCategoryID)
        {
            var result = _adminRepo.UpdateCondition(ConditionID, ConditionName, Description, ConditionCategoryID);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Condition updated successfully."
                : "Failed to update condition: " + result;
            return RedirectToAction("MedicalConditions");
        }

        [HttpPost]
        public IActionResult DeleteCondition(int id)
        {
            var result = _adminRepo.DeleteCondition(id);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Condition deleted successfully."
                : "Failed to delete condition: " + result;
            return RedirectToAction("MedicalConditions");
        }

        // ── ALLERGY CATEGORIES ────────────────────────────────
        public IActionResult AllergyCategories(bool showInactive = false)
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
            ViewData["Categories"] = _adminRepo.GetAllAllergyCategories(showInactive);
            ViewData["ShowInactive"] = showInactive;
            return View("~/Views/Admin/AllergyCategories.cshtml");
        }

        [HttpPost]
        public IActionResult CreateAllergyCategory(string CategoryName, string? Description)
        {
            var result = _adminRepo.CreateAllergyCategory(CategoryName, Description);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Category created successfully."
                : "Failed to create category: " + result;
            return RedirectToAction("AllergyCategories");
        }

        [HttpPost]
        public IActionResult EditAllergyCategory(int AllergyCategoryID, string CategoryName, string? Description)
        {
            var result = _adminRepo.UpdateAllergyCategory(AllergyCategoryID, CategoryName, Description);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Category updated successfully."
                : "Failed to update category: " + result;
            return RedirectToAction("AllergyCategories");
        }

        [HttpPost]
        public IActionResult DeleteAllergyCategory(int id)
        {
            var result = _adminRepo.DeleteAllergyCategory(id);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Category deleted successfully."
                : "Failed to delete category: " + result;
            return RedirectToAction("AllergyCategories");
        }

        // ── ALLERGIES ─────────────────────────────────────────
        public IActionResult Allergies()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
            ViewData["Allergies"] = _adminRepo.GetAllAllergies();
            ViewData["Categories"] = _adminRepo.GetAllAllergyCategories(false);
            return View("~/Views/Admin/Allergies.cshtml");
        }

        [HttpPost]
        public IActionResult CreateAllergy(string AllergyName, string? Description, int AllergyCategoryID)
        {
            var result = _adminRepo.CreateAllergy(AllergyName, Description, AllergyCategoryID);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Allergy created successfully."
                : "Failed to create allergy: " + result;
            return RedirectToAction("Allergies");
        }

        [HttpPost]
        public IActionResult EditAllergy(int AllergyID, string AllergyName, string? Description, int AllergyCategoryID)
        {
            var result = _adminRepo.UpdateAllergy(AllergyID, AllergyName, Description, AllergyCategoryID);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Allergy updated successfully."
                : "Failed to update allergy: " + result;
            return RedirectToAction("Allergies");
        }

        [HttpPost]
        public IActionResult DeleteAllergy(int id)
        {
            var result = _adminRepo.DeleteAllergy(id);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Allergy deleted successfully."
                : "Failed to delete allergy: " + result;
            return RedirectToAction("Allergies");
        }

        // ── MEDICATION CATEGORIES ─────────────────────────────
        public IActionResult MedicationCategories(bool showInactive = false)
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
            ViewData["Categories"] = _adminRepo.GetAllMedicationCategories(showInactive);
            ViewData["ShowInactive"] = showInactive;
            return View("~/Views/Admin/MedicationCategories.cshtml");
        }

        [HttpPost]
        public IActionResult CreateMedicationCategory(string CategoryName, string? Description)
        {
            var result = _adminRepo.CreateMedicationCategory(CategoryName, Description);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Category created successfully."
                : "Failed to create category: " + result;
            return RedirectToAction("MedicationCategories");
        }

        [HttpPost]
        public IActionResult EditMedicationCategory(int MedicationCategoryID, string CategoryName, string? Description)
        {
            var result = _adminRepo.UpdateMedicationCategory(MedicationCategoryID, CategoryName, Description);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Category updated successfully."
                : "Failed to update category: " + result;
            return RedirectToAction("MedicationCategories");
        }

        [HttpPost]
        public IActionResult DeleteMedicationCategory(int id)
        {
            var result = _adminRepo.DeleteMedicationCategory(id);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Category deleted successfully."
                : "Failed to delete category: " + result;
            return RedirectToAction("MedicationCategories");
        }

        // ── MEDICATIONS ───────────────────────────────────────
        public IActionResult Medications()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
            ViewData["Medications"] = _adminRepo.GetAllMedications();
            ViewData["Categories"] = _adminRepo.GetAllMedicationCategories(false);
            return View("~/Views/Admin/Medications.cshtml");
        }

        [HttpPost]
        public IActionResult CreateMedication(string MedicationName, string? Description, int MedicationCategoryID)
        {
            var result = _adminRepo.CreateMedication(MedicationName, Description, MedicationCategoryID);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Medication created successfully."
                : "Failed to create medication: " + result;
            return RedirectToAction("Medications");
        }

        [HttpPost]
        public IActionResult EditMedication(int MedicationID, string MedicationName, string? Description, int MedicationCategoryID)
        {
            var result = _adminRepo.UpdateMedication(MedicationID, MedicationName, Description, MedicationCategoryID);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Medication updated successfully."
                : "Failed to update medication: " + result;
            return RedirectToAction("Medications");
        }

        [HttpPost]
        public IActionResult DeleteMedication(int id)
        {
            var result = _adminRepo.DeleteMedication(id);
            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                ? "Medication deleted successfully."
                : "Failed to delete medication: " + result;
            return RedirectToAction("Medications");
        }

        // ── MY PROFILE ────────────────────────────────────────
        public IActionResult Profile()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            var email = HttpContext.Session.GetString("Email") ?? "";
            var account = _adminRepo.GetAdminAccount(userId);

            ViewData["AdminEmail"] = email;
            ViewData["Username"] = account?.Username ?? "";
            ViewData["MemberSince"] = account?.CreatedDate;
            ViewData["RecentActivity"] = _adminRepo.GetActivityLog()
                .Where(l => string.Equals(l.PerformedBy, email, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(l => l.LogDate)
                .Take(5)
                .ToList();
            return View("~/Views/Admin/Profile.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            CurrentPassword ??= "";
            NewPassword ??= "";
            ConfirmPassword ??= "";

            string? error =
                CurrentPassword.Length == 0 ? "Please enter your current password." :
                !_userRepo.PasswordMatches(userId, CurrentPassword) ? "Your current password is incorrect." :
                PasswordRuleError(NewPassword) is string ruleError ? ruleError :
                NewPassword != ConfirmPassword ? "New password and confirmation do not match." :
                NewPassword == CurrentPassword ? "New password cannot be the same as your current password." :
                null;

            if (error != null)
            {
                TempData["Error"] = error;
                return RedirectToAction("Profile");
            }

            _userRepo.SetOwnPassword(userId, NewPassword);
            _adminRepo.LogActivity("Changed own password", HttpContext.Session.GetString("Email") ?? "");
            TempData["Success"] = "Your password has been changed. Use the new one next time you sign in.";
            return RedirectToAction("Profile");
        }

        // Same password rules as the Doctor and Patient pages.
        private static string? PasswordRuleError(string password)
        {
            if (password.Length < 8) return "New password must be at least 8 characters.";
            if (!password.Any(char.IsUpper)) return "New password must contain at least one uppercase letter.";
            if (!password.Any(char.IsDigit)) return "New password must contain at least one number.";
            if (password.All(char.IsLetterOrDigit)) return "New password must contain at least one special character.";
            return null;
        }

        // ── ACTIVITY LOG ──────────────────────────────────────
        public IActionResult ActivityLog()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
            ViewData["Logs"] = _adminRepo.GetActivityLog();
            return View("~/Views/Admin/ActivityLog.cshtml");
        }

    }
}