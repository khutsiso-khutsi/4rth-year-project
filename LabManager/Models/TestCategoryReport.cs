
namespace LabManager.Models
{
    public class TestsByCategoryReportViewModel
    {
        public DateTime FromDate { get; set; }

        public DateTime EndDate { get; set; }

        public int TotalTests { get; set; }

        public List<TestsByCategoryReportItem> Categories { get; set; }
            = new List<TestsByCategoryReportItem>();
    }

    public class TestCategoryReportItem
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public int TestId { get; set; }
        public string TestTypeName { get; set; } = string.Empty;

        public int TestCount { get; set; }
    }

    public class TestCategoryOption
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class TestsByCategoryReportItem
    {
        public string CategoryName { get; set; } = "";
        public string TestTypeName { get; set; } = "";
        public DateTime PerformedDate { get; set; }
        public long TestCount { get; set; }
    }

    public class TotalTestsPerformedResult
    {
        public int TotalTests { get; set; }
    }


    
}
