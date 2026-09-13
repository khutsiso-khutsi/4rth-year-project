using LabManager.DataAccess;
using LabManager.Models;
using LabManager.Repository;

namespace LabManager.Repositories
{
    public class TestCategoryRepository : ITestCategoryrepository
    {
        private readonly ISqlDataAcess _dataAcess;

        public TestCategoryRepository(ISqlDataAcess dataAcess)
        {
            _dataAcess = dataAcess;
        }

        // =========================================================
        // ADD CATEGORY
        // =========================================================

        public async Task<bool> AddTestCategory(TestCategory category)
        {
            try
            {
                await _dataAcess.SaveData(
                    "sp_AddCategory",
                    new
                    {
                        category.CategoryName,
                        category.Description
                    });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =========================================================
        // ADD TEST TYPE
        // =========================================================

        public async Task<bool> AddTestType(TestType test)
        {
            try
            {
                await _dataAcess.SaveData(
                    "sp_AddTestType",
                    new
                    {
                        test.TestName,
                        test.CategoryId,
                        test.SampleTypeId,
                        test.TurnaroundTime,
                        test.ConsumablesUsed,
                        test.UnitMeasurement,
                        test.NormalRangeMax,
                        test.NormalRangeMin
                      
                    });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =========================================================
        // EDIT TEST TYPE
        // =========================================================

        public async Task<bool> EditTestType(TestType test)
        {
            try
            {
                await _dataAcess.SaveData(
                    "sp_UpdateTestType",
                    new
                    {
                        test.TestId,
                        test.TestName,
                        test.CategoryId,
                        test.SampleTypeId,
                        test.TurnaroundTime,
                        test.ConsumablesUsed,
                        test.UnitMeasurement,
                        test.NormalRangeMax,
                        test.NormalRangeMin
                    });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =========================================================
        // EDIT CATEGORY
        // =========================================================

        public async Task<bool> EditCategory(TestCategory category)
        {
            try
            {
                await _dataAcess.SaveData(
                    "sp_UpdateTestCategory",
                    new
                    {
                        category.Id,
                        category.CategoryName,
                        category.Description
                    });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =========================================================
        // DELETE TEST TYPE
        // =========================================================

        public async Task<bool> DeleteTestType(int id)
        {
            try
            {
                await _dataAcess.SaveData(
                    "sp_DeleteTestType",
                    new
                    {
                        TestId = id
                    });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =========================================================
        // DELETE CATEGORY
        // =========================================================

        public async Task<bool> DeleteCategory(int id)
        {
            try
            {
                await _dataAcess.SaveData(
                    "sp_DeleteTestCategory",
                    new
                    {
                        CategoryId = id
                    });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =========================================================
        // GET ALL CATEGORIES
        // =========================================================

        public async Task<IEnumerable<TestCategory>> GetAllCategory()
        {
            return await _dataAcess.GetData<TestCategory, dynamic>(
                "sp_GetAllTestCategories",
                new { }
            );
        }

        // =========================================================
        // GET ALL TEST TYPES
        // Expects sp_GetAllTestTypes to join Category and alias the
        // category name column as CategoryName so TestType.CategoryName
        // populates automatically.
        // =========================================================

        public async Task<IEnumerable<TestType>> GetAllTestTypes()
        {
            return await _dataAcess.GetData<TestType, dynamic>(
                "sp_GetAllTestTypes",
                new { }
            );
        }

        // =========================================================
        // GET ONE TEST TYPE
        // =========================================================

        public async Task<TestType?> GetTestType(int id)
        {
            IEnumerable<TestType> result =
                await _dataAcess.GetData<TestType, dynamic>(
                    "sp_GetTestTypeById",
                    new
                    {
                        TestId = id
                    });

            return result.FirstOrDefault();
        }

        //Foreign keys 
        public async Task<IEnumerable<TestCategory>> GetCategory()
        {
            return await _dataAcess.GetData<TestCategory, dynamic>(
                "sp_GetCategory",
                new { }
            );
        }



        public async Task<IEnumerable<TestType>> GetSampleType()
        {
            return await _dataAcess.GetData<TestType, dynamic>(
                "sp_GetSampleType",
                new { }
            );
        }
    }
}