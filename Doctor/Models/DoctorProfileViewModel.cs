using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Patient.Models
{
    public class DoctorProfileViewModel
    {
        public int DoctorID { get; set; }

        [Required, MinLength(2)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, MinLength(2)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Licence Number")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Cellphone number must be exactly 10 digits.")]
        [Display(Name = "Cellphone Number")]
        public string CellphoneNumber { get; set; } = string.Empty;

        [Required, MinLength(5)]
        [Display(Name = "Home Address")]
        public string HomeAddress { get; set; } = string.Empty;

        [Required, MinLength(2)]
        public string Specialization { get; set; } = string.Empty;

        [Required]
        public string Department { get; set; } = string.Empty;

        public SelectList DepartmentList => new SelectList(new[]
        {
            "Haematology",
            "Oncology",
            "Internal Medicine",
            "Pathology",
            "General Practice",
            "Cardiology",
            "Neurology",
            "Paediatrics",
            "Radiology",
            "Surgery"
        });

        [Required, MinLength(5)]
        [Display(Name = "Practice Address")]
        public string PracticeAddress { get; set; } = string.Empty;

        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; }
    }
}