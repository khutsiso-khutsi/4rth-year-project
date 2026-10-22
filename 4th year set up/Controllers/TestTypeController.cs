using _4th_year_set_up.Models;
using LabManager.Models;
using LabManager.Repositories;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LabManager.Controllers
{
    public class TestCatalogueController : Controller
    {
        // Same rule as the client-side check in the view
        private static readonly Regex NamePattern =
            new(@"^[A-Za-z0-9 ()/.,&+\-]+$", RegexOptions.Compiled);

        private readonly ITestCatalogueRepository _repository;
        private readonly ICatalogueLookupRepository _lookups;
        private readonly LabManager.Repository.IOrderRepository _orderRepository;
        private readonly LabManager.Repository.IStaffRepository _staffRepository;

        public TestCatalogueController(
            ITestCatalogueRepository repository,
            ICatalogueLookupRepository lookups,
            LabManager.Repository.IOrderRepository orderRepository,
            LabManager.Repository.IStaffRepository staffRepository)
        {
            _repository = repository;
            _lookups = lookups;
            _orderRepository = orderRepository;
            _staffRepository = staffRepository;
        }

        // =========================================================
        // INDEX
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Categories + sample types come from the lookup repository.
            // The view reads ViewBag.Categories / ViewBag.SampleTypes (Manage modals)
            // and ViewBag.Category / ViewBag.SampleType (dropdowns).
            var categories = await _lookups.GetCategoriesAsync();
            var sampleTypes = await _lookups.GetSampleTypesAsync();

            ViewBag.Categories = categories;
            ViewBag.SampleTypes = sampleTypes;

            ViewBag.Category = categories
                .Select(c => new SelectListItem { Value = c.CategoryID.ToString(), Text = c.CategoryName })
                .ToList();

            ViewBag.SampleType = sampleTypes
                .Select(s => new SelectListItem { Value = s.SampleTypeId.ToString(), Text = s.SampleName })
                .ToList();

            IEnumerable<Consumable> consumables = await _repository.GetConsumables();
            ViewBag.Consumables = consumables.Select(c => new SelectListItem { Value = c.ConsumableID.ToString(), Text = c.ConsumableName });

            var tests = await _repository.GetAllTestTypes();

            // Provide a simple map of TestTypeId -> assigned technician name (first assigned)
            try
            {
                var technicians = await _staffRepository.GetTechnician();
                var techMap = technicians
                    .Where(t => t.TestTypeID > 0)
                    .GroupBy(t => t.TestTypeID)
                    .ToDictionary(g => g.Key, g => g.First().FullName);

                ViewBag.TechnicianMap = techMap;
            }
            catch
            {
                ViewBag.TechnicianMap = new Dictionary<int, string>();
            }

            ViewBag.Email = HttpContext.Session.GetString("Email");

            ViewBag.DeletedTests = await _repository.GetDeletedTestTypesAsync();
            return View(tests);
        }

        // =========================================================
        // ADD TEST TYPE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTestType(TestType test)
        {
            try
            {
                // parse decimal inputs explicitly to avoid missing values when model binder can't parse due to culture
                var minStr = Request.Form["NormalRangeMin"].FirstOrDefault();
                var maxStr = Request.Form["NormalRangeMax"].FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(minStr))
                {
                    if (decimal.TryParse(minStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var minVal) ||
                        decimal.TryParse(minStr, NumberStyles.Any, CultureInfo.CurrentCulture, out minVal))
                    {
                        test.NormalRangeMin = minVal;
                    }
                }

                if (!string.IsNullOrWhiteSpace(maxStr))
                {
                    if (decimal.TryParse(maxStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var maxVal) ||
                        decimal.TryParse(maxStr, NumberStyles.Any, CultureInfo.CurrentCulture, out maxVal))
                    {
                        test.NormalRangeMax = maxVal;
                    }
                }

                // if SampleTypeId posted as text, try parse to int
                var sampleStr = Request.Form["SampleTypeId"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(sampleStr) && int.TryParse(sampleStr, out var sampleId))
                {
                    test.SampleTypeId = sampleId;
                }

                if (string.IsNullOrWhiteSpace(test.TestName))
                {
                    TempData["Error"] = "Test name is required.";
                    return RedirectToAction(nameof(Index));
                }

                if (test.NormalRangeMin.HasValue &&
                    test.NormalRangeMax.HasValue &&
                    test.NormalRangeMin > test.NormalRangeMax)
                {
                    TempData["Error"] = "Normal range minimum cannot be greater than maximum.";
                    return RedirectToAction(nameof(Index));
                }

                // capture consumables JSON from form and assign so repository can persist it
                var consumablesJson = Request.Form["ConsumablesJson"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(consumablesJson))
                {
                    test.ConsumablesUsed = consumablesJson;
                }

                var result = await _repository.AddTestType(test);

                if (result)
                {
                    TempData["Success"] = "Test type added successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to add the test type.";
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while adding the test type.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT TEST TYPE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTestType(TestType test)
        {
            try
            {
                // prefer the posted TestId when present (model binder may miss it)
                var idStr = Request.Form["TestId"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(idStr) && int.TryParse(idStr, out var idVal) && idVal > 0)
                {
                    test.TestId = idVal;
                }

                // parse decimal inputs explicitly to avoid missing values when model binder can't parse due to culture
                var minStr = Request.Form["NormalRangeMin"].FirstOrDefault();
                var maxStr = Request.Form["NormalRangeMax"].FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(minStr))
                {
                    if (decimal.TryParse(minStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var minVal) ||
                        decimal.TryParse(minStr, NumberStyles.Any, CultureInfo.CurrentCulture, out minVal))
                    {
                        test.NormalRangeMin = minVal;
                    }
                }

                if (!string.IsNullOrWhiteSpace(maxStr))
                {
                    if (decimal.TryParse(maxStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var maxVal) ||
                        decimal.TryParse(maxStr, NumberStyles.Any, CultureInfo.CurrentCulture, out maxVal))
                    {
                        test.NormalRangeMax = maxVal;
                    }
                }

                // if SampleTypeId posted as text, try parse to int
                var sampleStr = Request.Form["SampleTypeId"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(sampleStr) && int.TryParse(sampleStr, out var sampleId))
                {
                    test.SampleTypeId = sampleId;
                }

                // the Edit modal now posts the consumable rows as JSON (same as Add)
                var consumablesJson = Request.Form["ConsumablesJson"].FirstOrDefault();
                if (consumablesJson != null)
                {
                    test.ConsumablesUsed = consumablesJson;
                }

                if (test.TestId <= 0)
                {
                    TempData["Error"] = "Invalid test type.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(test.TestName))
                {
                    TempData["Error"] = "Test name is required.";
                    return RedirectToAction(nameof(Index));
                }

                if (test.NormalRangeMin.HasValue &&
                    test.NormalRangeMax.HasValue &&
                    test.NormalRangeMin > test.NormalRangeMax)
                {
                    TempData["Error"] = "Normal range minimum cannot be greater than maximum.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _repository.UpdateTestType(test);

                if (result)
                {
                    TempData["Success"] = "Test type updated successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to update the test type.";
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while updating the test type.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE / RESTORE TEST TYPE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTest(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid test type.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _repository.DeleteTestTypeAsync(id);
                TempData["Success"] = "Test type deleted. You can restore it later.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Unable to delete the test type.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreTest(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid test type.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _repository.RestoreTestTypeAsync(id);
                TempData["Success"] = "Test type restored successfully.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Unable to restore the test type.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // CATEGORIES (Manage Categories modal)
        // =========================================================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string categoryName, string? description)
        {
            if (!TryCleanName(categoryName, "Category name", out var name, out var error) ||
                !TryCleanDescription(description, out var desc, out error))
                return Fail(error!);

            var (result, _) = await _lookups.AddCategoryAsync(name, desc);
            return Report(result, $"Category \"{name}\" added.", $"A category named \"{name}\" already exists.");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int categoryId, string categoryName, string? description)
        {
            if (categoryId <= 0) return Fail("Invalid category.");
            if (!TryCleanName(categoryName, "Category name", out var name, out var error) ||
                !TryCleanDescription(description, out var desc, out error))
                return Fail(error!);

            var result = await _lookups.UpdateCategoryAsync(categoryId, name, desc);
            return Report(result, $"Category \"{name}\" updated.", $"A category named \"{name}\" already exists.");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (id <= 0) return Fail("Invalid category.");

            var result = await _lookups.DeleteCategoryAsync(id);
            return Report(result, "Category deleted.", null,
                inUseMessage: "This category is used by one or more test types. Reassign or delete those tests first.");
        }

        // =========================================================
        // SAMPLE TYPES (Manage Sample Types modal)
        // =========================================================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSampleType(string sampleName)
        {
            if (!TryCleanName(sampleName, "Sample type name", out var name, out var error))
                return Fail(error!);

            var (result, _) = await _lookups.AddSampleTypeAsync(name);
            return Report(result, $"Sample type \"{name}\" added.", $"A sample type named \"{name}\" already exists.");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSampleType(int sampleTypeId, string sampleName)
        {
            if (sampleTypeId <= 0) return Fail("Invalid sample type.");
            if (!TryCleanName(sampleName, "Sample type name", out var name, out var error))
                return Fail(error!);

            var result = await _lookups.UpdateSampleTypeAsync(sampleTypeId, name);
            return Report(result, $"Sample type \"{name}\" updated.", $"A sample type named \"{name}\" already exists.");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSampleType(int id)
        {
            if (id <= 0) return Fail("Invalid sample type.");

            var result = await _lookups.DeleteSampleTypeAsync(id);
            return Report(result, "Sample type deleted.", null,
                inUseMessage: "This sample type is used by one or more test types. Reassign or delete those tests first.");
        }

        // ───────────── helpers ─────────────

        private IActionResult Fail(string message)
        {
            TempData["Error"] = message;
            return RedirectToAction(nameof(Index));
        }

        private IActionResult Report(CatalogueResult result, string success, string? duplicate, string? inUseMessage = null)
        {
            switch (result)
            {
                case CatalogueResult.Success: TempData["Success"] = success; break;
                case CatalogueResult.Duplicate: TempData["Error"] = duplicate ?? "That name already exists."; break;
                case CatalogueResult.NotFound: TempData["Error"] = "The item no longer exists. Refresh and try again."; break;
                case CatalogueResult.InUse: TempData["Error"] = inUseMessage ?? "The item is in use."; break;
                default: TempData["Error"] = "Something went wrong. Please try again."; break;
            }
            return RedirectToAction(nameof(Index));
        }

        // Mirrors the client-side rules in the view (2-50 chars, allowed characters)
        private static bool TryCleanName(string? raw, string label, out string name, out string? error)
        {
            name = Regex.Replace((raw ?? "").Trim(), @"\s+", " ");
            error = null;

            if (name.Length == 0) error = $"{label} is required.";
            else if (name.Length < 2 || name.Length > 50) error = $"{label} must be 2-50 characters.";
            else if (!NamePattern.IsMatch(name)) error = $"{label} contains invalid characters.";

            return error == null;
        }

        private static bool TryCleanDescription(string? raw, out string? description, out string? error)
        {
            description = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
            error = description?.Length > 200 ? "Description must be at most 200 characters." : null;
            return error == null;
        }
    }
}
