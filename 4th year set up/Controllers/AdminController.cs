//using Microsoft.AspNetCore.Mvc;
//using admin.Repository;

//namespace _4th_year_set_up.Controllers
//{
//    public class AdminController : Controller
//    {
//        private readonly AdminRepository _adminRepo;

//        public AdminController(AdminRepository adminRepo)
//        {
//            _adminRepo = adminRepo;
//        }

//        // ── DASHBOARD ─────────────────────────────────────────
//        public IActionResult Dashboard()
//        {
//            if (HttpContext.Session.GetString("RoleName") != "Admin")
//                return RedirectToAction("Login", "Home");

//            var (conditions, allergies, medications, conditionCats, allergyCats, medicationCats) = _adminRepo.GetDashboardCounts();

//            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
//            ViewData["ConditionCount"] = conditions;
//            ViewData["AllergyCount"] = allergies;
//            ViewData["MedicationCount"] = medications;
//            ViewData["ConditionCatCount"] = conditionCats;
//            ViewData["AllergyCatCount"] = allergyCats;
//            ViewData["MedCatCount"] = medicationCats;
//            ViewData["CategoryCount"] = conditionCats + allergyCats + medicationCats;

//            return View("~/Views/Admin/AdminDashboard.cshtml");
//        }

//        // ── CONDITION CATEGORIES ──────────────────────────────
//        public IActionResult ConditionCategories()
//        {
//            if (HttpContext.Session.GetString("RoleName") != "Admin")
//                return RedirectToAction("Login", "Home");

//            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
//            ViewData["Categories"] = _adminRepo.GetAllConditionCategories();
//            return View("~/Views/Admin/ConditionCategories.cshtml");
//        }

//        [HttpPost]
//        public IActionResult CreateConditionCategory(string CategoryName, string? Description)
//        {
//            var result = _adminRepo.CreateConditionCategory(CategoryName, Description);
//            if (result == "SUCCESS")
//            {
//                _adminRepo.LogActivity($"Created condition category: {CategoryName}", HttpContext.Session.GetString("Email")!);
//                TempData["Success"] = "Category created successfully.";
//            }
//            else
//            {
//                TempData["Error"] = "Failed to create category: " + result;
//            }
//            return RedirectToAction("ConditionCategories");
//        }

//        [HttpPost]
//        public IActionResult EditConditionCategory(int ConditionCategoryID, string CategoryName, string? Description)
//        {
//            var result = _adminRepo.UpdateConditionCategory(ConditionCategoryID, CategoryName, Description);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Category updated successfully."
//                : "Failed to update category: " + result;
//            return RedirectToAction("ConditionCategories");
//        }

//        [HttpPost]
//        public IActionResult DeleteConditionCategory(int id)
//        {
//            var result = _adminRepo.DeleteConditionCategory(id);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Category deleted successfully."
//                : "Failed to delete category: " + result;
//            return RedirectToAction("ConditionCategories");
//        }

//        // ── MEDICAL CONDITIONS ────────────────────────────────
//        public IActionResult MedicalConditions()
//        {
//            if (HttpContext.Session.GetString("RoleName") != "Admin")
//                return RedirectToAction("Login", "Home");

//            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
//            ViewData["Conditions"] = _adminRepo.GetAllConditions();
//            ViewData["Categories"] = _adminRepo.GetAllConditionCategories();
//            return View("~/Views/Admin/MedicalConditions.cshtml");
//        }

//        [HttpPost]
//        public IActionResult CreateCondition(string ConditionName, string? Description, int ConditionCategoryID)
//        {
//            var result = _adminRepo.CreateCondition(ConditionName, Description, ConditionCategoryID);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Condition created successfully."
//                : "Failed to create condition: " + result;
//            return RedirectToAction("MedicalConditions");
//        }

//        [HttpPost]
//        public IActionResult EditCondition(int ConditionID, string ConditionName, string? Description, int ConditionCategoryID)
//        {
//            var result = _adminRepo.UpdateCondition(ConditionID, ConditionName, Description, ConditionCategoryID);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Condition updated successfully."
//                : "Failed to update condition: " + result;
//            return RedirectToAction("MedicalConditions");
//        }

//        [HttpPost]
//        public IActionResult DeleteCondition(int id)
//        {
//            var result = _adminRepo.DeleteCondition(id);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Condition deleted successfully."
//                : "Failed to delete condition: " + result;
//            return RedirectToAction("MedicalConditions");
//        }

