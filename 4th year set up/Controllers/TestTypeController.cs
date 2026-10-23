using _4th_year_set_up.Models;
using LabManager.Models;
using LabManager.Repositories;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

namespace LabManager.Controllers
{
    public class TestCatalogueController : Controller
    {
        private readonly ITestCatalogueRepository _repository;
        private readonly LabManager.Repository.IOrderRepository _orderRepository;
        private readonly LabManager.Repository.IStaffRepository _staffRepository;

        public TestCatalogueController(
            ITestCatalogueRepository repository,
            LabManager.Repository.IOrderRepository orderRepository,
            LabManager.Repository.IStaffRepository staffRepository)
        {
            _repository = repository;
            _orderRepository = orderRepository;
            _staffRepository = staffRepository;
        }

        // =========================================================
        // INDEX
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {


            IEnumerable<TestCategory> category = await _repository.GetCategory();
            ViewBag.Category = category.Select(c => new SelectListItem { Value = c.CategoryID.ToString(), Text = c.CategoryName });

            IEnumerable<TestType> sample = await _repository.GetSampleType();
            // expose raw sample types for views that expect them
            ViewBag.SampleTypes = sample;
            // build a SelectListItem list for dropdowns; ensure Text falls back to the id when name is missing
            ViewBag.SampleType = sample
                .Select(c => new SelectListItem { Value = c.SampleTypeId.ToString(), Text = c.SampleName ?? c.SampleTypeId.ToString() })
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

                // if SampleTypeId posted as text (e.g. from legacy hardcoded options), try parse to int
                var sampleStr = Request.Form["SampleTypeId"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(sampleStr) && int.TryParse(sampleStr, out var sampleId))
                {
                    test.SampleTypeId = sampleId;
                }
                if (string.IsNullOrWhiteSpace(test.TestName))
                {
                    TempData["Error"] =
                        "Test name is required.";

                    return RedirectToAction(nameof(Index));
                }

                //if (!test.CategoryId.HasValue)
                //{
                //    TempData["Error"] =
                //        "Please select a category.";

                //    return RedirectToAction(nameof(Index));
                //}

                //if (string.IsNullOrWhiteSpace(test.SampleTypeId))
                //{
                //    TempData["Error"] =
                //        "Please select a sample type.";

                //    return RedirectToAction(nameof(Index));
                //}

                if (test.NormalRangeMin.HasValue &&
                    test.NormalRangeMax.HasValue &&
                    test.NormalRangeMin > test.NormalRangeMax)
                {
                    TempData["Error"] =
                        "Normal range minimum cannot be greater than maximum.";

                    return RedirectToAction(nameof(Index));
                }

                // capture consumables JSON from form and assign so repository can persist it
                var consumablesJson = Request.Form["ConsumablesJson"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(consumablesJson))
                {
                    test.ConsumablesUsed = consumablesJson;
                }

                var result =
                    await _repository.AddTestType(test);

                if (result)
                {
                    TempData["Success"] =
                        "Test type added successfully.";
                }
                else
                {
                    TempData["Error"] =
                        "Unable to add the test type.";
                }
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "An error occurred while adding the test type.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ADD CATEGORY
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(
            TestCategory category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category.CategoryName))
                {
                    TempData["Error"] =
                        "Category name is required.";

                    return RedirectToAction(nameof(Index));
                }

                var result =
                    await _repository.AddCategory(category);

                if (result)
                {
                    TempData["Success"] =
                        "Category added successfully.";
                }
                else
                {
                    TempData["Error"] =
                        "Unable to add the category.";
                }
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "An error occurred while adding the category.";
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
                // ensure we pick up values that the model binder may have missed (e.g. TestId, numeric fields)
                var idStr = Request.Form["TestId"].FirstOrDefault();
                // prefer the posted TestId when present (model binder may miss it)
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

                if (test.TestId <= 0)
                {
                    TempData["Error"] =
                        "Invalid test type.";

                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(test.TestName))
                {
                    TempData["Error"] =
                        "Test name is required.";

                    return RedirectToAction(nameof(Index));
                }



                if (test.NormalRangeMin.HasValue &&
                    test.NormalRangeMax.HasValue &&
                    test.NormalRangeMin > test.NormalRangeMax)
                {
                    TempData["Error"] =
                        "Normal range minimum cannot be greater than maximum.";

                    return RedirectToAction(nameof(Index));
                }

                var result =
                    await _repository.UpdateTestType(test);

                if (result)
                {
                    TempData["Success"] =
                        "Test type updated successfully.";
                }
                else
                {
                    TempData["Error"] =
                        "Unable to update the test type.";
                }
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "An error occurred while updating the test type.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE TEST TYPE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSampleType(SampleType sampleType)
        {
            if (string.IsNullOrWhiteSpace(sampleType.SampleName))
            {
                TempData["Error"] = "Sample type name is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _repository.AddSampleTypeAsync(sampleType);
                TempData["Success"] = "Sample type added successfully.";
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "Unable to add sample type. Check whether it already exists.";
            }

            return RedirectToAction(nameof(Index));
        }

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
                TempData["Success"] =
                    "Test type deleted. You can restore it later.";
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(int categoryId, string categoryName, string? description)
        {
            if (categoryId <= 0 || string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Category name is required.";
                return RedirectToAction("Index");
            }

            int result = await _repository.UpdateCategoryAsync(categoryId, categoryName.Trim(), description);

            TempData[result == 0 ? "Success" : "Error"] = result switch
            {
                0 => "Category updated.",
                1 => "A category with that name already exists.",
                2 => "That category no longer exists.",
                _ => "Unable to update the category. Please try again."
            };

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            int result = await _repository.DeleteCategoryAsync(categoryId);

            TempData[result == 0 ? "Success" : "Error"] = result switch
            {
                0 => "Category deleted.",
                2 => "That category no longer exists.",
                3 => "This category still has tests assigned. Reassign or remove them first.",
                _ => "Unable to delete the category. Please try again."
            };

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSampleType(int sampleTypeId, string sampleName)
        {
            if (sampleTypeId <= 0 || string.IsNullOrWhiteSpace(sampleName))
            {
                TempData["Error"] = "Sample type name is required.";
                return RedirectToAction("Index");
            }

            int result = await _repository.UpdateSampleTypeAsync(sampleTypeId, sampleName.Trim());

            TempData[result == 0 ? "Success" : "Error"] = result switch
            {
                0 => "Sample type updated.",
                1 => "A sample type with that name already exists.",
                2 => "That sample type no longer exists.",
                _ => "Unable to update the sample type. Please try again."
            };

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSampleType(int sampleTypeId)
        {
            int result = await _repository.DeleteSampleTypeAsync(sampleTypeId);

            TempData[result == 0 ? "Success" : "Error"] = result switch
            {
                0 => "Sample type deleted.",
                2 => "That sample type no longer exists.",
                3 => "This sample type is still used by tests. Reassign or remove them first.",
                _ => "Unable to delete the sample type. Please try again."
            };

            return RedirectToAction("Index");
        }
    }
}