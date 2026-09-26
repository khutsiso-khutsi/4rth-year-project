using LabManager.Models;
using LabManager.Repositories;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LabManager.Controllers
{
    public class TestCatalogueController : Controller
    {
        private readonly ITestCatalogueRepository _repository;

        public TestCatalogueController(
            ITestCatalogueRepository repository)
        {
            _repository = repository;
        }

        // =========================================================
        // INDEX
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            

                IEnumerable<TestCategory> category = await _repository.GetCategory();
                ViewBag.Category = category.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.CategoryName });

                IEnumerable<TestType> sample = await _repository.GetSampleType();
                ViewBag.SampleType = sample.Select(c => new SelectListItem { Value = c.SampleTypeId.ToString(), Text = c.SampleName });

                var tests = await _repository.GetAllTestTypes();

                ViewBag.Email = HttpContext.Session.GetString("Email");

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
        public async Task<IActionResult> DeleteTest(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] =
                        "Invalid test type.";

                    return RedirectToAction(nameof(Index));
                }

                var result =
                    await _repository.DeleteTestType(id);

                if (result)
                {
                    TempData["Success"] =
                        "Test type deleted successfully.";
                }
                else
                {
                    TempData["Error"] =
                        "Unable to delete the test type.";
                }
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "An error occurred while deleting the test type.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DROPDOWNS
        // =========================================================
        //private async Task LoadDropdowns()
        //{
        //    var categories =
        //        await _repository.GetAllCategories();

        //    var sampleTypes =
        //        await _repository.GetAllSampleTypes();

        //    ViewBag.Category = categories.Select(x =>
        //        new SelectListItem
        //        {
        //            Value = x.CategoryId.ToString(),
        //            Text = x.CategoryName
        //        }).ToList();

        //    ViewBag.SampleType = sampleTypes.Select(x =>
        //        new SelectListItem
        //        {
        //            Value = x.SampleName,
        //            Text = x.SampleName
        //        }).ToList();

        //    ViewBag.Categories = categories;
        //}
    }
}