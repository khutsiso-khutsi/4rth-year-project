using _4th_year_set_up.Models;
using LabManager.Models;
using Microsoft.AspNetCore.Mvc;
using Patient.Models;
using System.Numerics;

namespace _4th_year_set_up.Controllers
{
    public class ManagerDashboardController : Controller
    {
        // =========================================================
        // PROTOTYPE USER
        // =========================================================

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

        // =========================================================
        // TEST CATALOGUE DATA
        // =========================================================

        private static List<TestCatalogue> _tests = new()
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

        private static List<TestCategory> _categories = new()
        {
            new TestCategory
            {
                Id = 1,
                CategoryName = "Haematology",
                Description = "Blood cell tests"
            },

            new TestCategory
            {
                Id = 2,
                CategoryName = "Coagulation",
                Description = "Clotting tests"
            },

            new TestCategory
            {
                Id = 3,
                CategoryName = "Immunology",
                Description = "Immune tests"
            }
        };

        // =========================================================
        // CONSUMABLES DATA
        // =========================================================

        private static List<Consumable> _consumables = new()
        {
            new Consumable
            {
                Id = 1,
                ConsumableName = "EDTA Tubes",
                Supplier = "MediPath SA",
                OnHand = 8,
                ReorderLevel = 50,
                StockStatus = "Low"
            },

            new Consumable
            {
                Id = 2,
                ConsumableName = "Reagent Kit FBC",
                Supplier = "LabSupply Co",
                OnHand = 18,
                ReorderLevel = 20,
                StockStatus = "Medium"
            },

            new Consumable
            {
                Id = 3,
                ConsumableName = "Lancets (sterile)",
                Supplier = "MediPath SA",
                OnHand = 320,
                ReorderLevel = 100,
                StockStatus = "Good"
            },

            new Consumable
            {
                Id = 4,
                ConsumableName = "Coagulation Reagent",
                Supplier = "Haema Diagnostics",
                OnHand = 65,
                ReorderLevel = 30,
                StockStatus = "Good"
            }
        };

        private static List<Supplier> _suppliers = new()
        {
            new Supplier
            {
                Id = 1,
                SupplierName = "MediPath SA",
                Email = "orders@medipath.co.za"
            },

            new Supplier
            {
                Id = 2,
                SupplierName = "LabSupply Co",
                Email = "orders@labsupply.co.za"
            },

            new Supplier
            {
                Id = 3,
                SupplierName = "Haema Diagnostics",
                Email = "orders@haema.co.za"
            }
        };

        // =========================================================
        // ORDERS DATA
        // =========================================================

        private static List<ConsumableOrder> _orders = new()
        {
            new ConsumableOrder
            {
                Id = 1,
                OrderNumber = "ORD-2026-0041",
                Supplier = "MediPath SA",
                Items = "EDTA Tubes (200), Lancets (500)",
                OrderDate = new DateTime(2026, 5, 15),
                Status = "Ordered"
            },

            new ConsumableOrder
            {
                Id = 2,
                OrderNumber = "ORD-2026-0039",
                Supplier = "LabSupply Co",
                Items = "Reagent Kit FBC (50)",
                OrderDate = new DateTime(2026, 5, 10),
                Status = "Partially Complete"
            },

            new ConsumableOrder
            {
                Id = 3,
                OrderNumber = "ORD-2026-0035",
                Supplier = "Haema Diagnostics",
                Items = "Coagulation Reagent (100)",
                OrderDate = new DateTime(2026, 4, 28),
                Status = "Complete",
                CompletedDate = new DateTime(2026, 5, 2)
            }
        };

        // =========================================================
        // STAFF DATA
        // =========================================================

        private static List<Doctor> _doctors = new()
        {
            new Doctor
            {
                Id = 1,
                FullName = "Dr. Aisha Khan",
                Email = "aisha@nmbhdl.co.za",
                HpcsaNumber = "HP-7834521",
                IsActive = true
            },

            new Doctor
            {
                Id = 2,
                FullName = "Dr. Pieter de Wet",
                Email = "pieter@nmbhdl.co.za",
                HpcsaNumber = "HP-4512309",
                IsActive = true
            }
        };

        private static List<Technician> _technicians = new()
        {
            new Technician
            {
                Id = 1,
                FullName = "Khanya Nkosi",
                Email = "knkosi@lab.net",
                EmployeeNumber = "EMP-0042",
                TestTypes = new List<string> { "FBC", "Diff" }
            },

            new Technician
            {
                Id = 2,
                FullName = "Sipho Nkabinde",
                Email = "sipho@lab.net",
                EmployeeNumber = "EMP-0031",
                TestTypes = new List<string> { "Coag", "PBF" }
            }
        };

        // =========================================================
        // SESSION
        // =========================================================

        private void SetSession()
        {
            ViewBag.Email =
                HttpContext.Session.GetString("Email")
                ?? "manager@nmbhdl.co.za";
        }

