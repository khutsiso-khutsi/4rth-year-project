using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Models
{
    public   class UserProfileViewModel
    {
        public string Id { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Email { get; set; }
        public string IDNumber { get; set; }

        public DateTime RegistrationDate { get; set; }
        public DateTime DateOfBirth { get; set; }

        public string CellphoneNumber { get; set; }
        public string HomeAddress { get; set; }

        // Password fields (prototype only)
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }

        public string Role { get; set; }

    }
}