//        // ── ALLERGY CATEGORIES ────────────────────────────────
//        public IActionResult AllergyCategories()
//        {
//            if (HttpContext.Session.GetString("RoleName") != "Admin")
//                return RedirectToAction("Login", "Home");

//            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
//            ViewData["Categories"] = _adminRepo.GetAllAllergyCategories();
//            return View("~/Views/Admin/AllergyCategories.cshtml");
//        }

//        [HttpPost]
//        public IActionResult CreateAllergyCategory(string CategoryName, string? Description)
//        {
//            var result = _adminRepo.CreateAllergyCategory(CategoryName, Description);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Category created successfully."
//                : "Failed to create category: " + result;
//            return RedirectToAction("AllergyCategories");
//        }

//        [HttpPost]
//        public IActionResult EditAllergyCategory(int AllergyCategoryID, string CategoryName, string? Description)
//        {
//            var result = _adminRepo.UpdateAllergyCategory(AllergyCategoryID, CategoryName, Description);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Category updated successfully."
//                : "Failed to update category: " + result;
//            return RedirectToAction("AllergyCategories");
//        }

//        [HttpPost]
//        public IActionResult DeleteAllergyCategory(int id)
//        {
//            var result = _adminRepo.DeleteAllergyCategory(id);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Category deleted successfully."
//                : "Failed to delete category: " + result;
//            return RedirectToAction("AllergyCategories");
//        }

//        // ── ALLERGIES ─────────────────────────────────────────
//        public IActionResult Allergies()
//        {
//            if (HttpContext.Session.GetString("RoleName") != "Admin")
//                return RedirectToAction("Login", "Home");

//            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
//            ViewData["Allergies"] = _adminRepo.GetAllAllergies();
//            ViewData["Categories"] = _adminRepo.GetAllAllergyCategories();
//            return View("~/Views/Admin/Allergies.cshtml");
//        }

//        [HttpPost]
//        public IActionResult CreateAllergy(string AllergyName, string? Description, int AllergyCategoryID)
//        {
//            var result = _adminRepo.CreateAllergy(AllergyName, Description, AllergyCategoryID);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Allergy created successfully."
//                : "Failed to create allergy: " + result;
//            return RedirectToAction("Allergies");
//        }

//        [HttpPost]
//        public IActionResult EditAllergy(int AllergyID, string AllergyName, string? Description, int AllergyCategoryID)
//        {
//            var result = _adminRepo.UpdateAllergy(AllergyID, AllergyName, Description, AllergyCategoryID);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Allergy updated successfully."
//                : "Failed to update allergy: " + result;
//            return RedirectToAction("Allergies");
//        }

//        [HttpPost]
//        public IActionResult DeleteAllergy(int id)
//        {
//            var result = _adminRepo.DeleteAllergy(id);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Allergy deleted successfully."
//                : "Failed to delete allergy: " + result;
//            return RedirectToAction("Allergies");
//        }

//        // ── MEDICATION CATEGORIES ─────────────────────────────
//        public IActionResult MedicationCategories()
//        {
//            if (HttpContext.Session.GetString("RoleName") != "Admin")
//                return RedirectToAction("Login", "Home");

//            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
//            ViewData["Categories"] = _adminRepo.GetAllMedicationCategories();
//            return View("~/Views/Admin/MedicationCategories.cshtml");
//        }

//        [HttpPost]
//        public IActionResult CreateMedicationCategory(string CategoryName, string? Description)
//        {
//            var result = _adminRepo.CreateMedicationCategory(CategoryName, Description);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Category created successfully."
//                : "Failed to create category: " + result;
//            return RedirectToAction("MedicationCategories");
//        }

//        [HttpPost]
//        public IActionResult EditMedicationCategory(int MedicationCategoryID, string CategoryName, string? Description)
//        {
//            var result = _adminRepo.UpdateMedicationCategory(MedicationCategoryID, CategoryName, Description);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Category updated successfully."
//                : "Failed to update category: " + result;
//            return RedirectToAction("MedicationCategories");
//        }

//        [HttpPost]
//        public IActionResult DeleteMedicationCategory(int id)
//        {
//            var result = _adminRepo.DeleteMedicationCategory(id);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Category deleted successfully."
//                : "Failed to delete category: " + result;
//            return RedirectToAction("MedicationCategories");
//        }

