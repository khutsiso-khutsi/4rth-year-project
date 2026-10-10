using System;
using System.ComponentModel.DataAnnotations;

namespace Patient.Models
{
    /// <summary>
    /// The doctor's own profile. Only uses columns that really exist on the
    /// Doctors / Users tables (spec: name, surname, HPCSA number, e-mail,
    /// contact number).
    ///
    /// The doctor may change their name and contact number. E-mail (their
    /// username) and HPCSA number are managed by the admin, so they are
    /// shown read-only here.
    /// </summary>
    public class DoctorProfileViewModel
    {
        public int DoctorID { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be 2 to 50 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be 2 to 50 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Contact number must be 10 digits and start with 0, e.g. 0821234567.")]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        // ---- Read-only (set from the database, never from the posted form) ----

        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "HPCSA Number")]
        public string HPCSANumber { get; set; } = string.Empty;

        [Display(Name = "Registration Date")]
        public DateTime? RegistrationDate { get; set; }
    }
}
