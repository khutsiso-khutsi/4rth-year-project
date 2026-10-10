using Patient.Models;

namespace _4th_year_set_up.Models
{
    /// <summary>
    /// Data for the patient's "results report" PDF (spec 6: all test results
    /// in a date range, grouped by test category).
    /// </summary>
    public class PatientReportViewModel
    {
        public ProfileViewModel? Profile { get; set; }
        public List<TestRequest> Requests { get; set; } = new();
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        /// <summary>True when wkhtmltopdf isn't installed and the report is shown as a printable page instead.</summary>
        public bool PrintMode { get; set; }
    }
}
