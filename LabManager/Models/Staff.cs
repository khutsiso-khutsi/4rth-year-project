using System;
using System.ComponentModel.DataAnnotations;

namespace LabManager.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string EmailAddress { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public string? PasswordHash { get; set; }

        public bool MustChangePassword { get; set; } = true;

        public DateTime? TemporaryPasswordExpiry { get; set; }

        public bool IsActive { get; set; } = true;

        public string Status { get; set; } = "Active";

        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    public class Doctor : User
    {
        [Required]
        [Display(Name = "HPCSA Number")]
        public string HCPSANumber { get; set; } = string.Empty;

        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        public Doctor()
        {
            Role = "Doctor";
        }
    }

    public class Technician : User
    {
        [Required]
        [Display(Name = "Employee Number")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Display(Name = "Test Type")]
        public int TestTypeID { get; set; }

        // Populated by the test-type join in SQL.
        public string? TestName { get; set; }

        public Technician()
        {
            Role = "Technician";
        }
    }

    public class Staff
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public string? Status { get; set; }

        [Display(Name = "HPCSA Number")]
        public string? HPCSANumber { get; set; }

        [Display(Name = "Employee Number")]
        public string? EmployeeNumber { get; set; }

        [Display(Name = "Test Types")]
        public string? TestTypes { get; set; }
    }

    
}
