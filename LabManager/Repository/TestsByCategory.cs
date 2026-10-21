using LabManager.DataAccess;
using LabManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Repository
{
    public interface ITestsByCategoryRepository
    {
        Task<IEnumerable<TestsByCategoryReportItem>>
            GetTestsByCategory(
                DateTime fromDate,
                DateTime toDate,
                int? categoryId = null);

        Task<int> GetTotalTestsPerformed(
            DateTime fromDate,
            DateTime toDate,
            int? categoryId = null);
    }


    public class TestsByCategoryRepository : ITestsByCategoryRepository
    {
        private readonly ISqlDataAcess _dataAcess;

        public TestsByCategoryRepository(ISqlDataAcess dataAcess)
        {
            _dataAcess = dataAcess;
        }

        // =====================================================
        // GET TESTS BY CATEGORY
        // =====================================================
        public async Task<IEnumerable<TestsByCategoryReportItem>>
            GetTestsByCategory(
                DateTime fromDate,
                DateTime toDate,
                int? categoryId = null)
        {
            try
            {
                return await _dataAcess.GetData<
                    TestsByCategoryReportItem, dynamic>(
                    "sp_GetTestsByCategory",
                    new
                    {
                        FromDate = fromDate.Date,
                        ToDate = toDate.Date,
                        CategoryID = categoryId
                    });
            }
            catch
            {
                return Enumerable.Empty<TestsByCategoryReportItem>();
            }
        }

        // =====================================================
        // GET TOTAL TESTS PERFORMED
        // =====================================================
        public async Task<int> GetTotalTestsPerformed(
            DateTime fromDate,
            DateTime toDate,
            int? categoryId = null)
        {
            try
            {
                var result = await _dataAcess.GetData<
                    TotalTestsPerformedResult, dynamic>(
                    "sp_GetTotalTestsPerformed",
                    new
                    {
                        FromDate = fromDate.Date,
                        ToDate = toDate.Date,
                        CategoryID = categoryId
                    });

                return result.FirstOrDefault()?.TotalTests ?? 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
