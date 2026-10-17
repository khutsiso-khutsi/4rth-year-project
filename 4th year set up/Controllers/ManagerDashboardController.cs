
using LabManager.Models;
using LabManager.Repositories;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace _4th_year_set_up.Controllers
{
    public class ManagerDashboardController : Controller
    {
        // =========================================================
        // REPOSITORIES
        // =========================================================

        private readonly ITestCatalogueRepository _testCatalogueRepository;
        private readonly IConsumablesRepository _consumableRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly IOrderRepository _orderRepository;

        public ManagerDashboardController(
            ITestCatalogueRepository testCatalogueRepository,
            IConsumablesRepository consumableRepository,
            IStaffRepository staffRepository,
            IOrderRepository orderRepository)
        {
            _testCatalogueRepository = testCatalogueRepository;
            _consumableRepository = consumableRepository;
            _staffRepository = staffRepository;
            _orderRepository = orderRepository;
        }


        // =========================================================
        // SESSION
        // =========================================================

        private void SetSession()
        {
            ViewBag.Email = HttpContext.Session.GetString("Email") ?? "";
            ViewBag.DisplayName = HttpContext.Session.GetString("Username")
                ?? ViewBag.Email?.ToString()?.Split('@').FirstOrDefault()
                ?? "Manager";
        }


        // =========================================================
        // LABORATORY MANAGER DASHBOARD
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            SetSession();

            // -----------------------------------------------------
            // Load Test Catalogue
            // -----------------------------------------------------

            var categories =
                (await _testCatalogueRepository.GetAllCategories())
                .ToList();

            var testTypes =
                (await _testCatalogueRepository.GetAllTestTypes())
                .ToList();


            // -----------------------------------------------------
            // Load Consumables
            // -----------------------------------------------------

            var consumables =
                (await _consumableRepository.GetAllConsumables())
                .ToList();

            var suppliers =
                (await _consumableRepository.GetAllSuppliers())
                .ToList();


            // -----------------------------------------------------
            // Load Staff
            // -----------------------------------------------------

            var doctors =
                (await _staffRepository.GetAllDoctor())
                .ToList();

            var technicians =
                (await _staffRepository.GetTechnician())
                .ToList();


            // -----------------------------------------------------
            // Load Consumable Orders
            // -----------------------------------------------------

            var orders =
                (await _orderRepository.GetOrders() )
                .ToList();


            // =====================================================
            // STOCK ALERTS
            // =====================================================
            //
            // A consumable is considered:
            //
            // Below reorder:
            // QuantityOnHand <= ReorderLevel
            //
            // Near reorder:
            // QuantityOnHand <= ReorderLevel + 10%
            //
            // Example:
            //
            // Reorder level = 100
            // 10% = 10
            // Alert threshold = 110
            //
            // =====================================================

            var stockAlerts = consumables
                .Where(x =>
                    x.QuantityOnHand <=
                    Math.Ceiling(x.ReorderLevel * 1.10))
                .OrderBy(x => x.QuantityOnHand)
                .Take(5)
                .ToList();


            // =====================================================
            // OPEN ORDERS
            // =====================================================

            var openOrders = orders
                .Where(x => x.Status != null &&
                    (x.Status.Equals("Ordered", StringComparison.OrdinalIgnoreCase) ||
                     x.Status.Equals("Partially complete", StringComparison.OrdinalIgnoreCase)))
                .ToList();


            // =====================================================
            // DASHBOARD VIEW MODEL
            // =====================================================

            var model = new DashboardViewModel
            {
                // -------------------------------------------------
                // Test Catalogue
                // -------------------------------------------------

                CategoryCount = categories.Count,

                TestTypeCount = testTypes.Count,

                RecentTests = testTypes
                    .OrderBy(x => x.TestName)
                    .Take(5)
                    .ToList(),


                // -------------------------------------------------
                // Consumables
                // -------------------------------------------------

                ConsumableCount = consumables.Count,

                LowStockCount = stockAlerts.Count,

                StockAlerts = stockAlerts,


                // -------------------------------------------------
                // Suppliers
                // -------------------------------------------------

                SupplierCount = suppliers.Count,


                // -------------------------------------------------
                // Staff
                // -------------------------------------------------

                DoctorCount = doctors.Count,

                TechnicianCount = technicians.Count,

                Doctors = doctors,

                Technicians = technicians,


                // -------------------------------------------------
                // Orders
                // -------------------------------------------------

                OpenOrderCount = openOrders.Count,

                RecentOrders = orders
                    .OrderByDescending(x => x.OrderDate)
                    .Take(5)
                    .ToList()
            };


            // =====================================================
            // RETURN DASHBOARD
            // =====================================================

            return View(
                "~/Views/ManagerDashboard/Index.cshtml",
                model);
        }


        // =========================================================
        // REPORT
        // =========================================================

        //public IActionResult Report()
        //{
        //    return RedirectToAction(nameof(TestsByCategory));
        //}


        //// =========================================================
        //// TESTS BY CATEGORY REPORT
        //// =========================================================

        //[HttpGet]
        //public IActionResult TestsByCategory(
        //    DateTime? from,
        //    DateTime? to)
        //{
        //    SetSession();

        //    DateTime fromDate =
        //        from ?? new DateTime(2026, 8, 1);

        //    DateTime toDate =
        //        to ?? new DateTime(2026, 8, 30);


        //    var categories =
        //        new List<CategoryReportItemViewModel>
        //        {
        //            new CategoryReportItemViewModel
        //            {
        //                CategoryName = "Full Blood Count",
        //                TestCount = 120
        //            },

        //            new CategoryReportItemViewModel
        //            {
        //                CategoryName = "Coagulation",
        //                TestCount = 70
        //            },

        //            new CategoryReportItemViewModel
        //            {
        //                CategoryName = "Blood Chemistry",
        //                TestCount = 60
        //            }
        //        };


        //    int totalTests =
        //        categories.Sum(x => x.TestCount);


        //    foreach (var category in categories)
        //    {
        //        category.Percentage =
        //            totalTests > 0
        //                ? ((decimal)category.TestCount /
        //                   totalTests) * 100
        //                : 0;
        //    }


        //    var report =
        //        new TestsByCategoryReportViewModel
        //        {
        //            FromDate = fromDate,
        //            EndDate = toDate,
        //            TotalTests = totalTests,
        //            Categories = categories
        //        };


        //    return View(
        //        "~/Views/ManagerDashboard/TestsByCategory.cshtml",
        //        report);
        //}


        // =========================================================
        // AUDIT LOG
        // =========================================================

        private static readonly List<AuditLogEntry> _demoAuditLogs =
            new()
            {
                new AuditLogEntry
                {
                    Id = 1,
                    Timestamp =
                        DateTime.Today
                            .AddDays(-1)
                            .AddHours(9)
                            .AddMinutes(12),

                    UserEmail = "manager@nmbhdl.co.za",
                    UserRole = "Manager",
                    Action = AuditAction.Created,
                    Module = "Staff",
                    ReferenceId = "STF-014",
                    Details =
                        "Added new staff member Sipho Khumalo",
                    IpAddress = "10.0.0.14"
                },

                new AuditLogEntry
                {
                    Id = 2,
                    Timestamp =
                        DateTime.Today
                            .AddDays(-1)
                            .AddHours(11)
                            .AddMinutes(40),

                    UserEmail = "dev-doctor@test.com",
                    UserRole = "Doctor",
                    Action = AuditAction.Updated,
                    Module = "Test Request",
                    ReferenceId = "REQ-889",
                    Details =
                        "Marked Haemoglobin result as reviewed",
                    IpAddress = "10.0.0.22"
                },

                new AuditLogEntry
                {
                    Id = 3,
                    Timestamp =
                        DateTime.Today
                            .AddDays(-2)
                            .AddHours(8)
                            .AddMinutes(5),

                    UserEmail = "manager@nmbhdl.co.za",
                    UserRole = "Manager",
                    Action = AuditAction.Deleted,
                    Module = "Consumables",
                    ReferenceId = "CON-102",
                    Details =
                        "Removed expired reagent batch",
                    IpAddress = "10.0.0.14"
                },

                new AuditLogEntry
                {
                    Id = 4,
                    Timestamp =
                        DateTime.Today
                            .AddDays(-2)
                            .AddHours(16)
                            .AddMinutes(50),

                    UserEmail = "manager@nmbhdl.co.za",
                    UserRole = "Manager",
                    Action = AuditAction.Login,
                    Module = "Auth",
                    ReferenceId = "-",
                    Details = "Signed in",
                    IpAddress = "10.0.0.14"
                },

                new AuditLogEntry
                {
                    Id = 5,
                    Timestamp =
                        DateTime.Today
                            .AddDays(-3)
                            .AddHours(14)
                            .AddMinutes(3),

                    UserEmail = "dev-doctor@test.com",
                    UserRole = "Doctor",
                    Action = AuditAction.Approved,
                    Module = "Test Request",
                    ReferenceId = "REQ-890",
                    Details =
                        "Approved Prothrombin Time result",
                    IpAddress = "10.0.0.22"
                },

                new AuditLogEntry
                {
                    Id = 6,
                    Timestamp =
                        DateTime.Today
                            .AddDays(-4)
                            .AddHours(17)
                            .AddMinutes(30),

                    UserEmail = "manager@nmbhdl.co.za",
                    UserRole = "Manager",
                    Action = AuditAction.Logout,
                    Module = "Auth",
                    ReferenceId = "-",
                    Details = "Signed out",
                    IpAddress = "10.0.0.14"
                }
            };


        // =========================================================
        // AUDIT LOG PAGE
        // =========================================================

        [HttpGet]
        public IActionResult AuditLog(
            DateTime? from,
            DateTime? to,
            AuditAction? action)
        {
            SetSession();

            var toDate =
                to ?? DateTime.Today;

            var fromDate =
                from ?? toDate.AddDays(-5);


            var entries =
                _demoAuditLogs
                    .Where(a =>
                        a.Timestamp.Date >= fromDate.Date &&
                        a.Timestamp.Date <= toDate.Date)

                    .Where(a =>
                        !action.HasValue ||
                        a.Action == action.Value)

                    .OrderByDescending(a => a.Timestamp)

                    .ToList();


            var model =
                new AuditLogViewModel
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    ActionFilter = action,
                    Entries = entries
                };


            return View(
                "~/Views/ManagerDashboard/AuditLog.cshtml",
                model);
        }
    }
}




