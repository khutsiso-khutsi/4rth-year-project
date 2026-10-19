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
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string IDNumber { get; set; }

        // Nullable: existing users have no date of birth until they enter one
        public DateTime? DateOfBirth { get; set; }

        public DateTime RegistrationDate { get; set; }
        public string CellphoneNumber { get; set; }
        public string HomeAddress { get; set; }
    }

    // Used only to read the stored password hash for a user.
    public class UserCredentials
    {
        public string PasswordHash { get; set; }
    }

    public class UpdateAccountInfoRequest
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, ErrorMessage = "First name cannot be longer than 100 characters.")]
        [RegularExpression(@"^[\p{L}][\p{L} '\-]*$",
            ErrorMessage = "First name can only contain letters, spaces, hyphens and apostrophes.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, ErrorMessage = "Last name cannot be longer than 100 characters.")]
        [RegularExpression(@"^[\p{L}][\p{L} '\-]*$",
            ErrorMessage = "Last name can only contain letters, spaces, hyphens and apostrophes.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [StringLength(256, ErrorMessage = "Email address cannot be longer than 256 characters.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "ID number is required.")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "ID number must contain exactly 13 digits.")]
        public string IDNumber { get; set; }

        [Required(ErrorMessage = "Date of birth is required.")]
        [ValidDateOfBirth]
        public DateTime? DateOfBirth { get; set; }
    }

    public class UpdateContactRequest
    {
        [Required(ErrorMessage = "Cellphone number is required.")]
        [RegularExpression(@"^0[6-8]\d{8}$",
            ErrorMessage = "Please enter a valid 10-digit SA cellphone number.")]
        public string CellphoneNumber { get; set; }

        [Required(ErrorMessage = "Home address is required.")]
        [StringLength(255, MinimumLength = 5,
            ErrorMessage = "Please enter a valid home address (min. 5 characters).")]
        public string HomeAddress { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Please enter your current password.")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Please enter a new password.")]
        [StringLength(128, MinimumLength = 8,
            ErrorMessage = "Password must be at least 8 characters with 1 uppercase letter and 1 number.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must be at least 8 characters with 1 uppercase letter and 1 number.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    // =====================================================
    // CUSTOM ATTRIBUTE: date of birth must be a real, past, sensible date
    // =====================================================
    [AttributeUsage(AttributeTargets.Property)]
    public class ValidDateOfBirthAttribute : ValidationAttribute
    {
        public ValidDateOfBirthAttribute()
        {
            ErrorMessage = "Please enter a valid date of birth.";
        }

        public override bool IsValid(object value)
        {
            // [Required] handles null; nothing to check here
            if (value == null) return true;

            if (value is not DateTime date) return false;

            return date.Date <= DateTime.Today && date.Year >= 1900;
        }
    }

    // =====================================================
    // SOUTH AFRICAN ID NUMBER CHECKS
    // Format: YYMMDD SSSS C A Z
    //   YYMMDD = date of birth, SSSS = sequence/gender,
    //   C = citizenship (0 = SA citizen, 1 = permanent resident),
    //   A = historic digit, Z = Luhn check digit
    // =====================================================
    public static class SaIdNumber
    {
        // 13 digits, citizenship digit 0/1 and a passing Luhn checksum
        public static bool IsValid(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length != 13)
                return false;

            foreach (char c in id)
            {
                if (c < '0' || c > '9') return false;
            }

            if (id[10] != '0' && id[10] != '1')
                return false;

            return PassesLuhn(id);
        }

        // The YYMMDD part of the ID must equal the date of birth entered.
        // The ID has a 2-digit year, so 19YY and 20YY are both accepted.
        public static bool MatchesDateOfBirth(string id, DateTime dateOfBirth)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length < 6)
                return false;

            if (!int.TryParse(id.Substring(0, 2), out int yy) ||
                !int.TryParse(id.Substring(2, 2), out int mm) ||
                !int.TryParse(id.Substring(4, 2), out int dd))
                return false;

            return dateOfBirth.Month == mm &&
                   dateOfBirth.Day == dd &&
                   (dateOfBirth.Year == 1900 + yy || dateOfBirth.Year == 2000 + yy);
        }

        private static bool PassesLuhn(string digits)
        {
            int sum = 0;
            bool doubleIt = false;

            for (int i = digits.Length - 1; i >= 0; i--)
            {
                int d = digits[i] - '0';

                if (doubleIt)
                {
                    d *= 2;
                    if (d > 9) d -= 9;
                }

                sum += d;
                doubleIt = !doubleIt;
            }

            return sum % 10 == 0;
        }
    }
}

