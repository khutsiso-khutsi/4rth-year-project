using _4th_year_set_up.Models;
using LabManager.DataAccess;
using LabManager.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Repository
{

    public interface ITestCatalogueRepository
    {
        Task<IEnumerable<TestType>> GetAllTestTypes();

        Task<IEnumerable<TestCategory>> GetAllCategories();

        Task<bool> AddTestType(TestType test);

        Task<bool> UpdateTestType(TestType test);

        Task<bool> DeleteTestType(int testId);

       

        Task<TestType> GetTestTypeById(int testId);


        Task<IEnumerable<TestCategory>> GetCategory();
        Task<IEnumerable<TestType>> GetSampleType();

        Task<IEnumerable<TestType>> GetDeletedTestTypesAsync();

        Task DeleteTestTypeAsync(int testId);

        Task RestoreTestTypeAsync(int testId);

        Task<IEnumerable<Consumable>> GetConsumables();



    }

    public enum CatalogueResult
    {
        Success = 0,
        Duplicate = 1,
        NotFound = 2,
        InUse = 3,
        Error = 4
    }

    /// <summary>One-row result returned by the write procedures.</summary>
    public class CatalogueOpResult
    {
        public int Result { get; set; }
        public int? NewId { get; set; }
    }

    // Remove this class if you already have a SampleType model
    // (it needs SampleTypeID and SampleName).


    public interface ICatalogueLookupRepository
    {
        Task<List<TestCategory>> GetCategoriesAsync();
        Task<(CatalogueResult Result, int? NewId)> AddCategoryAsync(string name, string? description);
        Task<CatalogueResult> UpdateCategoryAsync(int id, string name, string? description);
        Task<CatalogueResult> DeleteCategoryAsync(int id);

        Task<List<SampleType>> GetSampleTypesAsync();
        Task<(CatalogueResult Result, int? NewId)> AddSampleTypeAsync(string name);
        Task<CatalogueResult> UpdateSampleTypeAsync(int id, string name);
        Task<CatalogueResult> DeleteSampleTypeAsync(int id);
    }

    public class CatalogueLookupRepository : ICatalogueLookupRepository
    {
        private const int SqlForeignKeyViolation = 547;

        private readonly ISqlDataAcess _dataAccess;   // use the same type your other repositories inject

        public CatalogueLookupRepository(ISqlDataAcess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        // ---------------------------------------------------------
        // GET ALL CATEGORIES
        // ---------------------------------------------------------
        public async Task<List<TestCategory>> GetCategoriesAsync()
        {
            var rows = await _dataAccess.GetData<TestCategory, dynamic>(
                "sp_GetAllTestCategories",
                new { }
            );

            return rows.ToList();
        }

        // ---------------------------------------------------------
        // ADD CATEGORY
        // ---------------------------------------------------------
        public async Task<(CatalogueResult Result, int? NewId)> AddCategoryAsync(string name, string? description)
        {
            var r = await RunAsync(
                "sp_AddCategory",
                new
                {
                    CategoryName = name,
                    Description = description
                }
            );

            return ((CatalogueResult)r.Result, r.NewId);
        }

        // ---------------------------------------------------------
        // UPDATE CATEGORY
        // ---------------------------------------------------------
        public async Task<CatalogueResult> UpdateCategoryAsync(int id, string name, string? description)
        {
            var r = await RunAsync(
                "sp_UpdateCategory",
                new
                {
                    CategoryID = id,
                    CategoryName = name,
                    Description = description
                }
            );

            return (CatalogueResult)r.Result;
        }

        // ---------------------------------------------------------
        // DELETE CATEGORY
        // ---------------------------------------------------------
        public async Task<CatalogueResult> DeleteCategoryAsync(int id)
        {
            var r = await RunAsync(
                "sp_DeleteCategory",
                new { CategoryID = id }
            );

            return (CatalogueResult)r.Result;
        }

        // ---------------------------------------------------------
        // GET ALL SAMPLE TYPES
        // ---------------------------------------------------------
        public async Task<List<SampleType>> GetSampleTypesAsync()
        {
            var rows = await _dataAccess.GetData<SampleType, dynamic>(
                "sp_GetAllSampleTypes",
                new { }
            );

            return rows.ToList();
        }

        // ---------------------------------------------------------
        // ADD SAMPLE TYPE
        // ---------------------------------------------------------
        public async Task<(CatalogueResult Result, int? NewId)> AddSampleTypeAsync(string name)
        {
            var r = await RunAsync(
                "sp_AddSampleType",
                new { SampleName = name }
            );

            return ((CatalogueResult)r.Result, r.NewId);
        }

        // ---------------------------------------------------------
        // UPDATE SAMPLE TYPE
        // ---------------------------------------------------------
        public async Task<CatalogueResult> UpdateSampleTypeAsync(int id, string name)
        {
            var r = await RunAsync(
                "sp_UpdateSampleType",
                new
                {
                    SampleTypeID = id,
                    SampleName = name
                }
            );

            return (CatalogueResult)r.Result;
        }

        // ---------------------------------------------------------
        // DELETE SAMPLE TYPE
        // ---------------------------------------------------------
        public async Task<CatalogueResult> DeleteSampleTypeAsync(int id)
        {
            var r = await RunAsync(
                "sp_DeleteSampleType",
                new { SampleTypeID = id }
            );

            return (CatalogueResult)r.Result;
        }

        // ---------------------------------------------------------
        // HELPER: runs a write procedure and reads its Result row
        // ---------------------------------------------------------
        private async Task<CatalogueOpResult> RunAsync(string procedure, object parameters)
        {
            try
            {
                var rows = await _dataAccess.GetData<CatalogueOpResult, dynamic>(procedure, parameters);

                return rows.FirstOrDefault()
                       ?? new CatalogueOpResult { Result = (int)CatalogueResult.Error };
            }
            catch (SqlException ex) when (ex.Number == SqlForeignKeyViolation)
            {
                // A foreign key still references the row (e.g. a concurrent insert)
                return new CatalogueOpResult { Result = (int)CatalogueResult.InUse };
            }
            catch
            {
                return new CatalogueOpResult { Result = (int)CatalogueResult.Error };
            }
        }
    }


}
