using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string HCPSANumber { get; set; }

        public string EmailAddress { get; set; }

        public string ContactNumber { get; set; }

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

