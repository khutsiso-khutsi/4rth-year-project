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
        Task<bool> AddTestCategory (TestCategory category);
        Task<bool> AddTestType(TestType test);

        Task<bool> Edit(TestType test);
        Task<bool> Edit(TestCategory category);

        Task<bool> DeleteCategory(int id );

        Task<IEnumerable<TestCategory>> GetAllCategory();
        Task<TestType> GetTestType(int id);











    }


} 
