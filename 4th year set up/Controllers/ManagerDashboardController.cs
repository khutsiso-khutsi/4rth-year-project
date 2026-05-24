using _4th_year_set_up.Models;
using LabManager.Models;
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

        public IActionResult Orders()
        {
            List<ConsumableOrder> orders = new List<ConsumableOrder>
            {
                new ConsumableOrder
                {
                    Id = 1,
                    OrderNumber = "ORD-2026-0041",
                    Supplier = "MediPath SA",
                    Items = "EDTA Tubes (200), Lancets (500)",
                    OrderDate = new DateTime(2026, 05, 15),
                    Status = "Ordered"
                },

                new ConsumableOrder
                {
                    Id = 2,
                    OrderNumber = "ORD-2026-0039",
                    Supplier = "LabSupply Co",
                    Items = "Reagent Kit FBC (50)",
                    OrderDate = new DateTime(2026, 05, 10),
                    Status = "Partially Complete"
                },

                new ConsumableOrder
                {
                    Id = 3,
                    OrderNumber = "ORD-2026-0035",
                    Supplier = "Haema Diagnostics",
                    Items = "Coagulation Reagent (100)",
                    OrderDate = new DateTime(2026, 04, 28),
                    Status = "Complete",
                    CompletedDate = new DateTime(2026, 05, 02)
                }
            };

            return View("~/Views/ManagerDashboard/Order.cshtml", orders);
        }

        public IActionResult Staff()
        {

            return View("~/Views/ManagerDashboard/Order.cshtml");
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

