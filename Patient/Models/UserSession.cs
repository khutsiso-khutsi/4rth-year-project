using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patient.Models
{
    public class UserSession
    {
        public int UserID { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public string FullName { get; set; }
        public int RoleID { get; set; }
        // IsEmailVerified removed: not present in database; login flow does not require it.
    }

    public class LoginViewModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }
}