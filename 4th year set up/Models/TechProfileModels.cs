using System;
using System.Collections.Generic;

namespace _4th_year_set_up.Models
{
    // Shown on the technician Profile screen. Name, phone and password can be changed; the rest is read-only.
    public class TechProfileViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string IDNumber { get; set; }
        public string EmployeeNumber { get; set; }
        public string ContactNumber { get; set; }
        public DateTime RegistrationDate { get; set; }
        public bool IsSenior { get; set; }
        public List<string> TestTypes { get; set; } = new();
    }
}