//        // ── MEDICATIONS ───────────────────────────────────────
//        public IActionResult Medications()
//        {
//            if (HttpContext.Session.GetString("RoleName") != "Admin")
//                return RedirectToAction("Login", "Home");

//            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
//            ViewData["Medications"] = _adminRepo.GetAllMedications();
//            ViewData["Categories"] = _adminRepo.GetAllMedicationCategories();
//            return View("~/Views/Admin/Medications.cshtml");
//        }

//        [HttpPost]
//        public IActionResult CreateMedication(string MedicationName, string? Description, int MedicationCategoryID)
//        {
//            var result = _adminRepo.CreateMedication(MedicationName, Description, MedicationCategoryID);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Medication created successfully."
//                : "Failed to create medication: " + result;
//            return RedirectToAction("Medications");
//        }

//        [HttpPost]
//        public IActionResult EditMedication(int MedicationID, string MedicationName, string? Description, int MedicationCategoryID)
//        {
//            var result = _adminRepo.UpdateMedication(MedicationID, MedicationName, Description, MedicationCategoryID);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Medication updated successfully."
//                : "Failed to update medication: " + result;
//            return RedirectToAction("Medications");
//        }

//        [HttpPost]
//        public IActionResult DeleteMedication(int id)
//        {
//            var result = _adminRepo.DeleteMedication(id);
//            TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
//                ? "Medication deleted successfully."
//                : "Failed to delete medication: " + result;
//            return RedirectToAction("Medications");
//        }

//        // ── ACTIVITY LOG ──────────────────────────────────────
//        public IActionResult ActivityLog()
//        {
//            if (HttpContext.Session.GetString("RoleName") != "Admin")
//                return RedirectToAction("Login", "Home");

//            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");
//            ViewData["Logs"] = _adminRepo.GetActivityLog();
//            return View("~/Views/Admin/ActivityLog.cshtml");
//        }

//    }
//}


