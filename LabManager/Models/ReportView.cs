using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Models
{
    public class TestsByCategoryReportViewModel
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public int TotalTests { get; set; }

        public List<CategoryReportItemViewModel> Categories { get; set; }
            = new List<CategoryReportItemViewModel>();
    }

    public class CategoryReportItemViewModel
    {
        public string CategoryName { get; set; }

        public int TestCount { get; set; }

        public decimal Percentage { get; set; }
    }
}
