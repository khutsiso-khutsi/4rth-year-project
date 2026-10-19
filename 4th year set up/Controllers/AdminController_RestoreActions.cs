using admin.Repository;
using Microsoft.AspNetCore.Mvc;

namespace _4th_year_set_up.Controllers
{
    // Serves /Admin/RestoreAllergy, /Admin/RestoreCondition, etc.
    // Separate file so it doesn't touch the existing AdminController.
    // Put this file in: 4th year set up/Controllers/
    [Route("Admin/[action]")]
    public class AdminRestoreController : Controller
    {
        private readonly AdminRepository _adminRepo;

        public AdminRestoreController(AdminRepository adminRepo)
        {
            _adminRepo = adminRepo;
        }

        // If your other admin actions check the session role, keep this check.
        // Adjust "Admin" if your role name differs.
        private bool IsAdmin() => HttpContext.Session.GetString("RoleName") == "Admin";

        [HttpPost]
        public IActionResult RestoreConditionCategory(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetMessage(_adminRepo.RestoreConditionCategory(id), "Condition category");
            return RedirectToAction("ConditionCategories", "Admin");
        }

        [HttpPost]
        public IActionResult RestoreCondition(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetMessage(_adminRepo.RestoreCondition(id), "Condition");
            return RedirectToAction("MedicalConditions", "Admin");   // fixed: was "Conditions"
        }

        [HttpPost]
        public IActionResult RestoreAllergyCategory(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetMessage(_adminRepo.RestoreAllergyCategory(id), "Allergy category");
            return RedirectToAction("AllergyCategories", "Admin");
        }

        [HttpPost]
        public IActionResult RestoreAllergy(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetMessage(_adminRepo.RestoreAllergy(id), "Allergy");
            return RedirectToAction("Allergies", "Admin");
        }

        [HttpPost]
        public IActionResult RestoreMedicationCategory(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetMessage(_adminRepo.RestoreMedicationCategory(id), "Medication category");
            return RedirectToAction("MedicationCategories", "Admin");
        }

        [HttpPost]
        public IActionResult RestoreMedication(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            SetMessage(_adminRepo.RestoreMedication(id), "Medication");
            return RedirectToAction("Medications", "Admin");
        }

        private void SetMessage(string result, string what)
        {
            if (result == "SUCCESS")
                TempData["Success"] = $"{what} restored.";
            else if (result == "NOT_FOUND")
                TempData["Error"] = $"{what} not found.";
            else
                TempData["Error"] = $"Could not restore {what.ToLower()}: {result}";
        }
    }
}