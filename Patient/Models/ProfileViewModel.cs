using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patient.Models
{
    public class ProfileViewModel
    {
        public int PatientID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string IDNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string CellphoneNumber { get; set; }
        public string HomeAddress { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string Email { get; set; }

        // Change password fields (not mapped to DB)
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
