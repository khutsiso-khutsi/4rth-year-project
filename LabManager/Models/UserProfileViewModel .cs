using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Models
{
   
    


        public class UserProfileViewModel
        {
        public int UserID { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "ID number is required.")]
        public string IDNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required.")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Cellphone number is required.")]
        public string CellphoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Home address is required.")]
        public string HomeAddress { get; set; } = string.Empty;

        public DateTime RegistrationDate { get; set; }

        public string? PasswordHash { get; set; }
    }
}
    
