
using LabManager.Models;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using LabManager.Models;
using LabManager.Repository;


namespace LabManager.Controllers
{
    public class TestsByCategoryController : Controller
    {
        private readonly ITestsByCategoryRepository _repository;

        public TestsByCategoryController(
            ITestsByCategoryRepository repository)
        {
            _repository = repository;
        }

        // GET: /TestsByCategory/Index
        public async Task<IActionResult> Index(
            DateTime? from,
            DateTime? to,
            int? categoryId)
        {
            DateTime fromDate = from?.Date
                ?? DateTime.Today.AddDays(-30);

            DateTime toDate = to?.Date
                ?? DateTime.Today;

            if (fromDate > toDate)
            {
                TempData["Error"] =
                    "The start date cannot be later than the end date.";

                fromDate = toDate.AddDays(-30);
            }

            var rows = await _repository.GetTestsByCategory(
                fromDate,
                toDate,
                categoryId);

            var total = await _repository.GetTotalTestsPerformed(
                fromDate,
                toDate,
                categoryId);

            var model = new TestsByCategoryReportViewModel
            {
                FromDate = fromDate,
                EndDate = toDate,
                TotalTests = total,
                Categories = rows.ToList()
            };

            ViewBag.Email = HttpContext.Session.GetString("Email");
            ViewBag.CategoryID = categoryId;

            return View(model);
        }

        // GET: /TestsByCategory/ExportCsv
        public async Task<IActionResult> ExportCsv(
            DateTime from,
            DateTime to,
            int? categoryId)
        {
            if (from.Date > to.Date)
            {
                return BadRequest(
                    "The start date cannot be later than the end date.");
            }

            var rows = await _repository.GetTestsByCategory(
                from.Date,
                to.Date,
                categoryId);

            var csv = new StringBuilder();

            csv.AppendLine(
                "Category,Test Type,Tests Performed");

            foreach (var row in rows)
            {
                csv.AppendLine(
                    $"{EscapeCsv(row.CategoryName)}," +
                    $"{EscapeCsv(row.TestTypeName)}," +
                    $"{row.TestCount}");
            }

            byte[] bytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(csv.ToString()))
                .ToArray();

            string fileName =
                $"TestsByCategory_{from:yyyyMMdd}_{to:yyyyMMdd}.csv";

            return File(bytes, "text/csv", fileName);
        }

        private static string EscapeCsv(string? value)
        {
            value ??= string.Empty;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}

