using _4th_year_set_up.Models;
using LabManager.Models;
using Microsoft.AspNetCore.Mvc;
using Patient.Models;

namespace _4th_year_set_up.Controllers
{
 

    namespace LabManager.Controllers
    {
        public class ManagerDashboardController : Controller
        {
            // =========================
            // PROTOTYPE USER (GLOBAL)
            // =========================
            private static UserProfileViewModel _user = new UserProfileViewModel
            {
                Id = "1",
                FirstName = "Lab",
                LastName = "Manager",
                Email = "manager@lab.local",
                IDNumber = "9001010001088",
                RegistrationDate = DateTime.Now.AddYears(-1),
                DateOfBirth = new DateTime(1995, 1, 1),
                CellphoneNumber = "0812345678",
                HomeAddress = "Port Elizabeth",
                Role = "Manager",
                CurrentPassword = "1234"
            };


            // =========================
            // DASHBOARD
            // =========================
            public IActionResult Index()
            {
                ViewBag.Email = HttpContext.Session.GetString("Email") ?? "dev-manager@test.com";
                return View("~/Views/ManagerDashboard/Index.cshtml");
            }

            // =========================
            // TEST CATALOGUE
            // =========================
            public IActionResult TestCatalogue()
            {
                List<TestCatalogue> tests = new List<TestCatalogue>
            {
                new TestCatalogue { TestId = 1, TestName = "Full Blood Count", Category = "Haematology", SampleType = "Whole Blood", Units = "Various", NormalRange = "See panel", TAT = 60 },
                new TestCatalogue { TestId = 2, TestName = "Differential Count", Category = "Haematology", SampleType = "Whole Blood", Units = "%", NormalRange = "See panel", TAT = 90 },
                new TestCatalogue { TestId = 3, TestName = "Prothrombin Time", Category = "Coagulation", SampleType = "Plasma", Units = "seconds", NormalRange = "11–14 s", TAT = 45 },
                new TestCatalogue { TestId = 4, TestName = "Peripheral Blood Film", Category = "Haematology", SampleType = "Whole Blood", Units = "Descriptive", NormalRange = "Normal morphology", TAT = 120 }
            };

                return View("~/Views/ManagerDashboard/Test.cshtml", tests);
            }

            // =========================
            // CONSUMABLES
            // =========================
            public IActionResult Consumables()
            {
                List<Consumable> consumables = new List<Consumable>
            {
                new Consumable { ConsumableId = 1, ConsumableName = "EDTA Tubes", Supplier = "MediPath SA", OnHand = 8, ReorderLevel = 50, StockStatus = "Low" },
                new Consumable { ConsumableId = 2, ConsumableName = "Reagent Kit FBC", Supplier = "LabSupply Co", OnHand = 18, ReorderLevel = 20, StockStatus = "Medium" },
                new Consumable { ConsumableId = 3, ConsumableName = "Lancets (sterile)", Supplier = "MediPath SA", OnHand = 320, ReorderLevel = 100, StockStatus = "Good" },
                new Consumable { ConsumableId = 4, ConsumableName = "Coagulation Reagent", Supplier = "Haema Diagnostics", OnHand = 65, ReorderLevel = 30, StockStatus = "Good" }
            };

                return View("~/Views/ManagerDashboard/Consumables.cshtml", consumables);
            }

            // =========================
            // ORDERS
            // =========================
            public IActionResult Orders()
            {
                List<ConsumableOrder> orders = new List<ConsumableOrder>
            {
                new ConsumableOrder { Id = 1, OrderNumber = "ORD-2026-0041", Supplier = "MediPath SA", Items = "EDTA Tubes (200), Lancets (500)", OrderDate = new DateTime(2026, 05, 15), Status = "Ordered" },
                new ConsumableOrder { Id = 2, OrderNumber = "ORD-2026-0039", Supplier = "LabSupply Co", Items = "Reagent Kit FBC (50)", OrderDate = new DateTime(2026, 05, 10), Status = "Partially Complete" },
                new ConsumableOrder { Id = 3, OrderNumber = "ORD-2026-0035", Supplier = "Haema Diagnostics", Items = "Coagulation Reagent (100)", OrderDate = new DateTime(2026, 04, 28), Status = "Complete", CompletedDate = new DateTime(2026, 05, 02) }
            };

                return View("~/Views/ManagerDashboard/Order.cshtml", orders);
            }

            // =========================
            // STAFF
            // =========================
            public IActionResult Staff()
            {
                List<Staff> staffList = new List<Staff>
            {
                new Staff { Id = 1, FullName = "Dr. Sarah Johnson", Role = "Doctor", HPCSANumber = "MP12345", Status = "Active" },
                new Staff { Id = 2, FullName = "Dr. Michael Brown", Role = "Doctor", HPCSANumber = "MP67890", Status = "Inactive" },
                new Staff { Id = 3, FullName = "John Williams", Role = "Technician", EmployeeNumber = "EMP001", TestTypes = "FBC, CBC" },
                new Staff { Id = 4, FullName = "Emily Smith", Role = "Technician", EmployeeNumber = "EMP002", TestTypes = "Coagulation" }
            };

                return View("~/Views/ManagerDashboard/StaffManagement.cshtml", staffList);
            }

            // =========================
            // PROFILE (VIEW)
            // =========================
            public IActionResult Profile()
            {
                return View("~/Views/ManagerDashboard/Profile.cshtml", _user);
            }


            public IActionResult UpdateProfile(ProfileViewModel model)
            {
                _user.FirstName = model.FirstName;
                _user.LastName = model.LastName;
                _user.DateOfBirth = model.DateOfBirth;
                _user.CellphoneNumber = model.CellphoneNumber;
                _user.HomeAddress = model.HomeAddress;

                ViewBag.Email = _user.Email;
                ViewBag.Success = "Profile updated successfully (prototype).";

                return View("~/Views/ManagerDashboard/Profile.cshtml", _user);
            }

            // =========================
            // PROFILE UPDATE PASSWORD
            // =========================
            [HttpPost]
            public IActionResult ChangePassword(ProfileViewModel model)
            {
                ViewBag.Email = _user.Email;

                if (model.CurrentPassword != _user.CurrentPassword)
                {
                    ViewBag.Error = "Current password is incorrect.";
                    return View("~/Views/ManagerDashboard/Profile.cshtml", _user);
                }

                if (model.NewPassword != model.ConfirmPassword)
                {
                    ViewBag.Error = "Passwords do not match.";
                    return View("~/Views/ManagerDashboard/Profile.cshtml", _user);
                }

                _user.CurrentPassword = model.NewPassword;

                ViewBag.Success = "Password updated successfully (prototype).";
                return View("~/Views/ManagerDashboard/Profile.cshtml", _user);
            }


            // AUDIT LOG

            public IActionResult AuditLog()
            {
                return View();
            }
        }
    }
}

