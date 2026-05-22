using System.ComponentModel.DataAnnotations;

namespace _4th_year_set_up.Models
{
    public class LoginViewModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class UserSession
    {
        public int UserID { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public string FullName { get; set; }
        public int RoleID { get; set; }
    }
}