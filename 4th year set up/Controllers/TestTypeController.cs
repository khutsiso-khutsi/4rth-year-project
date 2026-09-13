using LabManager.Models;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LabManager.Controllers
{
    public class TestCatalogueController : Controller
    {
        private readonly ITestCategoryrepository _repository;
        private readonly ILogger<TestCatalogueController> _logger;

        public TestCatalogueController(
            ITestCategoryrepository repository,
            ILogger<TestCatalogueController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // =========================================================
        // TEST CATALOGUE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            IEnumerable<TestType> testTypes = Enumerable.Empty<TestType>();
            IEnumerable<TestCategory> categories = Enumerable.Empty<TestCategory>();


            IEnumerable<TestCategory> category= await _repository.GetCategory();
            ViewBag.Category = category.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.CategoryName });

            IEnumerable<TestType> sample  = await _repository.GetSampleType();
            ViewBag.SampleType = sample .Select(c => new SelectListItem { Value = c.SampleTypeId.ToString(), Text = c.SampleName });



            try
            {
                testTypes = await _repository.GetAllTestTypes();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load test types.");
                TempData["Error"] = "Unable to load the test catalogue.";
            }

            try
            {
                categories = await _repository.GetAllCategory();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load test categories.");
                TempData["Error"] = "Unable to load test categories.";
            }

            ViewBag.Categories = categories;
            ViewBag.Email = User.Identity?.Name;

            return View(testTypes);
        }

        // =========================================================
        // ADD TEST CATEGORY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(TestCategory category)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please provide valid category information.";
                return RedirectToAction(nameof(Index));
            }

            bool result;

            try
            {
                result = await _repository.AddTestCategory(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add test category.");
                result = false;
            }

            TempData[result ? "Success" : "Error"] = result
                ? "Test category added successfully."
                : "Unable to add test category.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ADD TEST TYPE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTestType(TestType test)
        {
            IEnumerable<TestCategory> category = await _repository.GetCategory();
            ViewBag.Category = category.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.CategoryName });

            IEnumerable<TestType> sample = await _repository.GetSampleType();
            ViewBag.SampleType = sample.Select(c => new SelectListItem { Value = c.SampleTypeId.ToString(), Text = c.SampleName });


            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please provide valid test information.";
                return RedirectToAction(nameof(Index));
            }

            bool result;

            try
            {
                result = await _repository.AddTestType(test);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add test type.");
                result = false;
            }

            TempData[result ? "Success" : "Error"] = result
                ? "Test type added successfully."
                : "Unable to add test type.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT TEST TYPE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> EditTestType(int id)
        {

            IEnumerable<TestCategory> category = await _repository.GetCategory();
            ViewBag.Category = category.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.CategoryName });

            IEnumerable<TestType> sample = await _repository.GetSampleType();
            ViewBag.SampleType = sample.Select(c => new SelectListItem { Value = c.SampleTypeId.ToString(), Text = c.SampleName });

            TestType? test;

            try
            {
                test = await _repository.GetTestType(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load test type {TestId}.", id);
                TempData["Error"] = "Unable to load the test type.";
                return RedirectToAction(nameof(Index));
            }

            if (test == null)
            {
                TempData["Error"] = "Test type was not found.";
                return RedirectToAction(nameof(Index));
            }

            IEnumerable<TestCategory> categories = Enumerable.Empty<TestCategory>();

            try
            {
                categories = await _repository.GetAllCategory();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load test categories.");
            }

            ViewBag.Categories = categories;

            return View(test);
        }

        // =========================================================
        // EDIT TEST TYPE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTestType(TestType test)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please provide valid test information.";
                return RedirectToAction(nameof(Index));
            }

            bool result;

            try
            {
                result = await _repository.EditTestType(test);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update test type {TestId}.", test.TestId);
                result = false;
            }

            TempData[result ? "Success" : "Error"] = result
                ? "Test type updated successfully."
                : "Unable to update test type.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE TEST TYPE
        // Must be POST: a GET-triggered delete lets a link, image tag,
        // or prefetch trigger destructive action, and bypasses the
        // antiforgery token entirely.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTest(int id)
        {
            bool result;

            try
            {
                result = await _repository.DeleteTestType(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete test type {TestId}.", id);
                result = false;
            }

            TempData[result ? "Success" : "Error"] = result
                ? "Test type deleted successfully."
                : "Unable to delete test type.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE CATEGORY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            bool result;

            try
            {
                result = await _repository.DeleteCategory(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete test category {CategoryId}.", id);
                result = false;
            }

            TempData[result ? "Success" : "Error"] = result
                ? "Test category deleted successfully."
                : "Unable to delete test category.";

            return RedirectToAction(nameof(Index));
        }
    }
}
