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

        public string FullName { get; set; }

        public string Email { get; set; }

        public string EmployeeNumber { get; set; }

        public List<string> TestTypes { get; set; } = new();
    }
}
