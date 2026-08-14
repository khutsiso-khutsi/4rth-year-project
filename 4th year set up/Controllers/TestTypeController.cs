using LabManager.Models;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;

namespace _4th_year_set_up.Controllers
{
    public class TestTypeController : Controller
    {

        private readonly ITestCategoryrepository _test;

        public TestTypeController(ITestCategoryrepository test)
        {
            _test = test;
        }


        public async Task<IActionResult> AddTestType(TestType test)
        {

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(test);
                }

                bool addTestType = await _test.AddTestType(test);

                if (addTestType)
                {

                }
                else
                {

                }
            }
            catch
            {




            }

            return View(test);
        }


        public async Task<IActionResult> AddCategory(TestCategory category) {




            try
            {
                if (!ModelState.IsValid)
                {
                    return View(category);
                }

                bool addTestType = await _test.AddTestCategory(category);

                if (addTestType)
                {

                }
                else
                {

                }
            }
            catch
            {




            }

            return View(category);
        }

    

        public async Task<IActionResult> Edit(int id) {


            var result =await _test.GetTestType(id);

            return View(result);    






        }

        [HttpPost]

        public async Task<IActionResult> Edit(TestType type)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(type);
                }

                bool updateTestType = await _test.Edit(type);

                if (updateTestType)
                {

                    TempData["msg"] = "";

                }
                else
                {
                    TempData["msg"] = "";
                }
            }
            catch (Exception ex) {


                TempData["msg"] = "";

            }

            return View(type);

        }

        public async Task<IActionResult> Delete(int id) {
        

            var result = await _test.DeleteCategory(id);
            return RedirectToAction(nameof(DisplayAll));

        
        }

        public async Task<IActionResult> DisplayAll()
        {
            var result = await _test.GetAllCategory();
            return View(result);    
        }




    }
    
}
