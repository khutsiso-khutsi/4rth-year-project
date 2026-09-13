using LabManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Repository
{
  
        public interface ITestCategoryrepository
        {
        Task<bool> AddTestCategory(TestCategory category);
        Task<bool> EditCategory(TestCategory category);
        Task<bool> DeleteCategory(int id);
        Task<IEnumerable<TestCategory>> GetAllCategory();

        // Test types
        Task<bool> AddTestType(TestType test);
        Task<bool> EditTestType(TestType test);
        Task<bool> DeleteTestType(int id);
        Task<IEnumerable<TestType>> GetAllTestTypes();
        Task<TestType?> GetTestType(int id);


        Task<IEnumerable<TestCategory>> GetCategory();
        Task<IEnumerable<TestType>> GetSampleType();
    }











   


} 
