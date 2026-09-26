using LabManager.Models;
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

            Task<bool> AddCategory(TestCategory category);
        
           Task<TestType> GetTestTypeById(int testId);


        Task<IEnumerable<TestCategory>> GetCategory();
        Task<IEnumerable<TestType>> GetSampleType();
       }











   


} 
