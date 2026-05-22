using _4th_year_set_up.Models;
using Microsoft.AspNetCore.Mvc;

namespace _4th_year_set_up.Controllers
{
    public class ManagerDashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-manager@test.com";
            return View("~/Views/ManagerDashboard/Index.cshtml");
        }


        public IActionResult TestCatalogue()
        {
            List<TestCatalogue> tests = new List<TestCatalogue>()
            {
                new TestCatalogue
                {
                    TestId = 1,
                    TestName = "Full Blood Count",
                    Category = "Haematology",
                    SampleType = "Whole Blood",
                    Units = "Various",
                    NormalRange = "See panel",
                    TAT = 60
                },

                new TestCatalogue
                {
                    TestId = 2,
                    TestName = "Differential Count",
                    Category = "Haematology",
                    SampleType = "Whole Blood",
                    Units = "%",
                    NormalRange = "See panel",
                    TAT = 90
                },

                new TestCatalogue
                {
                    TestId = 3,
                    TestName = "Prothrombin Time",
                    Category = "Coagulation",
                    SampleType = "Plasma",
                    Units = "seconds",
                    NormalRange = "11–14 s",
                    TAT = 45
                },

                new TestCatalogue
                {
                    TestId = 4,
                    TestName = "Peripheral Blood Film",
                    Category = "Haematology",
                    SampleType = "Whole Blood",
                    Units = "Descriptive",
                    NormalRange = "Normal morphology",
                    TAT = 120
                }
            };

            return View("~/Views/ManagerDashboard/Test.cshtml", tests);

        }


      




        public IActionResult Consumables()
        {
            List<Consumable> consumables = new List<Consumable>()
            {
                new Consumable
                {
                    ConsumableId = 1,
                    ConsumableName = "EDTA Tubes",
                    Supplier = "MediPath SA",
                    OnHand = 8,
                    ReorderLevel = 50,
                    StockStatus = "Low"
                },

                new Consumable
                {
                    ConsumableId = 2,
                    ConsumableName = "Reagent Kit FBC",
                    Supplier = "LabSupply Co",
                    OnHand = 18,
                    ReorderLevel = 20,
                    StockStatus = "Medium"
                },

                new Consumable
                {
                    ConsumableId = 3,
                    ConsumableName = "Lancets (sterile)",
                    Supplier = "MediPath SA",
                    OnHand = 320,
                    ReorderLevel = 100,
                    StockStatus = "Good"
                },

                new Consumable
                {
                    ConsumableId = 4,
                    ConsumableName = "Coagulation Reagent",
                    Supplier = "Haema Diagnostics",
                    OnHand = 65,
                    ReorderLevel = 30,
                    StockStatus = "Good"
                }
            };

            return View("~/Views/ManagerDashboard/Consumables.cshtml", consumables);

        }

        public IActionResult Reports()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        public IActionResult AuditLog()
        {
            return View();
        }


    }
}

