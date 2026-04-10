using Microsoft.AspNetCore.Mvc;
using admin.Repository;

namespace _4th_year_set_up.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminRepository _adminRepo;

        public AdminController(AdminRepository adminRepo)
        {
            _adminRepo = adminRepo;
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

            return View("~/Views/Admin/AdminDashboard.cshtml");
        }

        // ── CONDITION CATEGORIES ──────────────────────────────
        public IActionResult ConditionCategories()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
            ViewData["Categories"] = _adminRepo.GetAllConditionCategories();
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
            ViewData["Categories"] = _adminRepo.GetAllConditionCategories();
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
        public IActionResult AllergyCategories()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
            ViewData["Categories"] = _adminRepo.GetAllAllergyCategories();
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
            ViewData["Categories"] = _adminRepo.GetAllAllergyCategories();
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
        public IActionResult MedicationCategories()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
            ViewData["Categories"] = _adminRepo.GetAllMedicationCategories();
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
            ViewData["Categories"] = _adminRepo.GetAllMedicationCategories();
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