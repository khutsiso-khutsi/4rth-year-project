using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Models
{
   public class Technician
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string EmployeeNumber { get; set; }

        public string EmailAddress { get; set; }

        // Foreign key
        public int TestTypeID { get; set; }

        // Comes from TestType table
        public string TestName { get; set; }

        public bool IsActive { get; set; }

        public string Status { get; set; }

        public string FullName
        {
            get
            {
                return $"{FirstName} {LastName}";
            }
        }
    }
}

