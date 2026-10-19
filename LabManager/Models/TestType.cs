using _4th_year_set_up.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Models
{
    public class TestType
    {
        public int TestId { get; set; }

        public string? TestName { get; set; }

        public int CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public int SampleTypeId { get; set; }

        public string? SampleName { get; set; }
        public string? UnitMeasurement { get; set; }

        public decimal? NormalRangeMin { get; set; }

        public decimal? NormalRangeMax { get; set; }

        public int? TurnaroundTime { get; set; }

        public string? ConsumablesUsed { get; set; }

        //Convenience for display; not persisted directly.
        public string NormalRangeDisplay =>
            (NormalRangeMin.HasValue && NormalRangeMax.HasValue)
                ? $"{NormalRangeMin:0.##} - {NormalRangeMax:0.##}"
                : "N/A";
    }

  


        public string NormalRangeDisplay
        {
            get
            {
                if (NormalRangeMin.HasValue && NormalRangeMax.HasValue)
                {
                    var difference = NormalRangeMax.Value - NormalRangeMin.Value;
                    return difference.ToString();
                }

                return "—";
            }
        }
    }

 
        public class DashboardViewModel
        {
            // ============================================================
            // TEST CATALOGUE
            // ============================================================

            /// <summary>
            /// Total number of test categories.
            /// Examples: Full Blood Count, Differential Count,
            /// Peripheral Blood Film, Coagulation Studies.
            /// </summary>
            public int CategoryCount { get; set; }

            /// <summary>
            /// Total number of test types in the catalogue.
            /// </summary>
            public int TestTypeCount { get; set; }

            /// <summary>
            /// Recent test types displayed on the dashboard.
            /// </summary>
            public IEnumerable<TestType> RecentTests { get; set; }
                = new List<TestType>();


            // ============================================================
            // CONSUMABLES
            // ============================================================

            /// <summary>
            /// Total number of consumable stock items.
            /// </summary>
            public int ConsumableCount { get; set; }

            /// <summary>
            /// Number of consumables that are below or within
            /// 10% of their reorder level.
            /// </summary>
            public int LowStockCount { get; set; }

            /// <summary>
            /// Consumables that require attention because their
            /// quantity on hand is near or below the reorder level.
            /// </summary>
            public IEnumerable<Consumable> StockAlerts { get; set; }
                = new List<Consumable>();


            // ============================================================
            // SUPPLIERS
            // ============================================================

            /// <summary>
            /// Total number of active suppliers.
            /// </summary>
            public int SupplierCount { get; set; }


            // ============================================================
            // STAFF
            // ============================================================

            /// <summary>
            /// Total number of doctor users.
            /// </summary>
            public int DoctorCount { get; set; }

            /// <summary>
            /// Total number of laboratory technician users.
            /// </summary>
            public int TechnicianCount { get; set; }

            public IEnumerable<Doctor> Doctors { get; set; } = new List<Doctor>();

            public IEnumerable<Technician> Technicians { get; set; } = new List<Technician>();


            // ============================================================
            // CONSUMABLE ORDERS
            // ============================================================

            /// <summary>
            /// Number of open orders.
            /// Includes:
            /// Ordered
            /// Partially complete
            /// </summary>
            public int OpenOrderCount { get; set; }

            /// <summary>
            /// Recent consumable orders displayed on the dashboard.
            /// </summary>
            public IEnumerable<ConsumableOrder> RecentOrders { get; set; }
                = new List<ConsumableOrder>();
        }
    




}
