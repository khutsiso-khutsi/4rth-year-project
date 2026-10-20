using _4th_year_set_up.Models;
using LabManager.DataAccess;
using LabManager.Models;
using LabManager.Repository;

namespace LabManager.Repositories
{
    public class TestCatalogueRepository : ITestCatalogueRepository
    {
        private readonly ISqlDataAcess _dataAccess;

        public TestCatalogueRepository(ISqlDataAcess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        // ---------------------------------------------------------
        // GET ALL TEST TYPES
        // ---------------------------------------------------------
        public async Task<IEnumerable<TestType>> GetAllTestTypes()
        {
            return await _dataAccess.GetData<TestType, dynamic>(
                "sp_GetAllTestTypes",
                new { }
            );
        }

        // ---------------------------------------------------------
        // GET ALL CATEGORIES
        // ---------------------------------------------------------
        public async Task<IEnumerable<TestCategory>> GetAllCategories()
        {
            return await _dataAccess.GetData<TestCategory, object>(
                "sp_GetAllTestCategories",
                new { }
            );
        }



        // ---------------------------------------------------------
        // ADD TEST TYPE
        // ---------------------------------------------------------
        public async Task<bool> AddTestType(TestType test)
        {
            try
            {
                await _dataAccess.SaveData(
                    "sp_AddTestType",
                    new
                    {
                        TestName = test.TestName,
                        CategoryId = test.CategoryId,
                        SampleTypeId = test.SampleTypeId,
                        UnitMeasurement = test.UnitMeasurement,
                        NormalRangeMin = test.NormalRangeMin,
                        NormalRangeMax = test.NormalRangeMax,
                        TurnaroundTime = test.TurnaroundTime,
                        ConsumablesUsed = test.ConsumablesUsed,

                    }
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ---------------------------------------------------------
        // UPDATE TEST TYPE
        // ---------------------------------------------------------
        public async Task<bool> UpdateTestType(TestType test)
        {
            try
            {
                await _dataAccess.SaveData(
                    "sp_UpdateTestType",
                    new
                    {
                        TestId = test.TestId,
                        TestName = test.TestName,
                        CategoryId = test.CategoryId,
                        SampleTypeId = test.SampleTypeId,
                        UnitMeasurement = test.UnitMeasurement,
                        NormalRangeMin = test.NormalRangeMin,
                        NormalRangeMax = test.NormalRangeMax,
                        TurnaroundTime = test.TurnaroundTime,
                        ConsumablesUsed = test.ConsumablesUsed
                    }
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ---------------------------------------------------------
        // DELETE TEST TYPE
        // ---------------------------------------------------------
        public async Task<bool> DeleteTestType(int testId)
        {
            try
            {
                await _dataAccess.SaveData(
                    "sp_DeleteTestType",
                    new
                    {
                        TestId = testId
                    }
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ---------------------------------------------------------
        // ADD CATEGORY
        // ---------------------------------------------------------
        public async Task<bool> AddCategory(TestCategory category)
        {
            try
            {
                await _dataAccess.SaveData(
                    "sp_AddCategory",
                    new
                    {
                        CategoryName = category.CategoryName,
                        Description = category.Description,
                        
                    }
                );

                return true;
            }
            catch
            {
                return false;
            }
        }


        public async Task<TestType> GetTestTypeById(int testId)
        {
            var result = await _dataAccess.GetData<TestType, dynamic>(
                "sp_GetTestTypeById",
                new { TestId = testId }
            );
            return result.FirstOrDefault();
        }

        //Foreign keys 
        public async Task<IEnumerable<TestCategory>> GetCategory()
        {
            return await _dataAccess.GetData<TestCategory, dynamic>(
                "sp_GetCategory",
                new { }
            );
        }



        public async Task<IEnumerable<TestType>> GetSampleType()
        {
            return await _dataAccess.GetData<TestType, dynamic>(
                "sp_GetSampleType",
                new { }
            );
        }

        public async Task AddSampleTypeAsync(SampleType sampleType)
        {
            await _dataAccess.SaveData(
                "dbo.sp_AddSampleType",
                new { sampleType.SampleName }
            );
        }

        public async Task<IEnumerable<TestType>>
            GetDeletedTestTypesAsync()
        {
            return await _dataAccess.GetData<TestType, dynamic>(
                "dbo.sp_GetDeletedTestTypes",
                new { });
        }

        public async Task DeleteTestTypeAsync(int testId)
        {
            await _dataAccess.SaveData(
                "dbo.sp_SoftDeleteTestType",
                new { TestId = testId });
        }

        public async Task RestoreTestTypeAsync(int testId)
        {
            await _dataAccess.SaveData(
                "dbo.sp_RestoreTestType",
                new { TestId = testId });
        }

     


        public async Task<IEnumerable<Consumable>> GetConsumables()
        {
            return await _dataAccess.GetData<Consumable, dynamic>(
                "sp_GetorderConsumables",
                new { });
        }

    }
}