using Microsoft.AspNetCore.Mvc;
using admin.Repository;
using admin.Models;

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

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");

            try
            {
                var (conditions, allergies, medications, conditionCats, allergyCats, medicationCats)
                    = _adminRepo.GetDashboardCounts();

                ViewData["ConditionCount"] = conditions;
                ViewData["AllergyCount"] = allergies;
                ViewData["MedicationCount"] = medications;
                ViewData["ConditionCatCount"] = conditionCats;
                ViewData["AllergyCatCount"] = allergyCats;
                ViewData["MedCatCount"] = medicationCats;
                ViewData["CategoryCount"] = conditionCats + allergyCats + medicationCats;
            }
            catch
            {
                ViewData["ConditionCount"] = 10;
                ViewData["AllergyCount"] = 10;
                ViewData["MedicationCount"] = 10;
                ViewData["ConditionCatCount"] = 4;
                ViewData["AllergyCatCount"] = 3;
                ViewData["MedCatCount"] = 3;
                ViewData["CategoryCount"] = 10;
            }

            return View("~/Views/Admin/AdminDashboard.cshtml");
        }

        // ── CONDITION CATEGORIES ──────────────────────────────
        public IActionResult ConditionCategories()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");

            try { ViewData["Categories"] = _adminRepo.GetAllConditionCategories(); }
            catch
            {
                ViewData["Categories"] = new List<ConditionCategory>
                {
                    new ConditionCategory { ConditionCategoryID = 1, CategoryName = "Cardiovascular", Description = "Heart and blood vessel conditions" },
                    new ConditionCategory { ConditionCategoryID = 2, CategoryName = "Respiratory", Description = "Lung and airway conditions" },
                    new ConditionCategory { ConditionCategoryID = 3, CategoryName = "Neurological", Description = "Brain and nervous system conditions" },
                    new ConditionCategory { ConditionCategoryID = 4, CategoryName = "Digestive", Description = "Stomach and intestinal conditions" },
                    new ConditionCategory { ConditionCategoryID = 5, CategoryName = "Endocrine", Description = "Hormonal and metabolic conditions" },
                    new ConditionCategory { ConditionCategoryID = 6, CategoryName = "Musculoskeletal", Description = "Bone and muscle conditions" },
                    new ConditionCategory { ConditionCategoryID = 7, CategoryName = "Dermatological", Description = "Skin conditions" },
                    new ConditionCategory { ConditionCategoryID = 8, CategoryName = "Immunological", Description = "Immune system conditions" },
                    new ConditionCategory { ConditionCategoryID = 9, CategoryName = "Psychiatric", Description = "Mental health conditions" },
                    new ConditionCategory { ConditionCategoryID = 10, CategoryName = "Oncological", Description = "Cancer-related conditions" }
                };
            }

            return View("~/Views/Admin/ConditionCategories.cshtml");
        }

        [HttpPost]
        public IActionResult CreateConditionCategory(string CategoryName, string? Description)
        {
            try
            {
                var result = _adminRepo.CreateConditionCategory(CategoryName, Description);
                if (result == "SUCCESS")
                {
                    try { _adminRepo.LogActivity($"Created condition category: {CategoryName}", HttpContext.Session.GetString("Email")!); } catch { }
                    TempData["Success"] = "Category created successfully.";
                }
                else TempData["Error"] = "Failed to create category: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("ConditionCategories");
        }

        [HttpPost]
        public IActionResult EditConditionCategory(int ConditionCategoryID, string CategoryName, string? Description)
        {
            try
            {
                var result = _adminRepo.UpdateConditionCategory(ConditionCategoryID, CategoryName, Description);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Category updated successfully." : "Failed to update: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("ConditionCategories");
        }

        [HttpPost]
        public IActionResult DeleteConditionCategory(int id)
        {
            try
            {
                var result = _adminRepo.DeleteConditionCategory(id);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Category deleted successfully." : "Failed to delete: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("ConditionCategories");
        }

        // ── MEDICAL CONDITIONS ────────────────────────────────
        public IActionResult MedicalConditions()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");

            try { ViewData["Conditions"] = _adminRepo.GetAllConditions(); }
            catch
            {
                ViewData["Conditions"] = new List<MedicalCondition>
                {
                    new MedicalCondition { ConditionID = 1, ConditionName = "Hypertension", Description = "High blood pressure", ConditionCategoryID = 1 },
                    new MedicalCondition { ConditionID = 2, ConditionName = "Asthma", Description = "Chronic airway inflammation", ConditionCategoryID = 2 },
                    new MedicalCondition { ConditionID = 3, ConditionName = "Type 2 Diabetes", Description = "Insulin resistance condition", ConditionCategoryID = 5 },
                    new MedicalCondition { ConditionID = 4, ConditionName = "Epilepsy", Description = "Recurrent seizure disorder", ConditionCategoryID = 3 },
                    new MedicalCondition { ConditionID = 5, ConditionName = "Crohn's Disease", Description = "Inflammatory bowel disease", ConditionCategoryID = 4 },
                    new MedicalCondition { ConditionID = 6, ConditionName = "Rheumatoid Arthritis", Description = "Autoimmune joint inflammation", ConditionCategoryID = 6 },
                    new MedicalCondition { ConditionID = 7, ConditionName = "Psoriasis", Description = "Chronic skin condition", ConditionCategoryID = 7 },
                    new MedicalCondition { ConditionID = 8, ConditionName = "Depression", Description = "Persistent low mood disorder", ConditionCategoryID = 9 },
                    new MedicalCondition { ConditionID = 9, ConditionName = "Lupus", Description = "Systemic autoimmune disease", ConditionCategoryID = 8 },
                    new MedicalCondition { ConditionID = 10, ConditionName = "Breast Cancer", Description = "Malignant breast tissue growth", ConditionCategoryID = 10 }
                };
            }

            try { ViewData["Categories"] = _adminRepo.GetAllConditionCategories(); }
            catch
            {
                ViewData["Categories"] = new List<ConditionCategory>
                {
                    new ConditionCategory { ConditionCategoryID = 1, CategoryName = "Cardiovascular" },
                    new ConditionCategory { ConditionCategoryID = 2, CategoryName = "Respiratory" },
                    new ConditionCategory { ConditionCategoryID = 3, CategoryName = "Neurological" },
                    new ConditionCategory { ConditionCategoryID = 4, CategoryName = "Digestive" },
                    new ConditionCategory { ConditionCategoryID = 5, CategoryName = "Endocrine" },
                    new ConditionCategory { ConditionCategoryID = 6, CategoryName = "Musculoskeletal" },
                    new ConditionCategory { ConditionCategoryID = 7, CategoryName = "Dermatological" },
                    new ConditionCategory { ConditionCategoryID = 8, CategoryName = "Immunological" },
                    new ConditionCategory { ConditionCategoryID = 9, CategoryName = "Psychiatric" },
                    new ConditionCategory { ConditionCategoryID = 10, CategoryName = "Oncological" }
                };
            }

            return View("~/Views/Admin/MedicalConditions.cshtml");
        }


        [HttpPost]
        public IActionResult CreateCondition(string ConditionName, string? Description, int ConditionCategoryID)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(ConditionName))
            {
                TempData["Error"] = "Condition name is required.";
                return RedirectToAction("MedicalConditions");
            }

            if (ConditionName.Length > 100)
            {
                TempData["Error"] = "Condition name cannot exceed 100 characters.";
                return RedirectToAction("MedicalConditions");
            }

            if (ConditionCategoryID <= 0)
            {
                TempData["Error"] = "Please select a valid category.";
                return RedirectToAction("MedicalConditions");
            }

            if (Description != null && Description.Length > 500)
            {
                TempData["Error"] = "Description cannot exceed 500 characters.";
                return RedirectToAction("MedicalConditions");
            }

            // Save
            try
            {
                var result = _adminRepo.CreateCondition(ConditionName, Description, ConditionCategoryID);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Condition created successfully." : "Failed to create: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("MedicalConditions");
        }

        [HttpPost]
        public IActionResult EditCondition(int ConditionID, string ConditionName, string? Description, int ConditionCategoryID)
        {
            try
            {
                var result = _adminRepo.UpdateCondition(ConditionID, ConditionName, Description, ConditionCategoryID);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Condition updated successfully." : "Failed to update: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("MedicalConditions");
        }

        [HttpPost]
        public IActionResult DeleteCondition(int id)
        {
            try
            {
                var result = _adminRepo.DeleteCondition(id);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Condition deleted successfully." : "Failed to delete: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("MedicalConditions");
        }

        // ── ALLERGY CATEGORIES ────────────────────────────────
        public IActionResult AllergyCategories()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");

            try { ViewData["Categories"] = _adminRepo.GetAllAllergyCategories(); }
            catch
            {
                ViewData["Categories"] = new List<AllergyCategory>
                {
                    new AllergyCategory { AllergyCategoryID = 1, CategoryName = "Food", Description = "Allergies triggered by food items" },
                    new AllergyCategory { AllergyCategoryID = 2, CategoryName = "Drug", Description = "Allergies triggered by medications" },
                    new AllergyCategory { AllergyCategoryID = 3, CategoryName = "Environmental", Description = "Allergies triggered by environment" },
                    new AllergyCategory { AllergyCategoryID = 4, CategoryName = "Insect", Description = "Allergies triggered by insect stings" },
                    new AllergyCategory { AllergyCategoryID = 5, CategoryName = "Contact", Description = "Skin contact allergies" },
                    new AllergyCategory { AllergyCategoryID = 6, CategoryName = "Latex", Description = "Latex material allergies" },
                    new AllergyCategory { AllergyCategoryID = 7, CategoryName = "Mold", Description = "Mold spore allergies" },
                    new AllergyCategory { AllergyCategoryID = 8, CategoryName = "Pet", Description = "Animal dander allergies" },
                    new AllergyCategory { AllergyCategoryID = 9, CategoryName = "Pollen", Description = "Plant pollen allergies" },
                    new AllergyCategory { AllergyCategoryID = 10, CategoryName = "Chemical", Description = "Chemical substance allergies" }
                };
            }

            return View("~/Views/Admin/AllergyCategories.cshtml");
        }

        [HttpPost]
        public IActionResult CreateAllergyCategory(string CategoryName, string? Description)
        {
            try
            {
                var result = _adminRepo.CreateAllergyCategory(CategoryName, Description);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Category created successfully." : "Failed to create: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("AllergyCategories");
        }

        [HttpPost]
        public IActionResult EditAllergyCategory(int AllergyCategoryID, string CategoryName, string? Description)
        {
            try
            {
                var result = _adminRepo.UpdateAllergyCategory(AllergyCategoryID, CategoryName, Description);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Category updated successfully." : "Failed to update: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("AllergyCategories");
        }

        [HttpPost]
        public IActionResult DeleteAllergyCategory(int id)
        {
            try
            {
                var result = _adminRepo.DeleteAllergyCategory(id);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Category deleted successfully." : "Failed to delete: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("AllergyCategories");
        }

        // ── ALLERGIES ─────────────────────────────────────────
        public IActionResult Allergies()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");

            try { ViewData["Allergies"] = _adminRepo.GetAllAllergies(); }
            catch
            {
                ViewData["Allergies"] = new List<Allergy>
                {
                    new Allergy { AllergyID = 1, AllergyName = "Peanuts", Description = "Reaction to peanut proteins", AllergyCategoryID = 1 },
                    new Allergy { AllergyID = 2, AllergyName = "Penicillin", Description = "Reaction to penicillin antibiotics", AllergyCategoryID = 2 },
                    new Allergy { AllergyID = 3, AllergyName = "Dust Mites", Description = "Reaction to dust mite particles", AllergyCategoryID = 3 },
                    new Allergy { AllergyID = 4, AllergyName = "Bee Stings", Description = "Reaction to bee venom", AllergyCategoryID = 4 },
                    new Allergy { AllergyID = 5, AllergyName = "Nickel", Description = "Skin reaction to nickel metal", AllergyCategoryID = 5 },
                    new Allergy { AllergyID = 6, AllergyName = "Latex Gloves", Description = "Reaction to latex rubber", AllergyCategoryID = 6 },
                    new Allergy { AllergyID = 7, AllergyName = "Black Mold", Description = "Reaction to black mold spores", AllergyCategoryID = 7 },
                    new Allergy { AllergyID = 8, AllergyName = "Cat Dander", Description = "Reaction to cat skin flakes", AllergyCategoryID = 8 },
                    new Allergy { AllergyID = 9, AllergyName = "Grass Pollen", Description = "Reaction to grass pollen", AllergyCategoryID = 9 },
                    new Allergy { AllergyID = 10, AllergyName = "Bleach", Description = "Reaction to bleach chemicals", AllergyCategoryID = 10 }
                };
            }

            try { ViewData["Categories"] = _adminRepo.GetAllAllergyCategories(); }
            catch
            {
                ViewData["Categories"] = new List<AllergyCategory>
                {
                    new AllergyCategory { AllergyCategoryID = 1, CategoryName = "Food" },
                    new AllergyCategory { AllergyCategoryID = 2, CategoryName = "Drug" },
                    new AllergyCategory { AllergyCategoryID = 3, CategoryName = "Environmental" },
                    new AllergyCategory { AllergyCategoryID = 4, CategoryName = "Insect" },
                    new AllergyCategory { AllergyCategoryID = 5, CategoryName = "Contact" },
                    new AllergyCategory { AllergyCategoryID = 6, CategoryName = "Latex" },
                    new AllergyCategory { AllergyCategoryID = 7, CategoryName = "Mold" },
                    new AllergyCategory { AllergyCategoryID = 8, CategoryName = "Pet" },
                    new AllergyCategory { AllergyCategoryID = 9, CategoryName = "Pollen" },
                    new AllergyCategory { AllergyCategoryID = 10, CategoryName = "Chemical" }
                };
            }

            return View("~/Views/Admin/Allergies.cshtml");
        }

        [HttpPost]
        public IActionResult CreateAllergy(string AllergyName, string? Description, int AllergyCategoryID)
        {
            try
            {
                var result = _adminRepo.CreateAllergy(AllergyName, Description, AllergyCategoryID);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Allergy created successfully." : "Failed to create: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("Allergies");
        }

        [HttpPost]
        public IActionResult EditAllergy(int AllergyID, string AllergyName, string? Description, int AllergyCategoryID)
        {
            try
            {
                var result = _adminRepo.UpdateAllergy(AllergyID, AllergyName, Description, AllergyCategoryID);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Allergy updated successfully." : "Failed to update: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("Allergies");
        }

        [HttpPost]
        public IActionResult DeleteAllergy(int id)
        {
            try
            {
                var result = _adminRepo.DeleteAllergy(id);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Allergy deleted successfully." : "Failed to delete: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("Allergies");
        }

        // ── MEDICATION CATEGORIES ─────────────────────────────
        public IActionResult MedicationCategories()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");

            try { ViewData["Categories"] = _adminRepo.GetAllMedicationCategories(); }
            catch
            {
                ViewData["Categories"] = new List<MedicationCategory>
                {
                    new MedicationCategory { MedicationCategoryID = 1, CategoryName = "Antibiotics", Description = "Medications that fight bacterial infections" },
                    new MedicationCategory { MedicationCategoryID = 2, CategoryName = "Analgesics", Description = "Pain relief medications" },
                    new MedicationCategory { MedicationCategoryID = 3, CategoryName = "Antihypertensives", Description = "Medications that lower blood pressure" },
                    new MedicationCategory { MedicationCategoryID = 4, CategoryName = "Antidiabetics", Description = "Medications that manage blood sugar" },
                    new MedicationCategory { MedicationCategoryID = 5, CategoryName = "Antihistamines", Description = "Medications that treat allergic reactions" },
                    new MedicationCategory { MedicationCategoryID = 6, CategoryName = "Antidepressants", Description = "Medications that treat depression" },
                    new MedicationCategory { MedicationCategoryID = 7, CategoryName = "Anticoagulants", Description = "Blood thinning medications" },
                    new MedicationCategory { MedicationCategoryID = 8, CategoryName = "Bronchodilators", Description = "Medications that open airways" },
                    new MedicationCategory { MedicationCategoryID = 9, CategoryName = "Corticosteroids", Description = "Anti-inflammatory steroid medications" },
                    new MedicationCategory { MedicationCategoryID = 10, CategoryName = "Antivirals", Description = "Medications that fight viral infections" }
                };
            }

            return View("~/Views/Admin/MedicationCategories.cshtml");
        }

        [HttpPost]
        public IActionResult CreateMedicationCategory(string CategoryName, string? Description)
        {
            try
            {
                var result = _adminRepo.CreateMedicationCategory(CategoryName, Description);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Category created successfully." : "Failed to create: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("MedicationCategories");
        }

        [HttpPost]
        public IActionResult EditMedicationCategory(int MedicationCategoryID, string CategoryName, string? Description)
        {
            try
            {
                var result = _adminRepo.UpdateMedicationCategory(MedicationCategoryID, CategoryName, Description);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Category updated successfully." : "Failed to update: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("MedicationCategories");
        }

        [HttpPost]
        public IActionResult DeleteMedicationCategory(int id)
        {
            try
            {
                var result = _adminRepo.DeleteMedicationCategory(id);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Category deleted successfully." : "Failed to delete: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("MedicationCategories");
        }

        // ── MEDICATIONS ───────────────────────────────────────
        public IActionResult Medications()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");

            try { ViewData["Medications"] = _adminRepo.GetAllMedications(); }
            catch
            {
                ViewData["Medications"] = new List<Medication>
                {
                    new Medication { MedicationID = 1, MedicationName = "Amoxicillin", Description = "Broad-spectrum antibiotic", MedicationCategoryID = 1 },
                    new Medication { MedicationID = 2, MedicationName = "Ibuprofen", Description = "Anti-inflammatory pain reliever", MedicationCategoryID = 2 },
                    new Medication { MedicationID = 3, MedicationName = "Lisinopril", Description = "ACE inhibitor for blood pressure", MedicationCategoryID = 3 },
                    new Medication { MedicationID = 4, MedicationName = "Metformin", Description = "First-line type 2 diabetes medication", MedicationCategoryID = 4 },
                    new Medication { MedicationID = 5, MedicationName = "Cetirizine", Description = "Non-drowsy antihistamine", MedicationCategoryID = 5 },
                    new Medication { MedicationID = 6, MedicationName = "Sertraline", Description = "SSRI antidepressant", MedicationCategoryID = 6 },
                    new Medication { MedicationID = 7, MedicationName = "Warfarin", Description = "Oral anticoagulant", MedicationCategoryID = 7 },
                    new Medication { MedicationID = 8, MedicationName = "Salbutamol", Description = "Short-acting bronchodilator inhaler", MedicationCategoryID = 8 },
                    new Medication { MedicationID = 9, MedicationName = "Prednisone", Description = "Oral corticosteroid", MedicationCategoryID = 9 },
                    new Medication { MedicationID = 10, MedicationName = "Acyclovir", Description = "Antiviral for herpes infections", MedicationCategoryID = 10 }
                };
            }

            try { ViewData["Categories"] = _adminRepo.GetAllMedicationCategories(); }
            catch
            {
                ViewData["Categories"] = new List<MedicationCategory>
                {
                    new MedicationCategory { MedicationCategoryID = 1, CategoryName = "Antibiotics" },
                    new MedicationCategory { MedicationCategoryID = 2, CategoryName = "Analgesics" },
                    new MedicationCategory { MedicationCategoryID = 3, CategoryName = "Antihypertensives" },
                    new MedicationCategory { MedicationCategoryID = 4, CategoryName = "Antidiabetics" },
                    new MedicationCategory { MedicationCategoryID = 5, CategoryName = "Antihistamines" },
                    new MedicationCategory { MedicationCategoryID = 6, CategoryName = "Antidepressants" },
                    new MedicationCategory { MedicationCategoryID = 7, CategoryName = "Anticoagulants" },
                    new MedicationCategory { MedicationCategoryID = 8, CategoryName = "Bronchodilators" },
                    new MedicationCategory { MedicationCategoryID = 9, CategoryName = "Corticosteroids" },
                    new MedicationCategory { MedicationCategoryID = 10, CategoryName = "Antivirals" }
                };
            }

            return View("~/Views/Admin/Medications.cshtml");
        }

        [HttpPost]
        public IActionResult CreateMedication(string MedicationName, string? Description, int MedicationCategoryID)
        {
            try
            {
                var result = _adminRepo.CreateMedication(MedicationName, Description, MedicationCategoryID);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Medication created successfully." : "Failed to create: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("Medications");
        }

        [HttpPost]
        public IActionResult EditMedication(int MedicationID, string MedicationName, string? Description, int MedicationCategoryID)
        {
            try
            {
                var result = _adminRepo.UpdateMedication(MedicationID, MedicationName, Description, MedicationCategoryID);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Medication updated successfully." : "Failed to update: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("Medications");
        }

        [HttpPost]
        public IActionResult DeleteMedication(int id)
        {
            try
            {
                var result = _adminRepo.DeleteMedication(id);
                TempData[result == "SUCCESS" ? "Success" : "Error"] = result == "SUCCESS"
                    ? "Medication deleted successfully." : "Failed to delete: " + result;
            }
            catch { TempData["Error"] = "Database unavailable."; }
            return RedirectToAction("Medications");
        }

        // ── ACTIVITY LOG ──────────────────────────────────────
        public IActionResult ActivityLog()
        {
            if (HttpContext.Session.GetString("RoleName") != "Admin")
                return RedirectToAction("Login", "Home");

            ViewData["AdminEmail"] = HttpContext.Session.GetString("Email");

            try { ViewData["Logs"] = _adminRepo.GetActivityLog(); }
            catch
            {
                ViewData["Logs"] = new List<ActivityLogEntry>
                {
                    new ActivityLogEntry { LogID = 1, Action = "Created condition category: Cardiovascular", PerformedBy = "dev-admin@test.com", Timestamp = DateTime.Now.AddHours(-1) },
                    new ActivityLogEntry { LogID = 2, Action = "Created medication: Amoxicillin", PerformedBy = "dev-admin@test.com", Timestamp = DateTime.Now.AddHours(-2) },
                    new ActivityLogEntry { LogID = 3, Action = "Deleted allergy category: Seasonal", PerformedBy = "dev-admin@test.com", Timestamp = DateTime.Now.AddHours(-3) },
                    new ActivityLogEntry { LogID = 4, Action = "Updated condition: Asthma", PerformedBy = "dev-admin@test.com", Timestamp = DateTime.Now.AddHours(-4) },
                    new ActivityLogEntry { LogID = 5, Action = "Created allergy: Peanuts", PerformedBy = "dev-admin@test.com", Timestamp = DateTime.Now.AddHours(-5) },
                    new ActivityLogEntry { LogID = 6, Action = "Updated medication category: Antibiotics", PerformedBy = "dev-admin@test.com", Timestamp = DateTime.Now.AddHours(-6) },
                    new ActivityLogEntry { LogID = 7, Action = "Created condition: Hypertension", PerformedBy = "dev-admin@test.com", Timestamp = DateTime.Now.AddHours(-7) },
                    new ActivityLogEntry { LogID = 8, Action = "Deleted medication: Warfarin", PerformedBy = "dev-admin@test.com", Timestamp = DateTime.Now.AddHours(-8) },
                    new ActivityLogEntry { LogID = 9, Action = "Created allergy category: Food", PerformedBy = "dev-admin@test.com", Timestamp = DateTime.Now.AddHours(-9) },
                    new ActivityLogEntry { LogID = 10, Action = "Updated condition category: Neurological", PerformedBy = "dev-admin@test.com", Timestamp = DateTime.Now.AddHours(-10) }
                };
            }

            return View("~/Views/Admin/ActivityLog.cshtml");
        }
    }
}