namespace LabManager.Models
{
    public class TestReportViewModel
    {
        // Category name (can be empty/null for uncategorised tests)
        public string Category { get; set; } = string.Empty;

        // Total number of tests in the category for the reporting period
        public int TotalTests { get; set; }
    }
}
