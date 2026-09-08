using LabManager.DataAccess;
using LabManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace LabManager.Repository
{
    public class TestCategoryRepository : ITestCategoryrepository
    {
        private readonly ISqlDataAcess _dataAcess;

        public TestCategoryRepository(ISqlDataAcess dataAcess)
        {
            _dataAcess = dataAcess;
        }

        public async Task<bool> AddTestCategory(TestCategory category)
        {
            try
            {
                await _dataAcess.SaveData("", new { category.CategoryName, category.Description });

                return true;
            }
            catch (Exception) {
               
                return false;
            
            }

    }
        public async Task<bool> AddTestType(TestType test)
        {
            try
            {
                await _dataAcess.SaveData ("", new { test.TestName, test.Category, test.TurnaroundTime, test.ConsumablesUsed, test.UnitMeasurement, test.NormalRangeMax, test.NormalRangeMin, test.SampleType, });
                return true;
            }
            catch (Exception ex) {

                return false;
            }

        }

        public async Task<bool> Edit(TestType test)
        {
            try
            {
                await _dataAcess.SaveData("", test);
                return true;

            }
            catch (Exception) {

                return false;
            
            }


        }
        public async Task<bool> Edit(TestCategory category)
        {

            try
            {
                await _dataAcess.SaveData("", category);
                return true;

            }
            catch (Exception)
            {

                return false;

            }
        }

        public async Task<bool> DeleteCategory(int id)
        {
            await _dataAcess.SaveData("", new { TestId = id });
            return true ;   
        }

        public async Task<IEnumerable<TestCategory>> GetAllCategory()
        {
            string query = "";
            return await _dataAcess.GetData<TestCategory, dynamic>(query, new { });
        }
       public async  Task<TestType> GetTestType(int id)
        {
            string query = "";
            IEnumerable<TestType> result = await _dataAcess.GetData<TestType, dynamic>(query, new { Id = id });

            return result.FirstOrDefault();
        }

      

    }
}