        // =========================================================
        // DASHBOARD
        // =========================================================

        public IActionResult Index()
        {
            SetSession();
            return View("~/Views/ManagerDashboard/Index.cshtml");
        }

        // =========================================================
        // TEST CATALOGUE
        // =========================================================

        public IActionResult TestCatalogue()
        {
            SetSession();

            ViewBag.Categories = _categories;

            return View("~/Views/ManagerDashboard/Test.cshtml", _tests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCategory(
            string categoryName,
            string description)
        {
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                _categories.Add(new TestCategory
                {
                    Id = _categories.Count + 1,
                    CategoryName = categoryName,
                    Description = description ?? ""
                });
            }

            return RedirectToAction("TestCatalogue");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddTestType(
            string testName,
            string category,
            string sampleType,
            string units,
            string normalRangeMin,
            string normalRangeMax,
            int turnaround,
            string consumables)
        {
            if (!string.IsNullOrWhiteSpace(testName))
            {
                _tests.Add(new TestCatalogue
                {
                    TestId = _tests.Count + 1,
                    TestName = testName,
                    Category = category,
                    SampleType = sampleType,
                    Units = units,
                    NormalRange =
                        (!string.IsNullOrWhiteSpace(normalRangeMin) &&
                         !string.IsNullOrWhiteSpace(normalRangeMax))
                        ? $"{normalRangeMin}–{normalRangeMax}"
                        : "See panel",

                    TAT = turnaround,
                    Consumables = consumables ?? ""
                });
            }

            return RedirectToAction("TestCatalogue");
        }

        public IActionResult DeleteTest(int id)
        {
            var test = _tests.FirstOrDefault(x => x.TestId == id);

            if (test != null)
            {
                _tests.Remove(test);
            }

            return RedirectToAction("TestCatalogue");
        }

        // =========================================================
        // CONSUMABLES
        // =========================================================

        public IActionResult Consumables()
        {
            SetSession();

            ViewBag.Suppliers = _suppliers;

            return View(
                "~/Views/ManagerDashboard/Consumables.cshtml",
                _consumables);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddConsumable(
            string consumableName,
            int onHand,
            int reorderLevel,
            string supplier)
        {
            if (!string.IsNullOrWhiteSpace(consumableName))
            {
                _consumables.Add(new Consumable
                {
                    Id = _consumables.Count + 1,
                    ConsumableName = consumableName,
                    OnHand = onHand,
                    ReorderLevel = reorderLevel,
                    Supplier = supplier,
                    StockStatus =
                        onHand < 10 ? "Low"
                        : onHand < 30 ? "Medium"
                        : "Good"
                });
            }

            return RedirectToAction("Consumables");
        }

        // =========================================================
        // ORDERS
        // =========================================================

        public IActionResult Orders(
            string status = "All Statuses")
        {
            SetSession();

            ViewBag.StatusFilter = status;

            var list = status == "All Statuses"
                ? _orders
                : _orders.Where(x => x.Status == status).ToList();

            return View(
                "~/Views/ManagerDashboard/Order.cshtml",
                list);
        }

        // =========================================================
        // STAFF
        // =========================================================

        public IActionResult Staff()
        {
            SetSession();

            var model = new StaffViewModel
            {
                Doctors = _doctors,
                Technicians = _technicians
            };

            return View(
                "~/Views/ManagerDashboard/StaffManagement.cshtml",
                model);
        }

        // =========================================================
        // PROFILE
        // =========================================================

        public IActionResult Profile()
        {
            SetSession();

            return View(
                "~/Views/ManagerDashboard/Profile.cshtml",
                _user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProfile(ProfileViewModel model)
        {
            _user.FirstName = model.FirstName;
            _user.LastName = model.LastName;
            _user.DateOfBirth = model.DateOfBirth;
            _user.CellphoneNumber = model.CellphoneNumber;
            _user.HomeAddress = model.HomeAddress;

            ViewBag.Success = "Profile updated successfully.";

            return View(
                "~/Views/ManagerDashboard/Profile.cshtml",
                _user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ProfileViewModel model)
        {
            if (model.CurrentPassword != _user.CurrentPassword)
            {
                ViewBag.Error = "Current password is incorrect.";

                return View(
                    "~/Views/ManagerDashboard/Profile.cshtml",
                    _user);
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";

                return View(
                    "~/Views/ManagerDashboard/Profile.cshtml",
                    _user);
            }

            _user.CurrentPassword = model.NewPassword;

            ViewBag.Success = "Password updated successfully.";

            return View(
                "~/Views/ManagerDashboard/Profile.cshtml",
                _user);
        }

        // =========================================================
        // AUDIT LOG
        // =========================================================

        public IActionResult AuditLog()
        {
            SetSession();

            return View(
                "~/Views/ManagerDashboard/AuditLog.cshtml");
        }
    }
}